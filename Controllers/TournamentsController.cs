using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Route("tournaments")]
    public class TournamentsController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public TournamentsController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var tournaments = await _leagueRepository.GetAllTournamentsAsync();
            return View(tournaments);
        }

        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            var tournaments = await _leagueRepository.GetAllTournamentsAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                tournaments = tournaments
                    .Where(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || t.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || t.Venue.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_TournamentCards", tournaments);
        }

        private async Task PopulateVenuesAsync(Guid? selectedId = null)
        {
            var venues = await _leagueRepository.GetAllVenuesAsync();
            ViewBag.Venues = new SelectList(venues, "Id", "Name", selectedId);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateVenuesAsync();
            return View(new Tournament { StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(2), IsOpen = true });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tournament tournament)
        {
            // parse date strings from the custom datepicker inputs if present
            if (Request.Form.TryGetValue("StartDate", out var startVal))
            {
                if (TryParseAnyDate(startVal, out var dt)) tournament.StartDate = dt;
                else ModelState.AddModelError("StartDate", "Invalid start date format.");
                // remove any previous modelstate error created by binder if we successfully parsed
            if (TryParseAnyDate(startVal, out var parsedStart))
            {
                ModelState.Remove("StartDate");
                tournament.StartDate = parsedStart;
            }
            }

            if (Request.Form.TryGetValue("EndDate", out var endVal))
            {
                if (TryParseAnyDate(endVal, out var dt2)) tournament.EndDate = dt2;
                else ModelState.AddModelError("EndDate", "Invalid end date format.");
            if (TryParseAnyDate(endVal, out var parsedEnd))
            {
                ModelState.Remove("EndDate");
                tournament.EndDate = parsedEnd;
            }
            }

            if (ModelState.IsValid)
            {
                tournament.Id = Guid.NewGuid();
                _context.Tournaments.Add(tournament);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateVenuesAsync(tournament.VenueId);
            return View(tournament);
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            await PopulateVenuesAsync(tournament.VenueId);
            return View(tournament);
        }

        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            // attempt to bind posted date strings from custom picker
            if (Request.Form.TryGetValue("StartDate", out var startVal))
            {
                if (TryParseAnyDate(startVal, out var dt)) tournament.StartDate = dt;
                else ModelState.AddModelError("StartDate", "Invalid start date format.");
            }

            if (Request.Form.TryGetValue("EndDate", out var endVal))
            {
                if (TryParseAnyDate(endVal, out var dt2)) tournament.EndDate = dt2;
                else ModelState.AddModelError("EndDate", "Invalid end date format.");
            }

            if (await TryUpdateModelAsync(tournament, "", t => t.Name, t => t.Description, t => t.VenueId, t => t.IsOpen))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            await PopulateVenuesAsync(tournament.VenueId);
            return View(tournament);
        }

        private static bool TryParseAnyDate(string? text, out DateTime value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var candidates = new[] { "dd.MM.yyyy HH:mm", "MM/dd/yyyy HH:mm", "yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm" };
            foreach (var fmt in candidates)
            {
                if (DateTime.TryParseExact(text, fmt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                {
                    value = dt; return true;
                }
            }

            // try culture-aware parse (hr locale)
            if (DateTime.TryParse(text, System.Globalization.CultureInfo.GetCultureInfo("hr"), System.Globalization.DateTimeStyles.None, out var dt2))
            {
                value = dt2; return true;
            }

            if (DateTime.TryParse(text, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out var dt3))
            {
                value = dt3; return true;
            }

            return false;
        }

        [HttpGet("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            return View(tournament);
        }

        [HttpPost("{id:guid}/delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tournament = await _context.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Route("{id:guid}/schedule")]
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
