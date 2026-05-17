using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Route("venues")]
    public class VenuesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public VenuesController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var venues = await _leagueRepository.GetAllVenuesAsync();
            return View(venues);
        }

        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            var venues = await _leagueRepository.GetAllVenuesAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                venues = venues
                    .Where(v => v.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || v.City.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || v.Country.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_VenueCards", venues);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new Venue());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue)
        {
            if (ModelState.IsValid)
            {
                venue.Id = Guid.NewGuid();
                _context.Venues.Add(venue);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(venue);
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }

        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(venue, "", v => v.Name, v => v.City, v => v.Country, v => v.Capacity, v => v.Indoor))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(venue);
        }

        [HttpGet("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }

        [HttpPost("{id:guid}/delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
