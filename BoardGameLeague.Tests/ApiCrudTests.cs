using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BoardGameLeague.Tests;

public class ApiCrudTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiCrudTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BoardGames_Crud_Works()
    {
        var createDto = new BoardGameCreateDto
        {
            Name = $"Test Game {Guid.NewGuid()}",
            Category = GameCategory.Party,
            MinPlayers = 2,
            MaxPlayers = 6,
            AveragePlayTimeMinutes = 45,
            Complexity = 1.5m
        };

        var created = await PostAuthorizedAsync<BoardGameCreateDto, BoardGameDto>("/api/boardgames", createDto);
        Assert.Equal(createDto.Name, created.Name);
        Assert.Equal(createDto.Category, created.Category);

        var updateDto = new BoardGameUpdateDto
        {
            Id = created.Id,
            Name = created.Name + " Updated",
            Category = created.Category,
            MinPlayers = created.MinPlayers,
            MaxPlayers = created.MaxPlayers,
            AveragePlayTimeMinutes = created.AveragePlayTimeMinutes,
            Complexity = created.Complexity
        };

        var updated = await PutAuthorizedAsync<BoardGameUpdateDto, BoardGameDto>($"/api/boardgames/{created.Id}", updateDto);
        Assert.Equal(updateDto.Name, updated.Name);

        var fetched = await _client.GetFromJsonAsync<BoardGameDto>($"/api/boardgames/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(updateDto.Name, fetched!.Name);

        var deleteResponse = await DeleteAuthorizedAsync($"/api/boardgames/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync($"/api/boardgames/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Players_Crud_Works()
    {
        var createDto = new PlayerCreateDto
        {
            Name = $"Test Player {Guid.NewGuid()}",
            Rating = 1500,
            JoinedDate = DateTime.Today.AddYears(-1),
            Country = "HR",
            Role = "Player"
        };

        var created = await PostAuthorizedAsync<PlayerCreateDto, PlayerDto>("/api/players", createDto);
        Assert.Equal(createDto.Name, created.Name);

        var updateDto = new PlayerUpdateDto
        {
            Id = created.Id,
            Name = created.Name + " Updated",
            Rating = created.Rating,
            JoinedDate = created.JoinedDate,
            Country = created.Country,
            Role = created.Role
        };

        var updated = await PutAuthorizedAsync<PlayerUpdateDto, PlayerDto>($"/api/players/{created.Id}", updateDto);
        Assert.Equal(updateDto.Name, updated.Name);

        var fetched = await _client.GetFromJsonAsync<PlayerDto>($"/api/players/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(updateDto.Name, fetched!.Name);

        var deleteResponse = await DeleteAuthorizedAsync($"/api/players/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync($"/api/players/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Teams_Crud_Works()
    {
        var createDto = new TeamCreateDto
        {
            Name = $"Test Team {Guid.NewGuid()}",
            Region = "Zagreb",
            FoundedDate = DateTime.Today.AddYears(-1),
            IsActive = true,
            TotalWins = 3,
            TotalLosses = 1,
            PlayerIds = new List<Guid>()
        };

        var created = await PostAuthorizedAsync<TeamCreateDto, TeamDto>("/api/teams", createDto);
        Assert.Equal(createDto.Name, created.Name);

        var updateDto = new TeamUpdateDto
        {
            Id = created.Id,
            Name = created.Name + " Updated",
            Region = created.Region,
            FoundedDate = created.FoundedDate,
            IsActive = created.IsActive,
            TotalWins = created.TotalWins,
            TotalLosses = created.TotalLosses,
            PlayerIds = createDto.PlayerIds
        };

        var updated = await PutAuthorizedAsync<TeamUpdateDto, TeamDto>($"/api/teams/{created.Id}", updateDto);
        Assert.Equal(updateDto.Name, updated.Name);

        var fetched = await _client.GetFromJsonAsync<TeamDto>($"/api/teams/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(updateDto.Name, fetched!.Name);

        var deleteResponse = await DeleteAuthorizedAsync($"/api/teams/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync($"/api/teams/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Venues_Crud_Works()
    {
        var createDto = new VenueCreateDto
        {
            Name = $"Test Venue {Guid.NewGuid()}",
            City = "Zagreb",
            Country = "HR",
            Capacity = 100,
            Indoor = true
        };

        var created = await PostAuthorizedAsync<VenueCreateDto, VenueDto>("/api/venues", createDto);
        Assert.Equal(createDto.Name, created.Name);

        var updateDto = new VenueUpdateDto
        {
            Id = created.Id,
            Name = created.Name + " Updated",
            City = created.City,
            Country = created.Country,
            Capacity = created.Capacity,
            Indoor = created.Indoor
        };

        var updated = await PutAuthorizedAsync<VenueUpdateDto, VenueDto>($"/api/venues/{created.Id}", updateDto);
        Assert.Equal(updateDto.Name, updated.Name);

        var fetched = await _client.GetFromJsonAsync<VenueDto>($"/api/venues/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(updateDto.Name, fetched!.Name);

        var deleteResponse = await DeleteAuthorizedAsync($"/api/venues/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync($"/api/venues/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Tournaments_Crud_Works()
    {
        var venues = await GetListAsync<VenueDto>("/api/venues");
        var teams = await GetListAsync<TeamDto>("/api/teams");
        Assert.NotEmpty(venues);
        Assert.True(teams.Count >= 2);

        var createDto = new TournamentCreateDto
        {
            Name = $"Test Tournament {Guid.NewGuid()}",
            Description = "Integration test tournament.",
            StartDate = DateTime.Today.AddDays(10),
            EndDate = DateTime.Today.AddDays(12),
            VenueId = venues[0].Id,
            IsOpen = true,
            TeamIds = teams.Take(2).Select(t => t.Id).ToList()
        };

        var created = await PostAuthorizedAsync<TournamentCreateDto, TournamentDto>("/api/tournaments", createDto);
        Assert.Equal(createDto.Name, created.Name);
        Assert.NotNull(created.Venue);

        var updateDto = new TournamentUpdateDto
        {
            Id = created.Id,
            Name = created.Name + " Updated",
            Description = created.Description,
            StartDate = created.StartDate,
            EndDate = created.EndDate,
            VenueId = created.Venue!.Id,
            IsOpen = created.IsOpen,
            TeamIds = createDto.TeamIds
        };

        var updated = await PutAuthorizedAsync<TournamentUpdateDto, TournamentDto>($"/api/tournaments/{created.Id}", updateDto);
        Assert.Equal(updateDto.Name, updated.Name);

        var fetched = await _client.GetFromJsonAsync<TournamentDto>($"/api/tournaments/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(updateDto.Name, fetched!.Name);

        var deleteResponse = await DeleteAuthorizedAsync($"/api/tournaments/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync($"/api/tournaments/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Matches_Crud_Works()
    {
        var tournaments = await GetListAsync<TournamentDto>("/api/tournaments");
        var teams = await GetListAsync<TeamDto>("/api/teams");
        var games = await GetListAsync<BoardGameDto>("/api/boardgames");
        Assert.NotEmpty(tournaments);
        Assert.True(teams.Count >= 2);
        Assert.NotEmpty(games);

        var createDto = new MatchCreateDto
        {
            TournamentId = tournaments[0].Id,
            TeamAId = teams[0].Id,
            TeamBId = teams[1].Id,
            GameId = games[0].Id,
            StartTime = DateTime.Today.AddDays(5).AddHours(10),
            ScoreA = 1,
            ScoreB = 2,
            IsCompleted = false
        };

        var created = await PostAuthorizedAsync<MatchCreateDto, MatchDto>("/api/matches", createDto);
        Assert.Equal(createDto.TournamentId, created.Tournament!.Id);
        Assert.Equal(createDto.TeamAId, created.TeamA!.Id);
        Assert.Equal(createDto.TeamBId, created.TeamB!.Id);

        var updateDto = new MatchUpdateDto
        {
            Id = created.Id,
            TournamentId = created.Tournament.Id,
            TeamAId = created.TeamA.Id,
            TeamBId = created.TeamB.Id,
            GameId = created.Game!.Id,
            StartTime = created.StartTime.AddHours(1),
            ScoreA = 2,
            ScoreB = 2,
            IsCompleted = true
        };

        var updated = await PutAuthorizedAsync<MatchUpdateDto, MatchDto>($"/api/matches/{created.Id}", updateDto);
        Assert.Equal(updateDto.ScoreA, updated.ScoreA);
        Assert.Equal(updateDto.IsCompleted, updated.IsCompleted);

        var fetched = await _client.GetFromJsonAsync<MatchDto>($"/api/matches/{created.Id}");
        Assert.NotNull(fetched);
        Assert.Equal(updateDto.ScoreA, fetched!.ScoreA);

        var deleteResponse = await DeleteAuthorizedAsync($"/api/matches/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var notFound = await _client.GetAsync($"/api/matches/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task PostEndpoints_RequireAuthentication()
    {
        var response = await _client.PostAsJsonAsync("/api/boardgames", new { Name = "Fail" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetNonExistingId_ReturnsNotFound_ForAllEntities()
    {
        var missingId = Guid.NewGuid();
        var routes = new[]
        {
            $"/api/boardgames/{missingId}",
            $"/api/players/{missingId}",
            $"/api/teams/{missingId}",
            $"/api/venues/{missingId}",
            $"/api/tournaments/{missingId}",
            $"/api/matches/{missingId}"
        };

        foreach (var route in routes)
        {
            var response = await _client.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task BoardGames_Post_InvalidModel_ReturnsBadRequest()
    {
        var invalidDto = new BoardGameCreateDto
        {
            Name = string.Empty,
            Category = GameCategory.Party,
            MinPlayers = 0,
            MaxPlayers = 0,
            AveragePlayTimeMinutes = 0,
            Complexity = 0m
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/boardgames")
        {
            Content = JsonContent.Create(invalidDto)
        };
        AddAuthHeader(request);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<List<T>> GetListAsync<T>(string route)
    {
        var list = await _client.GetFromJsonAsync<List<T>>(route);
        return list ?? throw new InvalidOperationException($"Unable to deserialize JSON response for route '{route}'.");
    }

    private async Task<TResponse> PostAuthorizedAsync<TRequest, TResponse>(string url, TRequest body, string roles = "Admin")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        AddAuthHeader(request, roles);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException($"Unable to deserialize JSON response for POST {url}.");
    }

    private async Task<TResponse> PutAuthorizedAsync<TRequest, TResponse>(string url, TRequest body, string roles = "Admin")
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(body)
        };
        AddAuthHeader(request, roles);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>()
            ?? throw new InvalidOperationException($"Unable to deserialize JSON response for PUT {url}.");
    }

    private async Task<HttpResponseMessage> DeleteAuthorizedAsync(string url, string roles = "Admin")
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        AddAuthHeader(request, roles);
        return await _client.SendAsync(request);
    }

    private static void AddAuthHeader(HttpRequestMessage request, string roles = "Admin")
    {
        request.Headers.Add("Authorization", $"Test {roles}");
    }
}
