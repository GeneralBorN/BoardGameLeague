using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    public class BoardGamesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public BoardGamesController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _leagueRepository.GetAllBoardGamesAsync();
            return View(games);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var game = await _leagueRepository.GetBoardGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
    }
}
