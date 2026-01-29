using Contextualizer.PluginContracts;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services.Logging
{
    internal class LogEntryProcessor : IDisposable
    {
        private readonly ChannelReader<LogEntry> _logReader;
        private readonly LogFileWriter _fileWriter;
        private readonly LogPerformanceTracker _performanceTracker;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _backgroundTask;
        private volatile bool _disposed = false;

        public LogEntryProcessor(
            ChannelReader<LogEntry> logReader,
            LogFileWriter fileWriter,
            LogPerformanceTracker performanceTracker)
        {
            _logReader = logReader ?? throw new ArgumentNullException(nameof(logReader));
            _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
            _performanceTracker = performanceTracker ?? throw new ArgumentNullException(nameof(performanceTracker));
            _cancellationTokenSource = new CancellationTokenSource();
            _backgroundTask = Task.Run(ProcessLogEntriesAsync);
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
                        _fileWriter.WriteLogEntry(logEntry);
                        _performanceTracker.UpdateMetrics(logEntry.Component, stopwatch.Elapsed, false);
                    }
                    catch (Exception ex)
                    {
                        _performanceTracker.UpdateMetrics(logEntry.Component, stopwatch.Elapsed, true);
                        
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _cancellationTokenSource.Cancel();
                if (!_backgroundTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("Background logging task did not complete within timeout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping log entry processor: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
            }
        }
    }
}
