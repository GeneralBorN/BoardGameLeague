using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Logging
{
    // Reads the JSON-lines log files written by FileLoggerBackgroundService back out for
    // the admin UI and the logs API. Deliberately reads straight off disk rather than
    // keeping its own in-memory copy, so what you see is always what's actually persisted.
    public class LogQueryService : ILogQueryService
    {
        private const string FilePrefix = "app-";
        private const string DateFormat = "yyyyMMdd";

        private readonly FileLoggerOptions _options;

        public LogQueryService(FileLoggerOptions options)
        {
            _options = options;
        }

        public LogQueryResult Query(LogQueryOptions query)
        {
            var entries = new List<LogEntry>();

            foreach (var file in GetLogFiles(query.Date))
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(file);
                }
                catch (IOException)
                {
                    // Being written to concurrently by the background service; skip this pass.
                    continue;
                }

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    LogEntry? entry;
                    try
                    {
                        entry = JsonSerializer.Deserialize<LogEntry>(line);
                    }
                    catch (JsonException)
                    {
                        continue;
                    }

                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
            }

            entries.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

            IEnumerable<LogEntry> filtered = entries;

            if (query.MinLevel.HasValue)
            {
                filtered = filtered.Where(e => Enum.TryParse<LogLevel>(e.Level, out var level) && level >= query.MinLevel.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                filtered = filtered.Where(e =>
                    e.Message.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    e.Category.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (e.Exception != null && e.Exception.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            var materialized = filtered.ToList();
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 200);

            return new LogQueryResult
            {
                Entries = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = materialized.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        public IReadOnlyList<DateOnly> GetAvailableDates()
        {
            if (!Directory.Exists(_options.LogDirectory))
            {
                return Array.Empty<DateOnly>();
            }

            var dates = new List<DateOnly>();
            foreach (var file in Directory.GetFiles(_options.LogDirectory, $"{FilePrefix}*.log"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var datePart = name.StartsWith(FilePrefix) ? name[FilePrefix.Length..] : name;
                if (DateOnly.TryParseExact(datePart, DateFormat, out var date))
                {
                    dates.Add(date);
                }
            }

            return dates.OrderByDescending(d => d).ToList();
        }

        private List<string> GetLogFiles(DateOnly? date)
        {
            if (date.HasValue)
            {
                return new List<string> { Path.Combine(_options.LogDirectory, $"{FilePrefix}{date.Value.ToString(DateFormat)}.log") };
            }

            // Default to today + yesterday (UTC, matching how entries are timestamped/named)
            // so recent activity is visible without scanning every file ever written.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new List<string>
            {
                Path.Combine(_options.LogDirectory, $"{FilePrefix}{today.ToString(DateFormat)}.log"),
                Path.Combine(_options.LogDirectory, $"{FilePrefix}{today.AddDays(-1).ToString(DateFormat)}.log")
            };
        }
    }
}
