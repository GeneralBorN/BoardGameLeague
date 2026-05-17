using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Route("players")]
    public class PlayersController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public PlayersController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var players = await _leagueRepository.GetAllPlayersAsync();
            return View(players);
        }

        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            var players = await _leagueRepository.GetAllPlayersAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                players = players
                    .Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || p.Country.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || p.Role.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_PlayerCards", players);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new Player { JoinedDate = DateTime.Today });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Player player)
        {
            if (ModelState.IsValid)
            {
                player.Id = Guid.NewGuid();
                _context.Players.Add(player);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(player);
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return View(player);
        }

        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(player, "", p => p.Name, p => p.Rating, p => p.JoinedDate, p => p.Country, p => p.Role))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(player);
        }

        [HttpGet("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return View(player);
        }

        [HttpPost("{id:guid}/delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Route("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var player = await _leagueRepository.GetPlayerByIdAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            // Load all teams and build SelectList in memory after player is loaded
            var allTeams = await _context.Teams.OrderBy(t => t.Name).ToListAsync();
            var selectedTeamIds = player.Teams.Select(t => t.Id).ToHashSet();
            ViewBag.AllTeams = allTeams
                .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name,
                    Selected = selectedTeamIds.Contains(t.Id)
                })
                .ToList();

            return View(player);
        }

        [HttpPost("{id:guid}/add-to-teams")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToTeams(Guid id, List<Guid> SelectedTeamIds)
        {
            var player = await _context.Players
                .Include(p => p.Teams)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound();

            SelectedTeamIds = SelectedTeamIds ?? new List<Guid>();

            // remove memberships not selected
            var toRemove = player.Teams.Where(t => !SelectedTeamIds.Contains(t.Id)).ToList();
            foreach (var t in toRemove)
            {
                player.Teams.Remove(t);
            }

            // add new memberships
            var selectedTeams = await _context.Teams.Where(t => SelectedTeamIds.Contains(t.Id)).ToListAsync();
            foreach (var t in selectedTeams)
            {
                if (!player.Teams.Any(pt => pt.Id == t.Id))
                {
                    player.Teams.Add(t);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
