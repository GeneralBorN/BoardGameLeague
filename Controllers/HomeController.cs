using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BoardGameLeague.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = await LeagueDashboardViewModelFactoryAsync();
            return View(model);
        }

        public async Task<IActionResult> Teams()
        {
            var model = await LeagueDashboardViewModelFactoryAsync();
            return View(model.AllTeams);
        }

        public async Task<IActionResult> Tournaments()
        {
            var model = await LeagueDashboardViewModelFactoryAsync();
            return View(model.AllTournaments);
        }

        public async Task<IActionResult> Matches()
        {
            var model = await LeagueDashboardViewModelFactoryAsync();
            return View(model.AllMatches);
        }

        public async Task<IActionResult> BoardGames()
        {
            var model = await LeagueDashboardViewModelFactoryAsync();
            return View(model.AllBoardGames);
        }

        public async Task<IActionResult> Players()
        {
            var model = await LeagueDashboardViewModelFactoryAsync();
            var allPlayers = model.AllTeams.SelectMany(t => t.Players).Distinct().ToList();
            return View(allPlayers);
        }

        private async Task<BoardGameLeague.Models.LeagueDashboardViewModel> LeagueDashboardViewModelFactoryAsync()
        {
            return await BoardGameLeague.Models.LeagueDataFactory.CreateSampleLeagueAsync();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
