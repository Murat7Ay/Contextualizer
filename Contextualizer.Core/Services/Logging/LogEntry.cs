using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;

namespace Contextualizer.Core.Services.Logging
{
    internal class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
    }
}
