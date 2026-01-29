using Contextualizer.PluginContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Contextualizer.Core.Services.Logging
{
    internal class LogPerformanceTracker
    {
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceMetrics;

        public LogPerformanceTracker()
        {
            _performanceMetrics = new ConcurrentDictionary<string, PerformanceMetrics>();
        }

        public void UpdateMetrics(string component, TimeSpan duration, bool isError)
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

        public Dictionary<string, PerformanceMetrics> GetMetrics()
        {
            return new Dictionary<string, PerformanceMetrics>(_performanceMetrics);
        }
    }
}
