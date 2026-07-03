using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Logging
{
    public class LogQueryOptions
    {
        public LogLevel? MinLevel { get; set; }
        public string? Search { get; set; }
        public DateOnly? Date { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class LogQueryResult
    {
        public List<LogEntry> Entries { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public interface ILogQueryService
    {
        LogQueryResult Query(LogQueryOptions options);
        IReadOnlyList<DateOnly> GetAvailableDates();
    }
}
