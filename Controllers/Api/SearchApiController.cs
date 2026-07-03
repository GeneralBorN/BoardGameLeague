using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLeague.Controllers.Api
{
    [Route("api/search")]
    [ApiController]
    public class SearchApiController : ControllerBase
    {
        private readonly ILeagueRepository _leagueRepository;

        public SearchApiController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
            {
                return Ok(new { });
            }

            var tournaments = (await _leagueRepository.GetAllTournamentsAsync())
                .Where(t => t.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .Select(t => new { t.Id, t.Name, Type = "Tournament" });

            var players = (await _leagueRepository.GetAllPlayersAsync())
                .Where(p => p.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .Select(p => new { p.Id, p.Name, Type = "Player" });

            var teams = (await _leagueRepository.GetAllTeamsAsync())
                .Where(t => t.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .Select(t => new { t.Id, t.Name, Type = "Team" });

            var boardGames = (await _leagueRepository.GetAllBoardGamesAsync())
                .Where(b => b.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .Select(b => new { b.Id, b.Name, Type = "BoardGame" });

            var venues = (await _leagueRepository.GetAllVenuesAsync())
                .Where(v => v.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .Select(v => new { v.Id, v.Name, Type = "Venue" });

            var matches = (await _leagueRepository.GetAllMatchesAsync())
                .Where(m => m.TeamA.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                         || m.TeamB.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                         || m.Tournament.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)
                         || m.Game.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                .Select(m => new { m.Id, Name = $"{m.TeamA.Name} - {m.TeamB.Name}", Type = "Match" });

            var results = new
            {
                Tournaments = tournaments.Take(5),
                Players = players.Take(5),
                Teams = teams.Take(5),
                BoardGames = boardGames.Take(5),
                Venues = venues.Take(5),
                Matches = matches.Take(5)
            };

            return Ok(results);
        }
    }
}
