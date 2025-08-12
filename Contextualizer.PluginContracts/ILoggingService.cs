using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface ILoggingService
    {
        // Local file logging (errors, debug, warnings)
        void LogError(string message, Exception? exception = null, Dictionary<string, object>? context = null);
        void LogWarning(string message, Dictionary<string, object>? context = null);
        void LogInfo(string message, Dictionary<string, object>? context = null);
        void LogDebug(string message, Dictionary<string, object>? context = null);
        
        // Usage statistics (sent to remote endpoint)
        Task LogUsageAsync(UsageEvent usageEvent);
        Task LogUserActivityAsync(string activity, Dictionary<string, object>? data = null);
        
        // Handler-specific logging
        void LogHandlerExecution(string handlerName, string handlerType, TimeSpan duration, bool success, Dictionary<string, object>? context = null);
        void LogHandlerError(string handlerName, string handlerType, Exception exception, Dictionary<string, object>? context = null);
        
        // System events
        void LogSystemEvent(string eventType, Dictionary<string, object>? data = null);
        Task LogSystemEventAsync(string eventType, Dictionary<string, object>? data = null);
        
        // Configuration
        void SetConfiguration(LoggingConfiguration config);
        LoggingConfiguration GetConfiguration();
    }

    public class UsageEvent
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty; // Anonymous hash
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
    }

    public class LoggingConfiguration
    {
        public bool EnableLocalLogging { get; set; } = true;
        public bool EnableUsageTracking { get; set; } = true;
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
        public string LocalLogPath { get; set; } = "logs";
        public string? UsageEndpointUrl { get; set; }
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int MaxLogFileCount { get; set; } = 5;
        public bool EnableDebugMode { get; set; } = false;
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
}