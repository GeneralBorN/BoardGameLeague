using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace BoardGameLeague.Logging
{
    // Sole writer to the log files: drains the channel FileLogger writes into and appends
    // one JSON line per entry to a daily-rolling file (app-yyyyMMdd.log), so concurrent
    // request threads never contend on the same file handle.
    public class FileLoggerBackgroundService : BackgroundService
    {
        private readonly Channel<LogEntry> _channel;
        private readonly FileLoggerOptions _options;

        public FileLoggerBackgroundService(Channel<LogEntry> channel, FileLoggerOptions options)
        {
            _channel = channel;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Directory.CreateDirectory(_options.LogDirectory);

            try
            {
                await foreach (var entry in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    var path = Path.Combine(_options.LogDirectory, $"app-{entry.Timestamp:yyyyMMdd}.log");
                    var line = JsonSerializer.Serialize(entry) + Environment.NewLine;
                    try
                    {
                        await File.AppendAllTextAsync(path, line, stoppingToken);
                    }
                    catch (IOException)
                    {
                        // Transient file contention - the entry is dropped rather than
                        // risking a crash of the background service over a single log line.
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }
    }
}
