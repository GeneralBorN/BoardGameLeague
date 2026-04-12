using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    public class PlayersController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public PlayersController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var players = await _leagueRepository.GetAllPlayersAsync();
            return View(players);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var player = await _leagueRepository.GetPlayerByIdAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return View(player);
        }
    }
}
