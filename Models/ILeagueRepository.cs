using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoardGameLeague.Models
{
    public interface ILeagueRepository
    {
        Task<LeagueDashboardViewModel> GetDashboardAsync();
        Task<List<Team>> GetAllTeamsAsync();
        Task<Team?> GetTeamByIdAsync(Guid id);

        Task<List<Player>> GetAllPlayersAsync();
        Task<Player?> GetPlayerByIdAsync(Guid id);

        Task<List<BoardGame>> GetAllBoardGamesAsync();
        Task<BoardGame?> GetBoardGameByIdAsync(Guid id);

        Task<List<Tournament>> GetAllTournamentsAsync();
        Task<Tournament?> GetTournamentByIdAsync(Guid id);

        Task<List<Match>> GetAllMatchesAsync();
        Task<Match?> GetMatchByIdAsync(Guid id);

        Task<List<Venue>> GetAllVenuesAsync();
        Task<Venue?> GetVenueByIdAsync(Guid id);
    }
}
