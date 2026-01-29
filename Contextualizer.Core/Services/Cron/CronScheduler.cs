using Contextualizer.PluginContracts;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contextualizer.Core.Services.Cron;

namespace Contextualizer.Core.Services
{
    public class CronScheduler : ICronService, IDisposable
    {
        private readonly CronJobManager _jobManager;
        private readonly CronJobExecutor _jobExecutor;
        private IScheduler? _scheduler;
        private bool _isRunning = false;

        public event EventHandler<CronJobExecutedEventArgs>? JobExecuted;
        public event EventHandler<CronJobErrorEventArgs>? JobExecutionFailed;

        public CronScheduler()
        {
            _jobManager = new CronJobManager();
            _jobExecutor = new CronJobExecutor(_jobManager);
        }

        public async Task Start()
        {
            if (_isRunning)
                return;

            try
            {
                // Create scheduler factory
                var factory = new StdSchedulerFactory();
                _scheduler = await factory.GetScheduler();

                // Start the scheduler
                await _scheduler.Start();
                _isRunning = true;
                _jobManager.SetScheduler(_scheduler);

                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Info,
                    "CronScheduler: Quartz scheduler started successfully"
                );

                var logger = ServiceLocator.Get<ILoggingService>();
                logger?.LogInfo("CronScheduler started successfully");
                _ = logger?.LogSystemEventAsync("cron_scheduler_start");
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Error,
                    $"CronScheduler: Failed to start scheduler: {ex.Message}"
                );

                var logger = ServiceLocator.Get<ILoggingService>();
                logger?.LogError("CronScheduler failed to start", ex);
                throw;
            }
        }

        public async Task Stop()
        {
            if (!_isRunning || _scheduler == null)
                return;

            try
            {
                // Shutdown the scheduler gracefully with a timeout
                var shutdownTask = _scheduler.Shutdown(true); // true = wait for jobs to complete
                var completedTask = await Task.WhenAny(shutdownTask, Task.Delay(TimeSpan.FromSeconds(5)));

                if (completedTask != shutdownTask)
                {
                    // Timeout occurred, force shutdown
                    ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                        LogType.Warning,
                        "CronScheduler: Graceful shutdown timed out, forcing shutdown"
                    );
                    await _scheduler.Shutdown(false); // false = don't wait for jobs
                }

                _isRunning = false;

                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Info,
                    "CronScheduler: Scheduler stopped"
                );
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Error,
                    $"CronScheduler: Error stopping scheduler: {ex.Message}"
                );
            }
        }

        public bool ScheduleJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null)
        {
            ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                LogType.Info,
                $"CronScheduler: Attempting to schedule job '{jobId}' with expression '{cronExpression}'"
            );

            var result = _jobManager.ScheduleJob(jobId, cronExpression, handlerConfig, timezone);

            if (result && _scheduler != null)
            {
                var jobKey = new JobKey(jobId);
                var trigger = _scheduler.GetTrigger(new TriggerKey($"{jobId}_trigger")).Result;
                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Info,
                    $"CronScheduler: Successfully scheduled job '{jobId}'. Next execution: {trigger?.GetNextFireTimeUtc()?.DateTime:yyyy-MM-dd HH:mm:ss} UTC. Total jobs: {_jobManager.GetActiveJobCount()}"
                );
            }
            else if (!result)
            {
                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Error,
                    $"CronScheduler: Error scheduling job '{jobId}'"
                );
            }

            return result;
        }

        public bool UnscheduleJob(string jobId)
        {
            return _jobManager.UnscheduleJob(jobId);
        }

        public bool UpdateJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null)
        {
            return _jobManager.UpdateJob(jobId, cronExpression, handlerConfig, timezone);
        }

        public Dictionary<string, CronJobInfo> GetScheduledJobs()
        {
            return _jobManager.GetScheduledJobs();
        }

        public CronJobInfo GetJobInfo(string jobId)
        {
            return _jobManager.GetJobInfo(jobId) ?? throw new KeyNotFoundException($"Job '{jobId}' not found");
        }

        public bool TriggerJob(string jobId)
        {
            return _jobManager.TriggerJob(jobId);
        }

        public bool SetJobEnabled(string jobId, bool enabled)
        {
            return _jobManager.SetJobEnabled(jobId, enabled);
        }

        internal async Task ExecuteJob(string jobId)
        {
            var jobInfo = _jobManager.GetJobInfoInternal(jobId);
            if (jobInfo == null)
                return;

            var startTime = DateTime.UtcNow;

            try
            {
                var result = await _jobExecutor.ExecuteJobAsync(jobId, (msg, level) =>
                {
                    ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(level, msg);
                });

                var duration = DateTime.UtcNow - startTime;

                // Fire success event
                JobExecuted?.Invoke(this, new CronJobExecutedEventArgs
                {
                    JobId = jobInfo.JobId,
                    ExecutionTime = startTime,
                    Duration = duration,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;

                // Fire error event
                JobExecutionFailed?.Invoke(this, new CronJobErrorEventArgs
                {
                    JobId = jobInfo.JobId,
                    ExecutionTime = startTime,
                    Exception = ex,
                    ErrorMessage = ex.Message
                });
            }
        }

        public bool IsRunning => _isRunning;

        public bool ValidateCronExpression(string cronExpression)
        {
            try
            {
                CronExpression.ValidateExpression(cronExpression);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public DateTime? GetNextExecution(string cronExpression, string timezone = null)
        {
            try
            {
                var timeZoneInfo = string.IsNullOrEmpty(timezone)
                    ? TimeZoneInfo.Local
                    : TimeZoneInfo.FindSystemTimeZoneById(timezone);

                var trigger = TriggerBuilder.Create()
                    .WithCronSchedule(cronExpression, x => x.InTimeZone(timeZoneInfo))
                    .Build();

                return trigger.GetNextFireTimeUtc()?.DateTime;
            }
            catch
            {
                return null;
            }
        }

        public int GetActiveJobCount()
        {
            return _jobManager.GetActiveJobCount();
        }

        public void Dispose()
        {
            if (_isRunning)
            {
                try
                {
                    // Use a timeout when waiting for Stop to complete
                    var stopTask = Stop();
                    if (!stopTask.Wait(TimeSpan.FromSeconds(10)))
                    {
                        ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                            LogType.Warning,
                            "CronScheduler: Dispose timeout, scheduler may not have stopped cleanly"
                        );
                    }
                }
                catch (Exception ex)
                {
                    ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                        LogType.Error,
                        $"CronScheduler: Error during dispose: {ex.Message}"
                    );
                }
            }
        }
    }
}
