using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Logging
{
    [ProviderAlias("File")]
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly FileLoggerOptions _options;
        private readonly ChannelWriter<LogEntry> _writer;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

        public FileLoggerProvider(FileLoggerOptions options, Channel<LogEntry> channel)
        {
            _options = options;
            _writer = channel.Writer;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options, _writer));

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
