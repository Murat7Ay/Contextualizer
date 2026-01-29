using Contextualizer.PluginContracts;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Contextualizer.Core.Services.Logging
{
    internal class LogFileWriter
    {
        private readonly LoggingConfiguration _config;
        private readonly object _fileLock = new object();

        public LogFileWriter(LoggingConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            EnsureLogDirectoryExists();
        }

        public void WriteLogEntry(LogEntry logEntry)
        {
            if (!_config.EnableLocalLogging) return;

            try
            {
                lock (_fileLock)
                {
                    var fileName = $"{GetLogFileName(logEntry.Level)}_{DateTime.UtcNow:yyyy-MM-dd}.log";
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

        public void UpdateConfiguration(LoggingConfiguration config)
        {
            lock (_fileLock)
            {
                EnsureLogDirectoryExists();
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
    }
}
