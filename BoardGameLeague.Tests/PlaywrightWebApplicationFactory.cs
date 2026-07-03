using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BoardGameLeague.Tests;

// Playwright drives a real browser over HTTP, so unlike CustomWebApplicationFactory
// (which uses the in-memory TestServer) this factory binds a real Kestrel listener
// on an ephemeral port. The DB seeding and header-based "Test" auth scheme are the
// same trick CustomWebApplicationFactory uses for the API tests, reused here so the
// browser can authenticate by sending an Authorization header instead of driving the
// real Identity login form.
public class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ServerAddress { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls("http://127.0.0.1:0");

        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(
                d => d.ServiceType == typeof(DbContextOptions<BoardGameLeagueDbContext>)).ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            var inMemoryDatabaseName = Guid.NewGuid().ToString();
            services.AddDbContext<BoardGameLeagueDbContext>(options =>
                options.UseInMemoryDatabase(inMemoryDatabaseName));

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BoardGameLeagueDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var sampleLeague = LeagueDataFactory.CreateSampleLeagueAsync().GetAwaiter().GetResult();
            context.Players.AddRange(sampleLeague.AllTeams.SelectMany(t => t.Players).Distinct());
            context.BoardGames.AddRange(sampleLeague.AllBoardGames);
            context.Venues.AddRange(sampleLeague.AllTournaments.Select(t => t.Venue).Distinct());
            context.Teams.AddRange(sampleLeague.AllTeams);
            context.Tournaments.AddRange(sampleLeague.AllTournaments);
            context.Matches.AddRange(sampleLeague.AllMatches);
            context.SaveChanges();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // WebApplicationFactory normally wires up an in-memory TestServer, which Playwright
        // (a real out-of-process browser) can't reach. Building a throwaway TestServer-backed
        // host first satisfies the base class's internal bootstrapping, then a second host is
        // built with Kestrel explicitly forced on, and that's the one actually started and
        // returned as the "real" server whose address Playwright navigates to.
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());
        var kestrelHost = builder.Build();
        kestrelHost.Start();

        ServerAddress = kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        testHost.Start();
        return testHost;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var headerValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var header = headerValues.ToString();
            if (!header.StartsWith("Test ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var roleSegment = header.Substring("Test ".Length);
            var roles = roleSegment.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testuser"),
                new Claim(ClaimTypes.Name, "Test User")
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
