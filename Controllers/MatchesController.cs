using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    [Route("matches")]
    public class MatchesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public MatchesController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var matches = await _leagueRepository.GetAllMatchesAsync();
            return View(matches);
        }

        [Route("{id:guid}/result")]
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
