using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;

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
        ReadOnlyObservableCollection<string> Logs { get; }
        void Clear();
    }

    public class LogService : ILogService
    {
        private readonly ObservableCollection<string> _logs = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> Logs { get; }

        public LogService()
        {
            Logs = new ReadOnlyObservableCollection<string>(_logs);
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] [{level}] {message}";

            // Ensure we update the collection on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logs.Add(formattedMessage);
                // Optional: limit the number of logs to prevent memory issues
                if (_logs.Count > 1000)
                {
                    _logs.RemoveAt(0);
                }
            });
        }

        public void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logs.Clear();
            });
        }
    }
}
