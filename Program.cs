using BoardGameLeague.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<BoardGameLeagueDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BoardGameLeagueDbContext")));
builder.Services.AddScoped<ILeagueRepository, EfLeagueRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BoardGameLeagueDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    dbContext.Database.Migrate();
    await SeedDatabaseAsync(dbContext, logger);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task SeedDatabaseAsync(BoardGameLeagueDbContext context, ILogger logger)
{
    if (await context.Teams.AnyAsync())
    {
        return;
    }

    var sample = await LeagueDataFactory.CreateSampleLeagueAsync();
    var players = sample.AllTeams.SelectMany(t => t.Players).Distinct().ToList();
    var venues = sample.AllTournaments.Select(t => t.Venue).Distinct().ToList();

    await context.Players.AddRangeAsync(players);
    await context.BoardGames.AddRangeAsync(sample.AllBoardGames);
    await context.Venues.AddRangeAsync(venues);
    await context.Teams.AddRangeAsync(sample.AllTeams);
    await context.Tournaments.AddRangeAsync(sample.AllTournaments);
    await context.Matches.AddRangeAsync(sample.AllMatches);
    await context.SaveChangesAsync();

    await ValidateSeedDataAsync(context, logger);
}

static async Task ValidateSeedDataAsync(BoardGameLeagueDbContext context, ILogger logger)
{
    var orphanMatches = await context.Matches
        .Include(m => m.Game)
        .Include(m => m.TeamA)
        .Include(m => m.TeamB)
        .Include(m => m.Tournament)
            .ThenInclude(t => t.Venue)
        .AsNoTracking()
        .Where(m => m.Game == null || m.TeamA == null || m.TeamB == null || m.Tournament == null || m.Tournament.Venue == null)
        .ToListAsync();

    if (orphanMatches.Any())
    {
        logger.LogWarning("Seed data validation found {Count} match record(s) with missing relations.", orphanMatches.Count);
        foreach (var match in orphanMatches)
        {
            logger.LogWarning("Match {MatchId} is missing: Game={HasGame}, TeamA={HasTeamA}, TeamB={HasTeamB}, Tournament={HasTournament}, Venue={HasVenue}.",
                match.Id,
                match.Game != null,
                match.TeamA != null,
                match.TeamB != null,
                match.Tournament != null,
                match.Tournament?.Venue != null);
        }
    }
    else
    {
        logger.LogInformation("Seed data validation passed: all matches have complete game, team, tournament, and venue relations.");
    }
}
