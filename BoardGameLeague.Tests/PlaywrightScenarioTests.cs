using BoardGameLeague.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace BoardGameLeague.Tests;

// End-to-end browser scenario: drives a real (headless) Chromium instance against a
// real Kestrel-hosted copy of the app via PlaywrightWebApplicationFactory. Unlike the
// API tests in ApiCrudTests.cs, this exercises the actual Razor views, client-side JS
// (search debounce, autocomplete dropdowns, culture-aware form fields) and full HTTP
// round trips a real user would hit.
public class PlaywrightScenarioTests : IClassFixture<PlaywrightWebApplicationFactory>, IAsyncLifetime
{
    private readonly PlaywrightWebApplicationFactory _factory;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private string _baseUrl = string.Empty;

    public PlaywrightScenarioTests(PlaywrightWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Force the factory to actually start its Kestrel listener before we read the address.
        _factory.CreateClient();
        _baseUrl = _factory.ServerAddress.TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        // The app authenticates via the same header-based "Test" scheme the API tests use
        // (see PlaywrightWebApplicationFactory), so every request the browser makes carries
        // Admin+Manager claims without having to drive the real Identity login form.
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = "Test Admin,Manager"
            }
        });
        _page = await context.NewPageAsync();

        // Delete links go through a native confirm() before navigating (see site.js
        // initDeleteConfirm). Playwright auto-dismisses dialogs unless told otherwise,
        // which would silently swallow every delete click, so auto-accept them here.
        _page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task TenStepUserJourney_CreatesEditsSearchesAndDeletesAcrossEntities()
    {
        // Grab real seeded team/game names up front so the scenario doesn't hardcode
        // data that might drift if the seed changes.
        string teamAName, teamBName;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BoardGameLeagueDbContext>();
            var teams = await context.Teams.OrderBy(t => t.Name).Take(2).ToListAsync();
            teamAName = teams[0].Name;
            teamBName = teams[1].Name;
        }

        // Step 1: Home page loads.
        await _page.GotoAsync(_baseUrl + "/");
        await Assertions.Expect(_page).ToHaveTitleAsync(new System.Text.RegularExpressions.Regex("BoardGameLeague"));

        // Step 2: Players index lists seeded players.
        await _page.GotoAsync(_baseUrl + "/Players");
        await Assertions.Expect(_page.Locator("#player-search-results h2").First).ToBeVisibleAsync();

        // Step 3: Per-page AJAX search filters the player list.
        await _page.FillAsync(".js-search-input", "a");
        await Assertions.Expect(_page.Locator("#player-search-results")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("search-updated"));

        // Step 4: Create a board game with a period-decimal Complexity ("3.00").
        var gameName = $"Playwright Test Game {Guid.NewGuid():N}".Substring(0, 24);
        await _page.GotoAsync(_baseUrl + "/BoardGames/Create");
        await _page.FillAsync("#Name", gameName);
        await _page.SelectOptionAsync("#Category", "Strategy");
        await _page.FillAsync("#MinPlayers", "2");
        await _page.FillAsync("#MaxPlayers", "4");
        await _page.FillAsync("#AveragePlayTimeMinutes", "45");
        await _page.FillAsync("#Complexity", "3.00");
        await _page.ClickAsync("button:has-text('Save game')");
        await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/BoardGames$"));
        await Assertions.Expect(_page.Locator($"h2:has-text('{gameName}')")).ToBeVisibleAsync();

        // Step 5: Edit that game, changing Complexity to another period-decimal value.
        await _page.ClickAsync($".metric-card:has(h2:has-text('{gameName}')) >> text=Edit");
        await Assertions.Expect(_page.Locator("#Complexity")).ToHaveValueAsync("3");
        await _page.FillAsync("#Complexity", "4.50");
        await _page.ClickAsync("button:has-text('Save changes')");
        await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/BoardGames$"));
        // The card renders Complexity.ToString("F1") in the app's "hr" culture, so 4.5 displays as "4,5".
        await Assertions.Expect(_page.Locator($".metric-card:has(h2:has-text('{gameName}'))")).ToContainTextAsync("Complexity: 4,5");

        // Step 6: Schedule a match via the autocomplete search fields (Create).
        await _page.GotoAsync(_baseUrl + "/Matches/Create");
        await FillAutocompleteAsync("#TeamAInput", teamAName);
        await FillAutocompleteAsync("#TeamBInput", teamBName);
        await FillAutocompleteFirstResultAsync("#GameInput", "a");
        await FillAutocompleteFirstResultAsync("#TournamentInput", "a");
        await _page.FillAsync("#ScoreA", "3");
        await _page.FillAsync("#ScoreB", "1");
        await _page.ClickAsync("button:has-text('Schedule match')");
        await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Matches$"));

        // Step 7: Global search finds the new match, formatted as "TeamA - TeamB".
        await _page.FillAsync("#global-search-input", teamAName);
        var matchResult = _page.Locator($"#global-search-results a:has-text('{teamAName} - {teamBName}')");
        await Assertions.Expect(matchResult).ToBeVisibleAsync();

        // Step 8: Follow the search result to the match Details page.
        await matchResult.ClickAsync();
        await Assertions.Expect(_page.Locator("h1")).ToHaveTextAsync($"{teamAName} vs {teamBName}");

        // Step 9: Edit the match's score from its Details page and confirm it persists.
        await _page.ClickAsync("a:has-text('Edit')");
        await _page.FillAsync("#ScoreA", "9");
        await _page.ClickAsync("button:has-text('Save match')");
        await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Matches$"));

        // Step 10: Delete the board game created in step 4 and confirm it's gone.
        await _page.GotoAsync(_baseUrl + "/BoardGames");
        await _page.ClickAsync($".metric-card:has(h2:has-text('{gameName}')) >> text=Delete");
        await _page.ClickAsync("button:has-text('Delete')");
        await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/BoardGames$"));
        await Assertions.Expect(_page.Locator($"h2:has-text('{gameName}')")).Not.ToBeVisibleAsync();
    }

    // Each Create/Edit page has several independent autocomplete widgets (Team A, Team B,
    // Game, Tournament, ...) sharing the same ".autocomplete-suggestion" class, so results
    // must be scoped to the specific widget's ".autocomplete-control" container rather than
    // queried globally, or a stale dropdown from an earlier field can be clicked by mistake.
    private async Task FillAutocompleteAsync(string inputSelector, string query)
    {
        var control = _page.Locator(".autocomplete-control", new PageLocatorOptions { Has = _page.Locator(inputSelector) });
        await _page.ClickAsync(inputSelector);
        await _page.FillAsync(inputSelector, query.Substring(0, Math.Min(3, query.Length)));
        await control.Locator($".autocomplete-suggestion:text-is('{query}')").ClickAsync();
    }

    private async Task FillAutocompleteFirstResultAsync(string inputSelector, string query)
    {
        var control = _page.Locator(".autocomplete-control", new PageLocatorOptions { Has = _page.Locator(inputSelector) });
        await _page.ClickAsync(inputSelector);
        await _page.FillAsync(inputSelector, query);
        await control.Locator(".autocomplete-suggestion").First.ClickAsync();
    }
}
