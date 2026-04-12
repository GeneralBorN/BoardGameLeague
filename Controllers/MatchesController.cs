using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    public class MatchesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public MatchesController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var matches = await _leagueRepository.GetAllMatchesAsync();
            return View(matches);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var match = await _leagueRepository.GetMatchByIdAsync(id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }
    }
}
