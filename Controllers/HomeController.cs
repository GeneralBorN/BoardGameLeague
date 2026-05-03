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

        [Route("")]
        [Route("dashboard")]
        public async Task<IActionResult> Index()
        {
            var model = await _leagueRepository.GetDashboardAsync();
            return View(model);
        }

        [Route("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [Route("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
