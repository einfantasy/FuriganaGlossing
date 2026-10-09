using System;
using System.Collections;
using System.Collections.Concurrent;
using FuriganaGlossing.Models;

namespace FuriganaGlossing.Services
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public interface ILogService
    {
        void Log(string message, LogLevel level = LogLevel.Info);
        IReadOnlyCollection<LogEntry> Logs { get; }
        void Clear();
        event EventHandler<LogEntry>? EntryAdded;
    }

    public class LogService : ILogService
    {
        private const int MaxLogCount = 1000;

        private readonly object _lock = new();
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IReadOnlyCollection<LogEntry> Logs => _entries;

        public event EventHandler<LogEntry>? EntryAdded;

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            var entry = new LogEntry(DateTime.Now, level, message);

            lock (_lock)
            {
                _entries.Enqueue(entry);
                while (_entries.Count > MaxLogCount)
                {
                    _entries.TryDequeue(out _);
                }
            }

            EntryAdded?.Invoke(this, entry);
        }

        public void Clear()
        {
            lock (_lock)
            {
                while (_entries.TryDequeue(out _))
                {
                    // drain
                }
            }
        }
    }
}
