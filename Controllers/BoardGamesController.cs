using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    [Route("games")]
    public class BoardGamesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public BoardGamesController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var games = await _leagueRepository.GetAllBoardGamesAsync();
            return View(games);
        }

        [Route("{id:guid}")]
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
