using Contextualizer.PluginContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services
{
    public class LoggingService : ILoggingService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private LoggingConfiguration _config;
        private readonly string _sessionId;
        private readonly string _userId;
        private readonly string _version;
        private readonly object _fileLock = new object();
        
        // Async logging support
        private readonly Channel<LogEntry> _logChannel;
        private readonly ChannelWriter<LogEntry> _logWriter;
        private readonly ChannelReader<LogEntry> _logReader;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _backgroundLoggingTask;
        
        // Performance metrics
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceMetrics;
        private volatile bool _disposed = false;

        public LoggingService()
        {
            _httpClient = new HttpClient();
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
            
            _cancellationTokenSource = new CancellationTokenSource();
            _performanceMetrics = new ConcurrentDictionary<string, PerformanceMetrics>();
            
            // Start background logging task
            _backgroundLoggingTask = Task.Run(ProcessLogEntriesAsync);
        }

        public void SetConfiguration(LoggingConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            EnsureLogDirectoryExists();
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
            if (!_config.EnableUsageTracking || string.IsNullOrEmpty(_config.UsageEndpointUrl))
                return;

            try
            {
                usageEvent.UserId = _userId;
                usageEvent.SessionId = _sessionId;
                usageEvent.Version = _version;

                var json = JsonSerializer.Serialize(usageEvent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var response = await _httpClient.PostAsync(_config.UsageEndpointUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    LogDebug($"Failed to send usage data: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error sending usage data: {ex.Message}");
            }
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

        private async Task ProcessLogEntriesAsync()
        {
            try
            {
                await foreach (var logEntry in _logReader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        WriteToFile(GetLogFileName(logEntry.Level), logEntry);
                        UpdatePerformanceMetrics(logEntry.Component, stopwatch.Elapsed, false);
                    }
                    catch (Exception ex)
                    {
                        UpdatePerformanceMetrics(logEntry.Component, stopwatch.Elapsed, true);
                        
                        // Fallback to console if file write fails
                        Console.WriteLine($"Logging failed: {ex.Message}");
                        Console.WriteLine($"Original log: {logEntry.Level} - {logEntry.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background logging task failed: {ex.Message}");
            }
        }

        private string GetLogFileName(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => "error",
                LogLevel.Warning => "warning", 
                LogLevel.Info => "info",
                LogLevel.Debug => "debug",
                LogLevel.Critical => "critical",
                _ => "general"
            };
        }

        private void UpdatePerformanceMetrics(string component, TimeSpan duration, bool isError)
        {
            _performanceMetrics.AddOrUpdate(component, 
                new PerformanceMetrics { TotalLogs = 1, TotalErrors = isError ? 1 : 0, TotalDuration = duration },
                (key, existing) => 
                {
                    existing.TotalLogs++;
                    if (isError) existing.TotalErrors++;
                    existing.TotalDuration = existing.TotalDuration.Add(duration);
                    existing.LastUpdate = DateTime.UtcNow;
                    return existing;
                });
        }

        private void WriteToFile(string logType, LogEntry logEntry)
        {
            if (!_config.EnableLocalLogging) return;

            try
            {
                lock (_fileLock)
                {
                    var fileName = $"{logType}_{DateTime.UtcNow:yyyy-MM-dd}.log";
                    var filePath = Path.Combine(_config.LocalLogPath, fileName);
                    
                    var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    });
                    
                    File.AppendAllText(filePath, json + Environment.NewLine);
                    
                    // Check file size and rotate if necessary
                    RotateLogFileIfNeeded(filePath);
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails
                Console.WriteLine($"Logging failed: {ex.Message}");
                Console.WriteLine($"Original log: {logEntry.Level} - {logEntry.Message}");
            }
        }

        private void RotateLogFileIfNeeded(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists && fileInfo.Length > _config.MaxLogFileSizeMB * 1024 * 1024)
                {
                    var directory = Path.GetDirectoryName(filePath)!;
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var extension = Path.GetExtension(filePath);
                    
                    // Find next available file number
                    int counter = 1;
                    string newFileName;
                    do
                    {
                        newFileName = Path.Combine(directory, $"{fileName}_{counter}{extension}");
                        counter++;
                    } while (File.Exists(newFileName) && counter <= _config.MaxLogFileCount);
                    
                    if (counter <= _config.MaxLogFileCount)
                    {
                        File.Move(filePath, newFileName);
                    }
                    else
                    {
                        // Delete oldest files if we exceed max count
                        CleanupOldLogFiles(directory, fileName, extension);
                    }
                }
            }
            catch
            {
                // Ignore rotation errors
            }
        }

        private void CleanupOldLogFiles(string directory, string baseName, string extension)
        {
            try
            {
                var pattern = $"{baseName}_*{extension}";
                var files = Directory.GetFiles(directory, pattern);
                
                if (files.Length >= _config.MaxLogFileCount)
                {
                    Array.Sort(files, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));
                    
                    // Delete oldest files
                    for (int i = 0; i < files.Length - _config.MaxLogFileCount + 1; i++)
                    {
                        File.Delete(files[i]);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_config.LocalLogPath))
                {
                    Directory.CreateDirectory(_config.LocalLogPath);
                }
            }
            catch
            {
                // Use current directory as fallback
                _config.LocalLogPath = Path.Combine(Environment.CurrentDirectory, "logs");
                Directory.CreateDirectory(_config.LocalLogPath);
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
                // Stop accepting new log entries
                _logWriter.Complete();
                
                // Wait for background task to finish processing remaining entries
                _backgroundLoggingTask.Wait(TimeSpan.FromSeconds(5));
                
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logging service disposal: {ex.Message}");
            }
            finally
            {
                _httpClient?.Dispose();
            }
        }

        // New API methods for enhanced logging

        public Dictionary<string, PerformanceMetrics> GetPerformanceMetrics()
        {
            return new Dictionary<string, PerformanceMetrics>(_performanceMetrics);
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
    }

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