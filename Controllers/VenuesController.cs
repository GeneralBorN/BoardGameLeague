using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    [Route("venues")]
    public class VenuesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public VenuesController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var venues = await _leagueRepository.GetAllVenuesAsync();
            return View(venues);
        }

        [Route("{id:guid}/location")]
        public async Task<IActionResult> Details(Guid id)
        {
            var venue = await _leagueRepository.GetVenueByIdAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }
    }
}
