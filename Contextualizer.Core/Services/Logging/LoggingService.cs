using Contextualizer.PluginContracts;
using Contextualizer.Core.Services.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services
{
    public class LoggingService : ILoggingService, IDisposable
    {
        private LoggingConfiguration _config;
        private readonly string _sessionId;
        private readonly string _userId;
        private readonly string _version;
        
        // Async logging support
        private readonly Channel<LogEntry> _logChannel;
        private readonly ChannelWriter<LogEntry> _logWriter;
        private readonly ChannelReader<LogEntry> _logReader;
        
        // Component dependencies
        private readonly LogFileWriter _fileWriter;
        private readonly LogPerformanceTracker _performanceTracker;
        private readonly LogUsageTracker _usageTracker;
        private readonly LogEntryProcessor _entryProcessor;
        
        private volatile bool _disposed = false;

        public LoggingService()
        {
            _config = new LoggingConfiguration();
            _sessionId = Guid.NewGuid().ToString();
            _userId = GetAnonymousUserId();
            _version = GetApplicationVersion();
            
            // Initialize async logging channel
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _logChannel = Channel.CreateBounded<LogEntry>(options);
            _logWriter = _logChannel.Writer;
            _logReader = _logChannel.Reader;
            
            // Initialize components
            _fileWriter = new LogFileWriter(_config);
            _performanceTracker = new LogPerformanceTracker();
            _usageTracker = new LogUsageTracker(_config, _userId, _sessionId, _version);
            _entryProcessor = new LogEntryProcessor(_logReader, _fileWriter, _performanceTracker);
        }

        public void SetConfiguration(LoggingConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileWriter.UpdateConfiguration(_config);
            _usageTracker.UpdateConfiguration(_config);
        }

        public LoggingConfiguration GetConfiguration() => _config;

        #region Local File Logging

        public void LogError(string message, Exception? exception = null, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Error)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Error, message, exception, context);
            LogAsync(logEntry);
        }

        public void LogWarning(string message, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Warning, message, null, context);
            LogAsync(logEntry);
        }

        public void LogInfo(string message, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Info, message, null, context);
            LogAsync(logEntry);
        }

        public void LogDebug(string message, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Debug)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Debug, message, null, context);
            LogAsync(logEntry);
        }

        #endregion

        #region Handler-specific Logging

        public void LogHandlerExecution(string handlerName, string handlerType, TimeSpan duration, bool success, Dictionary<string, object>? context = null)
        {
            var data = new Dictionary<string, object>
            {
                ["handler_name"] = handlerName,
                ["handler_type"] = handlerType,
                ["duration_ms"] = duration.TotalMilliseconds,
                ["success"] = success
            };

            if (context != null)
            {
                foreach (var kvp in context)
                    data[kvp.Key] = kvp.Value;
            }

            var message = $"Handler '{handlerName}' ({handlerType}) executed in {duration.TotalMilliseconds:F2}ms - {(success ? "SUCCESS" : "FAILED")}";
            
            if (success)
                LogInfo(message, data);
            else
                LogWarning(message, data);

            // Also send usage data
            _ = Task.Run(() => LogUserActivityAsync("handler_execution", data));
        }

        public void LogHandlerError(string handlerName, string handlerType, Exception exception, Dictionary<string, object>? context = null)
        {
            var data = new Dictionary<string, object>
            {
                ["handler_name"] = handlerName,
                ["handler_type"] = handlerType,
                ["error_type"] = exception.GetType().Name
            };

            if (context != null)
            {
                foreach (var kvp in context)
                    data[kvp.Key] = kvp.Value;
            }

            LogError($"Handler '{handlerName}' ({handlerType}) failed", exception, data);
        }

        #endregion

        #region Usage Statistics

        public async Task LogUsageAsync(UsageEvent usageEvent)
        {
            await _usageTracker.LogUsageAsync(usageEvent, (msg) => LogDebug(msg));
        }

        public async Task LogUserActivityAsync(string activity, Dictionary<string, object>? data = null)
        {
            var usageEvent = new UsageEvent
            {
                EventType = activity,
                Data = data ?? new Dictionary<string, object>()
            };

            await LogUsageAsync(usageEvent);
        }

        #endregion

        #region System Events

        public void LogSystemEvent(string eventType, Dictionary<string, object>? data = null)
        {
            var message = $"System Event: {eventType}";
            LogInfo(message, data);
        }

        public async Task LogSystemEventAsync(string eventType, Dictionary<string, object>? data = null)
        {
            LogSystemEvent(eventType, data);
            await LogUserActivityAsync($"system_{eventType}", data);
        }

        #endregion

        #region Enhanced Logging Features

        public Dictionary<string, PerformanceMetrics> GetPerformanceMetrics()
        {
            return _performanceTracker.GetMetrics();
        }

        public void LogPerformance(string operation, TimeSpan duration, Dictionary<string, object>? context = null)
        {
            var enrichedContext = context ?? new Dictionary<string, object>();
            enrichedContext["duration_ms"] = duration.TotalMilliseconds;
            enrichedContext["operation"] = operation;
            
            LogInfo($"Performance: {operation} completed in {duration.TotalMilliseconds:F2}ms", enrichedContext);
        }

        public IDisposable BeginScope(string component, Dictionary<string, object>? properties = null)
        {
            return LogContext.BeginScope(component, properties);
        }

        public void LogStructured(LogLevel level, string template, params object[] args)
        {
            if (!ShouldLog(level)) return;

            try
            {
                var message = string.Format(template, args);
                var context = new Dictionary<string, object>();
                
                // Add structured parameters
                for (int i = 0; i < args.Length; i++)
                {
                    context[$"arg{i}"] = args[i];
                }

                var logEntry = CreateLogEntry(level, message, null, context);
                LogAsync(logEntry);
            }
            catch (Exception ex)
            {
                LogError($"Failed to log structured message: {template}", ex);
            }
        }

        #endregion

        #region Private Methods

        private bool ShouldLog(LogLevel level)
        {
            return _config.EnableLocalLogging && level >= _config.MinimumLogLevel;
        }

        private LogEntry CreateLogEntry(LogLevel level, string message, Exception? exception = null, Dictionary<string, object>? context = null)
        {
            var logContext = LogContext.Current;
            var combinedContext = new Dictionary<string, object>(logContext.Properties);
            
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    combinedContext[kvp.Key] = kvp.Value;
                }
            }

            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Exception = exception?.ToString(),
                Context = combinedContext,
                SessionId = _sessionId,
                UserId = _userId,
                CorrelationId = logContext.CorrelationId,
                TraceId = logContext.TraceId,
                Component = logContext.Component
            };
        }

        private void LogAsync(LogEntry logEntry)
        {
            if (_disposed) return;

            try
            {
                if (!_logWriter.TryWrite(logEntry))
                {
                    // Channel is full, try async write with timeout
                    Task.Run(async () =>
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await _logWriter.WriteAsync(logEntry, cts.Token);
                        }
                        catch
                        {
                            // Fallback to console if async write fails
                            Console.WriteLine($"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss}] {logEntry.Level}: {logEntry.Message}");
                        }
                    });
                }
            }
            catch
            {
                // Fallback to console if channel write fails
                Console.WriteLine($"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss}] {logEntry.Level}: {logEntry.Message}");
            }
        }

        private string GetAnonymousUserId()
        {
            try
            {
                var machineId = Environment.MachineName + Environment.UserName;
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
                return Convert.ToHexString(hash)[..16]; // First 16 chars
            }
            catch
            {
                return Guid.NewGuid().ToString("N")[..16];
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                return assembly?.GetName().Version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Stop accepting new log entries first
                _logWriter.Complete();
                
                // Dispose components
                _entryProcessor?.Dispose();
                _usageTracker?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logging service disposal: {ex.Message}");
            }
        }
    }
}
