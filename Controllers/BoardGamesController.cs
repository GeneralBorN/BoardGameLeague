using System;
using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLeague.Controllers
{
    [Authorize]
    [Route("games")]
    public class BoardGamesController : Controller
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly BoardGameLeagueDbContext _context;

        public BoardGamesController(ILeagueRepository leagueRepository, BoardGameLeagueDbContext context)
        {
            _leagueRepository = leagueRepository;
            _context = context;
        }

        [AllowAnonymous]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var games = await _leagueRepository.GetAllBoardGamesAsync();
            return View(games);
        }

        [AllowAnonymous]
        [Route("search")]
        public async Task<IActionResult> Search(string q)
        {
            var games = await _leagueRepository.GetAllBoardGamesAsync();
            if (!string.IsNullOrWhiteSpace(q))
            {
                games = games
                    .Where(g => g.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                             || g.Category.ToString().Contains(q, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_BoardGameCards", games);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("create")]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(Enum.GetValues<GameCategory>());
            return View(new BoardGame { AveragePlayTime = TimeSpan.FromMinutes(60) });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BoardGame game)
        {
            if (ModelState.IsValid)
            {
                game.Id = Guid.NewGuid();
                _context.BoardGames.Add(game);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(Enum.GetValues<GameCategory>());
            return View(game);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(Enum.GetValues<GameCategory>(), game.Category);
            return View(game);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("{id:guid}/edit")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(game, "", g => g.Name, g => g.Category, g => g.MinPlayers, g => g.MaxPlayers, g => g.AveragePlayTimeMinutes, g => g.Complexity))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = new SelectList(Enum.GetValues<GameCategory>(), game.Category);
            return View(game);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}/delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:guid}/delete"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            _context.BoardGames.Remove(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [Route("{id:guid}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var game = await _leagueRepository.GetBoardGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
    }
}
