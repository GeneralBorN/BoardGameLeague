using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Route("teams")]
    public class TeamsController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public TeamsController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var teams = await _leagueRepository.GetAllTeamsAsync();
            return View(teams);
        }

        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            var teams = await _leagueRepository.GetAllTeamsAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                teams = teams
                    .Where(t => t.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || t.Region.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_TeamCards", teams);
        }

        private async Task PopulatePlayersAsync(IEnumerable<Guid>? selectedIds = null)
        {
            var players = await _context.Players
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.Players = new MultiSelectList(players, "Id", "Name", selectedIds);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulatePlayersAsync();
            return View(new Team { FoundedDate = DateTime.Today, IsActive = true });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Team team)
        {
            if (ModelState.IsValid)
            {
                if (team.SelectedPlayerIds != null && team.SelectedPlayerIds.Any())
                {
                    team.Players = await _context.Players
                        .Where(p => team.SelectedPlayerIds.Contains(p.Id))
                        .ToListAsync();
                }

                team.Id = Guid.NewGuid();
                _context.Teams.Add(team);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulatePlayersAsync(team.SelectedPlayerIds);
            return View(team);
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (team == null)
            {
                return NotFound();
            }

            await PopulatePlayersAsync(team.Players.Select(p => p.Id));
            return View(team);
        }

        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var team = await _context.Teams
                .Include(t => t.Players)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (team == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(team, "", t => t.Name, t => t.Region, t => t.FoundedDate, t => t.IsActive, t => t.TotalWins, t => t.TotalLosses, t => t.SelectedPlayerIds))
            {
                if (team.SelectedPlayerIds != null)
                {
                    var selectedPlayers = await _context.Players
                        .Where(p => team.SelectedPlayerIds.Contains(p.Id))
                        .ToListAsync();
                    team.Players.Clear();
                    foreach (var player in selectedPlayers)
                    {
                        team.Players.Add(player);
                    }
                }

                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            await PopulatePlayersAsync(team.SelectedPlayerIds);
            return View(team);
        }

        [HttpGet("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            return View(team);
        }

        [HttpPost("{id:guid}/delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return NotFound();
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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
