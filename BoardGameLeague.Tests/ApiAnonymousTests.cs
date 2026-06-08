using System.Net;
using System.Net.Http.Json;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BoardGameLeague.Tests;

public class ApiAnonymousTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiAnonymousTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/boardgames")]
    [InlineData("/api/players")]
    [InlineData("/api/teams")]
    [InlineData("/api/matches")]
    [InlineData("/api/venues")]
    [InlineData("/api/tournaments")]
    public async Task GetApiEndpoints_ShouldBeAccessibleWithoutAuthentication(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task GetTournaments_ReturnsDtoList()
    {
        var tournaments = await _client.GetFromJsonAsync<List<TournamentDto>>("/api/tournaments");

        Assert.NotNull(tournaments);
        Assert.NotEmpty(tournaments!);
        Assert.All(tournaments!, tournament => Assert.False(string.IsNullOrWhiteSpace(tournament.Name)));
    }

    [Fact]
    public async Task GetTournamentAttachments_ReturnsHtmlWithoutAuthentication()
    {
        var tournaments = await _client.GetFromJsonAsync<List<TournamentDto>>("/api/tournaments");
        Assert.NotNull(tournaments);
        Assert.NotEmpty(tournaments!);

        var tournamentId = tournaments![0].Id;
        var response = await _client.GetAsync($"/tournaments/{tournamentId}/attachments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<th>File</th>", html);
        Assert.Contains("<th>Size</th>", html);
        Assert.Contains("<th>Uploaded</th>", html);
    }
}
