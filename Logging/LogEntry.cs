namespace BoardGameLeague.Logging
{
    // One line of the on-disk log file is exactly the JSON serialization of this type
    // (see FileLoggerBackgroundService), so writing and reading share the same shape.
    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }
}
