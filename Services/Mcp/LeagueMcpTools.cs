using System.ComponentModel;
using BoardGameLeague.Models;
using ModelContextProtocol.Server;

namespace BoardGameLeague.Services.Mcp;

[McpServerToolType]
public static class LeagueMcpTools
{
    [McpServerTool, Description("Gets league-wide standings, top teams and recent match summary.")]
    public static async Task<LeagueDashboardViewModel> GetDashboard(ILeagueRepository repository)
        => await repository.GetDashboardAsync();

    [McpServerTool, Description("Lists all players in the league.")]
    public static async Task<List<Player>> GetPlayers(ILeagueRepository repository)
        => await repository.GetAllPlayersAsync();

    [McpServerTool, Description("Lists all teams in the league.")]
    public static async Task<List<Team>> GetTeams(ILeagueRepository repository)
        => await repository.GetAllTeamsAsync();

    [McpServerTool, Description("Lists all board games tracked by the league.")]
    public static async Task<List<BoardGame>> GetBoardGames(ILeagueRepository repository)
        => await repository.GetAllBoardGamesAsync();

    [McpServerTool, Description("Lists all venues used for tournaments.")]
    public static async Task<List<Venue>> GetVenues(ILeagueRepository repository)
        => await repository.GetAllVenuesAsync();

    [McpServerTool, Description("Lists all tournaments.")]
    public static async Task<List<Tournament>> GetTournaments(ILeagueRepository repository)
        => await repository.GetAllTournamentsAsync();

    [McpServerTool, Description("Lists all matches played across all tournaments.")]
    public static async Task<List<Match>> GetMatches(ILeagueRepository repository)
        => await repository.GetAllMatchesAsync();
}
