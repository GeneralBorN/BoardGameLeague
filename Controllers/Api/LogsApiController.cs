using BoardGameLeague.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Controllers.Api
{
    // Logs can contain stack traces and request details, so this is Admin-only - same
    // gating as the /Admin/Logs page, which is a thin wrapper around the same service.
    [Route("api/logs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LogsApiController : ControllerBase
    {
        private readonly ILogQueryService _logQueryService;

        public LogsApiController(ILogQueryService logQueryService)
        {
            _logQueryService = logQueryService;
        }

        [HttpGet]
        public ActionResult<LogQueryResult> Get(
            [FromQuery] string? minLevel,
            [FromQuery] string? search,
            [FromQuery] string? date,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = _logQueryService.Query(new LogQueryOptions
            {
                MinLevel = ParseLevel(minLevel),
                Search = search,
                Date = ParseDate(date),
                Page = page,
                PageSize = pageSize
            });

            return Ok(result);
        }

        [HttpGet("dates")]
        public ActionResult<IReadOnlyList<DateOnly>> GetDates()
        {
            return Ok(_logQueryService.GetAvailableDates());
        }

        private static LogLevel? ParseLevel(string? value) =>
            !string.IsNullOrWhiteSpace(value) && Enum.TryParse<LogLevel>(value, true, out var level) ? level : null;

        private static DateOnly? ParseDate(string? value) =>
            !string.IsNullOrWhiteSpace(value) && DateOnly.TryParse(value, out var date) ? date : null;
    }
}
