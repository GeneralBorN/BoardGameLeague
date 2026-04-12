using BoardGameLeague.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BoardGameLeague.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ILeagueRepository _leagueRepository;

        public HomeController(ILogger<HomeController> logger, ILeagueRepository leagueRepository)
        {
            _logger = logger;
            _leagueRepository = leagueRepository;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _leagueRepository.GetDashboardAsync();
            return View(model);
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
