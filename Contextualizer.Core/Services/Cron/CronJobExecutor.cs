using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services.Cron
{
    internal class CronJobExecutor
    {
        private readonly CronJobManager _jobManager;

        public CronJobExecutor(CronJobManager jobManager)
        {
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
        }

        public async Task<string> ExecuteJobAsync(string jobId, Action<string, LogType>? feedbackLogger = null)
        {
            var jobInfo = _jobManager.GetJobInfoInternal(jobId);
            if (jobInfo == null)
                return string.Empty;

            var startTime = DateTime.UtcNow;

            try
            {
                feedbackLogger?.Invoke($"CronScheduler: Executing job '{jobId}'", LogType.Info);

                // Update execution tracking
                jobInfo.LastExecution = startTime;
                jobInfo.ExecutionCount++;

                // Execute the handler through synthetic content approach
                var result = await ExecuteHandlerConfig(jobInfo.HandlerConfig);

                var duration = DateTime.UtcNow - startTime;
                jobInfo.LastError = null;

                feedbackLogger?.Invoke(
                    $"CronScheduler: Job '{jobId}' completed successfully in {duration.TotalMilliseconds}ms",
                    LogType.Info);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                jobInfo.LastError = ex.Message;

                feedbackLogger?.Invoke(
                    $"CronScheduler: Job '{jobId}' failed after {duration.TotalMilliseconds}ms: {ex.Message}",
                    LogType.Error);

                throw;
            }
        }

        private async Task<string> ExecuteHandlerConfig(HandlerConfig handlerConfig)
        {
            try
            {
                var handlerManager = ServiceLocator.Get<HandlerManager>();
                return await handlerManager.ExecuteHandlerConfig(handlerConfig);
            }
            catch (Exception ex)
            {
                return $"Failed to execute handler {handlerConfig.Name}: {ex.Message}";
            }
        }
    }
}
