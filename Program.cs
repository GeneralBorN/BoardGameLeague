using BoardGameLeague.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // <GenerateAssemblyInfo>false</GenerateAssemblyInfo> above suppresses the auto-generated
    // UserSecretsIdAttribute that AddUserSecrets<T>() normally reads off the entry assembly,
    // so the id (matching <UserSecretsId> in BoardGameLeague.csproj) has to be passed explicitly.
    builder.Configuration.AddUserSecrets("1198915e-fa4d-4dab-9a8b-2bce0f4fede4");
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// File-based logging: FileLoggerProvider (registered on the logging pipeline below) formats
// every ILogger call into a LogEntry and drops it on this channel; FileLoggerBackgroundService
// is the sole consumer, appending JSON lines to a daily-rolling file under App_Data/logs.
// LogQueryService reads those files back for the /Admin/Logs page and the /api/logs endpoint.
var fileLoggerOptions = new BoardGameLeague.Logging.FileLoggerOptions
{
    LogDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "logs"),
    MinLevel = LogLevel.Information
};
builder.Services.AddSingleton(fileLoggerOptions);
builder.Services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<BoardGameLeague.Logging.LogEntry>(
    new System.Threading.Channels.UnboundedChannelOptions { SingleReader = true }));
builder.Logging.Services.AddSingleton<ILoggerProvider, BoardGameLeague.Logging.FileLoggerProvider>();
builder.Services.AddHostedService<BoardGameLeague.Logging.FileLoggerBackgroundService>();
builder.Services.AddSingleton<BoardGameLeague.Logging.ILogQueryService, BoardGameLeague.Logging.LogQueryService>();
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddDbContext<BoardGameLeagueDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BoardGameLeagueDbContext")));
builder.Services.AddScoped<ILeagueRepository, EfLeagueRepository>();

builder.Services.AddHttpClient<BoardGameLeague.Services.IGeminiClient, BoardGameLeague.Services.GeminiClient>();
builder.Services.AddScoped<BoardGameLeague.Services.IChatAgentService, BoardGameLeague.Services.ChatAgentService>();

builder.Services.AddDefaultIdentity<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BoardGameLeagueDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SaveTokens = true;
        });
}
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BoardGameLeagueDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    await SeedDatabaseAsync(dbContext, logger);
    await SeedIdentityAsync(userManager, roleManager, logger);
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

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("hr"),
    SupportedCultures = new[] { new CultureInfo("hr"), new CultureInfo("en-US") },
    SupportedUICultures = new[] { new CultureInfo("hr"), new CultureInfo("en-US") }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

static async Task SeedDatabaseAsync(BoardGameLeagueDbContext context, ILogger logger)
{
    var hasExistingData = await context.Players.AnyAsync()
        || await context.Teams.AnyAsync()
        || await context.BoardGames.AnyAsync()
        || await context.Venues.AnyAsync()
        || await context.Tournaments.AnyAsync()
        || await context.Matches.AnyAsync();

    if (hasExistingData)
    {
        logger.LogInformation("Database already contains data; skipping sample data seeding.");
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

static async Task SeedIdentityAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
{
    var roles = new[] { "Admin", "Manager", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    var adminEmail = "admin@boardgameleague.local";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            OIB = "00000000000",
            JMBG = "0000000000000",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, "Admin123!");
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create default admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
