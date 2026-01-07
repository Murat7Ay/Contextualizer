using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfInteractionApp.Services
{
    public class ActivityLogService
    {
        private readonly ObservableCollection<LogEntry> _logs = new ObservableCollection<LogEntry>();

        public ObservableCollection<LogEntry> Logs => _logs;

        public int MaxEntries { get; set; } = 200;

        public void Add(LogEntry log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            const int maxLength = 200;
            if (!string.IsNullOrEmpty(log.Message) && log.Message.Length > maxLength)
                log.Message = log.Message.Substring(0, maxLength) + "...";

            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                Insert(log);
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() => Insert(log));
        }

        private void Insert(LogEntry log)
        {
            _logs.Insert(0, log);
            while (_logs.Count > MaxEntries)
                _logs.RemoveAt(_logs.Count - 1);
        }

        public void Clear()
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == true)
            {
                _logs.Clear();
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() => _logs.Clear());
        }
    }
}


