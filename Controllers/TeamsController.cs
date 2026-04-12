using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    public class TeamsController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public TeamsController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var teams = await _leagueRepository.GetAllTeamsAsync();
            return View(teams);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var team = await _leagueRepository.GetTeamByIdAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }
    }
}
