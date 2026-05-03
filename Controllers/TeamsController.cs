using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    [Route("teams")]
    public class TeamsController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public TeamsController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var teams = await _leagueRepository.GetAllTeamsAsync();
            return View(teams);
        }

        [Route("{id:guid}")]
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
