using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BoardGameLeague.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
