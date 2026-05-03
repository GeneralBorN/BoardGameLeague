using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Models
{
    public class EfLeagueRepository : ILeagueRepository
    {
        private readonly BoardGameLeagueDbContext _context;

        public EfLeagueRepository(BoardGameLeagueDbContext context)
        {
            _context = context;
        }

        public async Task<LeagueDashboardViewModel> GetDashboardAsync()
        {
            var teams = await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Tournaments)
                .AsNoTracking()
                .ToListAsync();

            var tournaments = await _context.Tournaments
                .Include(t => t.Venue)
                .Include(t => t.Teams)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Game)
                .AsNoTracking()
                .ToListAsync();

            var matches = await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.Game)
                .AsNoTracking()
                .ToListAsync();

            var boardGames = await _context.BoardGames
                .AsNoTracking()
                .ToListAsync();

            var topTeams = teams
                .OrderByDescending(t => t.Players?.Any() == true ? t.Players.Average(p => p.Rating) : 0)
                .ThenByDescending(t => t.WinRate)
                .Take(3)
                .ToList();

            var highWinRate = teams
                .Where(t => t.WinRate >= 0.60)
                .OrderByDescending(t => t.WinRate)
                .ToList();

            var upcoming = tournaments
                .Where(t => t.StartDate > DateTime.Today)
                .OrderBy(t => t.StartDate)
                .ToList();

            var popularGames = matches
                .Where(m => m.Game != null)
                .GroupBy(m => m.Game.Name)
                .Select(g => new { GameName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Select(x => x.GameName)
                .ToList();

            var matchesByVenue = matches
                .Where(m => m.Tournament?.Venue != null)
                .GroupBy(m => m.Tournament.Venue.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            return new LeagueDashboardViewModel
            {
                AllTeams = teams,
                AllTournaments = tournaments,
                AllMatches = matches,
                AllBoardGames = boardGames,
                TopTeamsByAveragePlayerRating = topTeams,
                HighWinRateTeams = highWinRate,
                UpcomingTournaments = upcoming,
                PopularGameNames = popularGames,
                MatchesByVenue = matchesByVenue
            };
        }

        public async Task<List<Team>> GetAllTeamsAsync()
        {
            return await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Tournaments)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Team?> GetTeamByIdAsync(Guid id)
        {
            return await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.Tournaments)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Player>> GetAllPlayersAsync()
        {
            return await _context.Players
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Player?> GetPlayerByIdAsync(Guid id)
        {
            return await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<BoardGame>> GetAllBoardGamesAsync()
        {
            return await _context.BoardGames
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BoardGame?> GetBoardGameByIdAsync(Guid id)
        {
            return await _context.BoardGames
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<List<Tournament>> GetAllTournamentsAsync()
        {
            return await _context.Tournaments
                .Include(t => t.Venue)
                .Include(t => t.Teams)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Game)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.TeamA)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.TeamB)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Tournament?> GetTournamentByIdAsync(Guid id)
        {
            return await _context.Tournaments
                .Include(t => t.Venue)
                .Include(t => t.Teams)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.Game)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.TeamA)
                .Include(t => t.Matches)
                    .ThenInclude(m => m.TeamB)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Match>> GetAllMatchesAsync()
        {
            return await _context.Matches
                .Include(m => m.Tournament)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.Game)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Match?> GetMatchByIdAsync(Guid id)
        {
            return await _context.Matches
                .Include(m => m.Tournament)
                    .ThenInclude(t => t.Venue)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.Game)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<Venue>> GetAllVenuesAsync()
        {
            return await _context.Venues
                .Include(v => v.Tournaments)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Venue?> GetVenueByIdAsync(Guid id)
        {
            return await _context.Venues
                .Include(v => v.Tournaments)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }
    }
}
