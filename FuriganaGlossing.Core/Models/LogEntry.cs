using FuriganaGlossing.Services;

namespace FuriganaGlossing.Models
{
    public class LogEntry
    {
        public DateTime Time { get; }
        public LogLevel Level { get; }
        public string Message { get; }

        public LogEntry(DateTime time, LogLevel level, string message)
        {
            Time = time;
            Level = level;
            Message = message;
        }
    }
}
