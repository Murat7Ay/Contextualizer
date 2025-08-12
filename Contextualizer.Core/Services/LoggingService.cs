using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly HttpClient _httpClient;
        private LoggingConfiguration _config;
        private readonly string _sessionId;
        private readonly string _userId;
        private readonly string _version;
        private readonly object _fileLock = new object();

        public LoggingService()
        {
            _httpClient = new HttpClient();
            _config = new LoggingConfiguration();
            _sessionId = Guid.NewGuid().ToString();
            _userId = GetAnonymousUserId();
            _version = GetApplicationVersion();
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
            WriteToFile("error", logEntry);
        }

        public void LogWarning(string message, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Warning)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Warning, message, null, context);
            WriteToFile("warning", logEntry);
        }

        public void LogInfo(string message, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Info)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Info, message, null, context);
            WriteToFile("info", logEntry);
        }

        public void LogDebug(string message, Dictionary<string, object>? context = null)
        {
            if (!ShouldLog(LogLevel.Debug)) return;
            
            var logEntry = CreateLogEntry(LogLevel.Debug, message, null, context);
            WriteToFile("debug", logEntry);
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
            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Exception = exception?.ToString(),
                Context = context ?? new Dictionary<string, object>(),
                SessionId = _sessionId,
                UserId = _userId
            };
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
            _httpClient?.Dispose();
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
    }
}