using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BoardGameLeague.Models
{
    public class MockLeagueRepository : ILeagueRepository
    {
        private readonly LeagueDashboardViewModel _dashboard;

        public MockLeagueRepository()
        {
            _dashboard = LeagueDataFactory.CreateSampleLeagueAsync().GetAwaiter().GetResult();
        }

        public Task<LeagueDashboardViewModel> GetDashboardAsync()
        {
            return Task.FromResult(_dashboard);
        }

        public Task<List<Team>> GetAllTeamsAsync()
        {
            return Task.FromResult(_dashboard.AllTeams);
        }

        public Task<Team?> GetTeamByIdAsync(Guid id)
        {
            return Task.FromResult(_dashboard.AllTeams.FirstOrDefault(t => t.Id == id));
        }

        public Task<List<Player>> GetAllPlayersAsync()
        {
            var players = _dashboard.AllTeams.SelectMany(t => t.Players).Distinct().ToList();
            return Task.FromResult(players);
        }

        public Task<Player?> GetPlayerByIdAsync(Guid id)
        {
            var player = _dashboard.AllTeams.SelectMany(t => t.Players).FirstOrDefault(p => p.Id == id);
            return Task.FromResult(player);
        }

        public Task<List<BoardGame>> GetAllBoardGamesAsync()
        {
            return Task.FromResult(_dashboard.AllBoardGames);
        }

        public Task<BoardGame?> GetBoardGameByIdAsync(Guid id)
        {
            return Task.FromResult(_dashboard.AllBoardGames.FirstOrDefault(g => g.Id == id));
        }

        public Task<List<Tournament>> GetAllTournamentsAsync()
        {
            return Task.FromResult(_dashboard.AllTournaments);
        }

        public Task<Tournament?> GetTournamentByIdAsync(Guid id)
        {
            return Task.FromResult(_dashboard.AllTournaments.FirstOrDefault(t => t.Id == id));
        }

        public Task<List<Match>> GetAllMatchesAsync()
        {
            return Task.FromResult(_dashboard.AllMatches);
        }

        public Task<Match?> GetMatchByIdAsync(Guid id)
        {
            return Task.FromResult(_dashboard.AllMatches.FirstOrDefault(m => m.Id == id));
        }

        public Task<List<Venue>> GetAllVenuesAsync()
        {
            var venues = _dashboard.AllTournaments.Select(t => t.Venue).Distinct().ToList();
            return Task.FromResult(venues);
        }

        public Task<Venue?> GetVenueByIdAsync(Guid id)
        {
            return Task.FromResult(_dashboard.AllTournaments.Select(t => t.Venue).FirstOrDefault(v => v.Id == id));
        }
    }
}
