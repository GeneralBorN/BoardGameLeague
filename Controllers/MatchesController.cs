using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Route("matches")]
    public class MatchesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public MatchesController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var matches = await _leagueRepository.GetAllMatchesAsync();
            return View(matches);
        }

        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            var matches = await _leagueRepository.GetAllMatchesAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                matches = matches
                    .Where(m => m.TeamA.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || m.TeamB.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || m.Tournament.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || m.Game.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_MatchCards", matches);
        }

        [HttpGet("lookup/teams")]
        public async Task<IActionResult> LookupTeams(string q)
        {
            var teams = await _context.Teams
                .Where(t => string.IsNullOrEmpty(q) || t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .Select(t => new { id = t.Id, text = t.Name })
                .Take(15)
                .ToListAsync();

            return Json(teams);
        }

        [HttpGet("lookup/games")]
        public async Task<IActionResult> LookupGames(string q)
        {
            var games = await _context.BoardGames
                .Where(g => string.IsNullOrEmpty(q) || g.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(g => g.Name)
                .Select(g => new { id = g.Id, text = g.Name })
                .Take(15)
                .ToListAsync();

            return Json(games);
        }

        [HttpGet("lookup/tournaments")]
        public async Task<IActionResult> LookupTournaments(string q)
        {
            var tournaments = await _context.Tournaments
                .Where(t => string.IsNullOrEmpty(q) || t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .Select(t => new { id = t.Id, text = t.Name })
                .Take(15)
                .ToListAsync();

            return Json(tournaments);
        }

        private async Task PopulateLookupFieldsAsync(Match match)
        {
            ViewBag.TeamA = match.TeamA?.Name ?? string.Empty;
            ViewBag.TeamB = match.TeamB?.Name ?? string.Empty;
            ViewBag.Game = match.Game?.Name ?? string.Empty;
            ViewBag.Tournament = match.Tournament?.Name ?? string.Empty;
        }

        private async Task PopulateSelectListsAsync()
        {
            ViewBag.TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            ViewBag.GameOptions = await _context.BoardGames
                .OrderBy(g => g.Name)
                .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name })
                .ToListAsync();

            ViewBag.TournamentOptions = await _context.Tournaments
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var match = new Match { StartTime = DateTime.Today.AddHours(10) };
            await PopulateLookupFieldsAsync(match);
            await PopulateSelectListsAsync();
            return View(match);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Match match)
        {
            await PopulateLookupFieldsAsync(match);
            await PopulateSelectListsAsync();
            if (ModelState.IsValid)
            {
                if (match.TeamAId == match.TeamBId)
                {
                    ModelState.AddModelError(string.Empty, "Team A and Team B must be different.");
                }
                else
                {
                    match.Id = Guid.NewGuid();
                    _context.Matches.Add(match);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(match);
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var match = await _context.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.Game)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match == null)
            {
                return NotFound();
            }

            await PopulateLookupFieldsAsync(match);
            await PopulateSelectListsAsync();
            return View(match);
        }

        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(match, "", m => m.TournamentId, m => m.TeamAId, m => m.TeamBId, m => m.GameId, m => m.StartTime, m => m.ScoreA, m => m.ScoreB, m => m.IsCompleted))
            {
                await PopulateLookupFieldsAsync(match);
                await PopulateSelectListsAsync();
                if (match.TeamAId == match.TeamBId)
                {
                    ModelState.AddModelError(string.Empty, "Team A and Team B must be different.");
                }
                else if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(match);
        }

        [HttpGet("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var match = await _context.Matches
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .Include(m => m.Game)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        [HttpPost("{id:guid}/delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }

            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
