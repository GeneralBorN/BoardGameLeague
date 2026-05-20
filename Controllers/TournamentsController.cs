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

        [HttpGet("lookup/venues")]
        public async Task<IActionResult> LookupVenues(string q)
        {
            var query = _context.Venues.AsQueryable();
            
            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(v => v.Name.ToLower().Contains(q.ToLower()));
            }
            
            var venues = await query
                .OrderBy(v => v.Name)
                .Select(v => new { id = v.Id, text = v.Name })
                .Take(15)
                .ToListAsync();

            return Json(venues);
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
        public async Task<IActionResult> Create([Bind("Name,Description,StartDate,EndDate,IsOpen")] Tournament tournament)
        {
            // parse date strings from the custom datepicker inputs if present
            if (Request.Form.TryGetValue("StartDate", out var startVal))
            {
                if (TryParseAnyDate(startVal, out var parsedStart))
                {
                    tournament.StartDate = parsedStart;
                    ModelState.Remove("StartDate");
                }
                else
                {
                    ModelState.AddModelError("StartDate", "Invalid start date format.");
                }
            }

            if (Request.Form.TryGetValue("EndDate", out var endVal))
            {
                if (TryParseAnyDate(endVal, out var parsedEnd))
                {
                    tournament.EndDate = parsedEnd;
                    ModelState.Remove("EndDate");
                }
                else
                {
                    ModelState.AddModelError("EndDate", "Invalid end date format.");
                }
            }

            if (Request.Form.TryGetValue("VenueId", out var venueIdValue) && Guid.TryParse(venueIdValue.ToString(), out var parsedVenueId) && parsedVenueId != Guid.Empty)
            {
                tournament.VenueId = parsedVenueId;
                ClearVenueIdModelState();
            }
            else if (tournament.VenueId == Guid.Empty && Request.Form.TryGetValue("VenueInput", out var venueInput) && !string.IsNullOrWhiteSpace(venueInput))
            {
                var normalizedVenueInput = venueInput.ToString().Trim();
                var matchedVenue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.Name.Equals(normalizedVenueInput, StringComparison.OrdinalIgnoreCase));
                if (matchedVenue != null)
                {
                    tournament.VenueId = matchedVenue.Id;
                    ClearVenueIdModelState();
                }
                else
                {
                    ModelState.AddModelError("VenueId", "Please select a venue from the list.");
                }
            }

            if (ModelState.IsValid)
            {
                tournament.Id = Guid.NewGuid();
                _context.Tournaments.Add(tournament);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var venue = await _context.Venues.FindAsync(tournament.VenueId);
            if (tournament.VenueId != Guid.Empty)
            {
                ViewBag.Venue = venue?.Name ?? string.Empty;
            }
            else if (Request.Form.TryGetValue("VenueInput", out var venueText))
            {
                ViewBag.Venue = venueText.ToString();
            }
            else
            {
                ViewBag.Venue = string.Empty;
            }

            await PopulateVenuesAsync(tournament.VenueId);
            return View(tournament);
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var tournament = await _context.Tournaments.Include(t => t.Venue).FirstOrDefaultAsync(t => t.Id == id);
            if (tournament == null)
            {
                return NotFound();
            }

            ViewBag.Venue = tournament.Venue?.Name ?? string.Empty;
            await PopulateVenuesAsync(tournament.VenueId);
            return View(tournament);
        }

        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var tournament = await _context.Tournaments.Include(t => t.Venue).FirstOrDefaultAsync(t => t.Id == id);
            if (tournament == null)
            {
                return NotFound();
            }
            // attempt to bind posted date strings from custom picker
            if (Request.Form.TryGetValue("StartDate", out var startVal))
            {
                if (TryParseAnyDate(startVal, out var parsedStart))
                {
                    tournament.StartDate = parsedStart;
                    ModelState.Remove("StartDate");
                }
                else
                {
                    ModelState.AddModelError("StartDate", "Invalid start date format.");
                }
            }

            if (Request.Form.TryGetValue("EndDate", out var endVal))
            {
                if (TryParseAnyDate(endVal, out var parsedEnd))
                {
                    tournament.EndDate = parsedEnd;
                    ModelState.Remove("EndDate");
                }
                else
                {
                    ModelState.AddModelError("EndDate", "Invalid end date format.");
                }
            }

            if (Request.Form.TryGetValue("VenueId", out var venueIdValue) && Guid.TryParse(venueIdValue.ToString(), out var parsedVenueId) && parsedVenueId != Guid.Empty)
            {
                tournament.VenueId = parsedVenueId;
                ClearVenueIdModelState();
            }
            else if (tournament.VenueId == Guid.Empty && Request.Form.TryGetValue("VenueInput", out var venueInput) && !string.IsNullOrWhiteSpace(venueInput))
            {
                var normalizedVenueInput = venueInput.ToString().Trim();
                var matchedVenue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.Name.Equals(normalizedVenueInput, StringComparison.OrdinalIgnoreCase));
                if (matchedVenue != null)
                {
                    tournament.VenueId = matchedVenue.Id;
                    ClearVenueIdModelState();
                }
                else
                {
                    ModelState.AddModelError("VenueId", "Please select a venue from the list.");
                }
            }

            if (await TryUpdateModelAsync(tournament, "", t => t.Name, t => t.Description, t => t.IsOpen))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            if (tournament.VenueId != Guid.Empty)
            {
                ViewBag.Venue = tournament.Venue?.Name ?? string.Empty;
            }
            else if (Request.Form.TryGetValue("VenueInput", out var venueText))
            {
                ViewBag.Venue = venueText.ToString();
            }
            else
            {
                ViewBag.Venue = string.Empty;
            }
            await PopulateVenuesAsync(tournament.VenueId);
            return View(tournament);
        }

        private void ClearVenueIdModelState()
        {
            ModelState.Remove("VenueId");
            ModelState.Remove("Tournament.VenueId");
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
