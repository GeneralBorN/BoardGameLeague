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
    public class PlayersController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public PlayersController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var players = await _leagueRepository.GetAllPlayersAsync();
            return View(players);
        }

        [AllowAnonymous]
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

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View(new Player { JoinedDate = DateTime.Today });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
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

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return View(player);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Player player)
        {
            if (id != player.Id)
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            return View(player);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, ActionName("Delete")]
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

        [AllowAnonymous]
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
    }
}
