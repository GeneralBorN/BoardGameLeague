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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
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
    // Clear existing data
    context.Matches.RemoveRange(context.Matches);
    context.Tournaments.RemoveRange(context.Tournaments);
    context.Venues.RemoveRange(context.Venues);
    context.Teams.RemoveRange(context.Teams);
    context.BoardGames.RemoveRange(context.BoardGames);
    context.Players.RemoveRange(context.Players);
    await context.SaveChangesAsync();

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
