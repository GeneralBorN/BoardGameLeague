using System;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BoardGameLeague.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;

        public VenuesController(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var venues = await _leagueRepository.GetAllVenuesAsync();
            return View(venues);
        }

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
