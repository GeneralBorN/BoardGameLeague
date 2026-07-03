using Microsoft.Extensions.Logging;

namespace BoardGameLeague.Logging
{
    public class FileLoggerOptions
    {
        public string LogDirectory { get; set; } = string.Empty;
        public LogLevel MinLevel { get; set; } = LogLevel.Information;
    }
}
