using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public TournamentsController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var tournaments = await _leagueRepository.GetAllTournamentsAsync();
            return View(tournaments);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var tournament = await _leagueRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            return View(tournament);
        }
    }
}
