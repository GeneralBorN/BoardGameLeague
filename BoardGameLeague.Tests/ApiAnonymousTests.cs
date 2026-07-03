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
    [InlineData("/api/v1/boardgames")]
    [InlineData("/api/v1/players")]
    [InlineData("/api/v1/teams")]
    [InlineData("/api/v1/matches")]
    [InlineData("/api/v1/venues")]
    [InlineData("/api/v1/tournaments")]
    public async Task GetApiEndpoints_ShouldBeAccessibleWithoutAuthentication(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
    }

    [Fact]
    public async Task GetTournaments_ReturnsDtoList()
    {
        var tournaments = await _client.GetFromJsonAsync<List<TournamentDto>>("/api/v1/tournaments");

        Assert.NotNull(tournaments);
        Assert.NotEmpty(tournaments!);
        Assert.All(tournaments!, tournament => Assert.False(string.IsNullOrWhiteSpace(tournament.Name)));
    }

    [Fact]
    public async Task GetAttachmentsByTournamentId_ShouldBeAccessibleWithoutAuthentication()
    {
        // Arrange: Get a valid tournament ID first
        var tournaments = await _client.GetFromJsonAsync<List<TournamentDto>>("/api/v1/tournaments");
        Assert.NotNull(tournaments);
        Assert.NotEmpty(tournaments!);
        var tournamentId = tournaments![0].Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/attachments/{tournamentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var attachments = await response.Content.ReadFromJsonAsync<List<AttachmentDto>>();
        Assert.NotNull(attachments);
        // Depending on seed data, attachments might be empty, so no Assert.NotEmpty here.
    }
}
