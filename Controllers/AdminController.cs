using System.Linq;
using System.Threading.Tasks;
using BoardGameLeague.Logging;
using BoardGameLeague.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogQueryService _logQueryService;

        public AdminController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogQueryService logQueryService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logQueryService = logQueryService;
        }

        public IActionResult Logs(string? minLevel, string? search, string? date, int page = 1)
        {
            LogLevel? parsedLevel = !string.IsNullOrWhiteSpace(minLevel) && Enum.TryParse<LogLevel>(minLevel, true, out var level)
                ? level
                : null;
            DateOnly? parsedDate = !string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d)
                ? d
                : null;

            var result = _logQueryService.Query(new LogQueryOptions
            {
                MinLevel = parsedLevel,
                Search = search,
                Date = parsedDate,
                Page = page,
                PageSize = 50
            });

            ViewBag.AvailableDates = _logQueryService.GetAvailableDates();
            ViewBag.SelectedMinLevel = minLevel ?? string.Empty;
            ViewBag.SelectedSearch = search ?? string.Empty;
            ViewBag.SelectedDate = date ?? string.Empty;

            return View(result);
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = users.Select(user => new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Roles = _userManager.GetRolesAsync(user).Result
            }).ToList();

            return View(userViewModels);
        }

        public async Task<IActionResult> EditUserRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                Roles = userRoles,
                AllRoles = new MultiSelectList(allRoles.Select(r => r.Name))
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRoles(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles ?? new string[] { };

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to add roles");
                return View(model);
            }

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to remove roles");
                return View(model);
            }

            return RedirectToAction("Index");
        }
    }
}
