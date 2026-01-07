using Contextualizer.PluginContracts;
using System;

namespace WpfInteractionApp
{
    public class LogEntry
    {
        public LogType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string AdditionalInfo { get; set; } = string.Empty;
    }
}


