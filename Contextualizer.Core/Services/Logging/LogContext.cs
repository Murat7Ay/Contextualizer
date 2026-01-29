using System;
using System.Collections.Generic;
using System.Threading;

namespace Contextualizer.Core.Services.Logging
{
    public class LogContext
    {
        private static readonly AsyncLocal<LogContext> _current = new();
        
        public static LogContext Current 
        { 
            get => _current.Value ??= new LogContext(); 
            set => _current.Value = value; 
        }

        public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string TraceId { get; set; } = Guid.NewGuid().ToString("N")[..12];
        public string Component { get; set; } = "Unknown";
        public Dictionary<string, object> Properties { get; set; } = new();

        public static IDisposable BeginScope(string component, Dictionary<string, object>? properties = null)
        {
            var previous = Current;
            Current = new LogContext
            {
                CorrelationId = previous.CorrelationId,
                TraceId = Guid.NewGuid().ToString("N")[..12],
                Component = component,
                Properties = new Dictionary<string, object>(previous.Properties)
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    Current.Properties[kvp.Key] = kvp.Value;
                }
            }

            return new LogScope(previous);
        }

        private class LogScope : IDisposable
        {
            private readonly LogContext _previous;

            public LogScope(LogContext previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                Current = _previous;
            }
        }
    }
}
