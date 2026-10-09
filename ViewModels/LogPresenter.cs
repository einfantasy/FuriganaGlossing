using System.Collections.ObjectModel;
using System.Windows.Threading;
using FuriganaGlossing.Models;
using FuriganaGlossing.Services;

namespace FuriganaGlossing.ViewModels
{
    public class LogPresenter
    {
        private const int MaxLogCount = 1000;

        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        private readonly ILogService _logService;
        private readonly Dispatcher _dispatcher;

        public LogPresenter(ILogService logService, Dispatcher dispatcher)
        {
            _logService = logService;
            _dispatcher = dispatcher;
            _logService.EntryAdded += OnEntryAdded;
        }

        private void OnEntryAdded(object? sender, LogEntry entry)
        {
            _dispatcher.Invoke(() => AppendLog(entry));
        }

        private void AppendLog(LogEntry entry)
        {
            Logs.Add($"[{entry.Time:HH:mm:ss}] [{entry.Level}] {entry.Message}");
            while (Logs.Count > MaxLogCount)
            {
                Logs.RemoveAt(0);
            }
        }
    }
}
