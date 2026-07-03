using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Authorize]
    public class VenuesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public VenuesController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var venues = await _leagueRepository.GetAllVenuesAsync();
            return View(venues);
        }

        [AllowAnonymous]
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

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View(new Venue());
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
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

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Venue venue)
        {
            if (id != venue.Id)
            {
                return NotFound();
            }

            var venueToUpdate = await _context.Venues.FindAsync(id);
            if (venueToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(venueToUpdate, "", v => v.Name, v => v.City, v => v.Country, v => v.Capacity, v => v.Indoor))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(venueToUpdate);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, ActionName("Delete")]
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

        [AllowAnonymous]
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
