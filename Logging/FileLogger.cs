using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Logging
{
    // Formats log calls into LogEntry records and hands them off to the shared channel;
    // the actual disk I/O happens on FileLoggerBackgroundService's background thread so
    // that logging from a request never blocks on file access.
    public class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly FileLoggerOptions _options;
        private readonly ChannelWriter<LogEntry> _writer;

        public FileLogger(string category, FileLoggerOptions options, ChannelWriter<LogEntry> writer)
        {
            _category = category;
            _options = options;
            _writer = writer;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _options.MinLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var entry = new LogEntry
            {
                Timestamp = DateTimeOffset.UtcNow,
                Level = logLevel.ToString(),
                Category = _category,
                Message = formatter(state, exception),
                Exception = exception?.ToString()
            };

            // Best-effort: if the channel is ever full/closed, drop rather than block or throw
            // from inside arbitrary application code that happens to be logging.
            _writer.TryWrite(entry);
        }
    }
}
