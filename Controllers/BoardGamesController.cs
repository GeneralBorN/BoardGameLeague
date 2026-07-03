using System;
using System.Globalization;
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
        public async Task<IActionResult> Index()
        {
            var games = await _leagueRepository.GetAllBoardGamesAsync();
            return View(games);
        }

        [AllowAnonymous]
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
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(Enum.GetValues<GameCategory>());
            return View(new BoardGame { AveragePlayTime = TimeSpan.FromMinutes(60), Complexity = 0m });
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BoardGame game)
        {
            ApplyComplexity(game);

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BoardGame game)
        {
            if (id != game.Id)
            {
                return NotFound();
            }

            var gameToUpdate = await _context.BoardGames.FindAsync(id);
            if (gameToUpdate == null)
            {
                return NotFound();
            }

            ApplyComplexity(gameToUpdate);

            if (await TryUpdateModelAsync(gameToUpdate, "", g => g.Name, g => g.Category, g => g.MinPlayers, g => g.MaxPlayers, g => g.AveragePlayTimeMinutes))
            {
                if (ModelState.IsValid)
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = new SelectList(Enum.GetValues<GameCategory>(), gameToUpdate.Category);
            return View(gameToUpdate);
        }

        private void ApplyComplexity(BoardGame game)
        {
            if (Request.Form.TryGetValue("Complexity", out var complexityValue) && TryParseComplexity(complexityValue, out var parsed))
            {
                game.Complexity = parsed;
                ModelState.Remove("Complexity");
                if (parsed < 0.1m || parsed > 5.0m)
                {
                    ModelState.AddModelError("Complexity", "The field Complexity must be between 0.1 and 5.");
                }
            }
        }

        private static bool TryParseComplexity(string? text, out decimal value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            return decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var game = await _context.BoardGames.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost, ActionName("Delete")]
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
