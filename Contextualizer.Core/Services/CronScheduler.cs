using Contextualizer.PluginContracts;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services
{
    public class CronScheduler : ICronService, IDisposable
    {
        private readonly ConcurrentDictionary<string, CronJobInfo> _jobs;
        private IScheduler? _scheduler;
        private bool _isRunning = false;

        public event EventHandler<CronJobExecutedEventArgs>? JobExecuted;
        public event EventHandler<CronJobErrorEventArgs>? JobExecutionFailed;

        public CronScheduler()
        {
            _jobs = new ConcurrentDictionary<string, CronJobInfo>();
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

                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Info, 
                    "CronScheduler: Quartz scheduler started successfully"
                );

                var logger = ServiceLocator.Get<ILoggingService>();
                logger?.LogInfo("CronScheduler started successfully");
                _ = logger?.LogSystemEventAsync("cron_scheduler_start");
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>()?.Log(
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
                await _scheduler.Shutdown();
                _isRunning = false;

                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Info, 
                    "CronScheduler: Scheduler stopped"
                );
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Error, 
                    $"CronScheduler: Error stopping scheduler: {ex.Message}"
                );
            }
        }

        public bool ScheduleJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null)
        {
            ServiceLocator.Get<IUserInteractionService>()?.Log(
                LogType.Info, 
                $"CronScheduler: Attempting to schedule job '{jobId}' with expression '{cronExpression}'"
            );
            
            if (string.IsNullOrEmpty(jobId) || handlerConfig == null || _scheduler == null)
                return false;

            try
            {
                // Create job info
                var jobInfo = new CronJobInfo
                {
                    JobId = jobId,
                    CronExpression = cronExpression,
                    HandlerConfig = handlerConfig,
                    Timezone = timezone ?? TimeZoneInfo.Local.Id,
                    Enabled = true,
                    ExecutionCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Store job info
                _jobs.AddOrUpdate(jobId, jobInfo, (key, existing) => jobInfo);

                // Create Quartz job
                var job = JobBuilder.Create<CronHandlerJob>()
                    .WithIdentity(jobId)
                    .UsingJobData("jobId", jobId)
                    .Build();

                // Create trigger with cron expression
                var triggerBuilder = TriggerBuilder.Create()
                    .WithIdentity($"{jobId}_trigger")
                    .WithCronSchedule(cronExpression, x => {
                        if (!string.IsNullOrEmpty(timezone))
                        {
                            x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timezone));
                        }
                    });

                var trigger = triggerBuilder.Build();

                // Schedule the job
                _scheduler.ScheduleJob(job, trigger).Wait();

                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Info, 
                    $"CronScheduler: Successfully scheduled job '{jobId}'. Next execution: {trigger.GetNextFireTimeUtc()?.DateTime:yyyy-MM-dd HH:mm:ss} UTC. Total jobs: {_jobs.Count}"
                );
                
                return true;
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Error, 
                    $"CronScheduler: Error scheduling job '{jobId}': {ex.Message}"
                );
                return false;
            }
        }

        public bool UnscheduleJob(string jobId)
        {
            if (_scheduler == null)
                return false;

            try
            {
                var jobKey = new JobKey(jobId);
                var result = _scheduler.DeleteJob(jobKey).Result;
                
                if (result)
                {
                    _jobs.TryRemove(jobId, out _);
                }
                
                return result;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null)
        {
            if (!_jobs.TryGetValue(jobId, out var existingJob) || _scheduler == null)
                return false;

            try
            {
                // Unschedule existing job
                UnscheduleJob(jobId);
                
                // Schedule new job
                return ScheduleJob(jobId, cronExpression, handlerConfig, timezone);
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, CronJobInfo> GetScheduledJobs()
        {
            return _jobs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public CronJobInfo GetJobInfo(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var job) ? job : null;
        }

        public bool TriggerJob(string jobId)
        {
            if (_scheduler == null || !_jobs.ContainsKey(jobId))
                return false;

            try
            {
                var jobKey = new JobKey(jobId);
                _scheduler.TriggerJob(jobKey).Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SetJobEnabled(string jobId, bool enabled)
        {
            if (!_jobs.TryGetValue(jobId, out var job) || _scheduler == null)
                return false;

            try
            {
                var jobKey = new JobKey(jobId);
                
                if (enabled)
                {
                    _scheduler.ResumeJob(jobKey).Wait();
                }
                else
                {
                    _scheduler.PauseJob(jobKey).Wait();
                }

                job.Enabled = enabled;
                job.UpdatedAt = DateTime.UtcNow;
                return true;
            }
            catch
            {
                return false;
            }
        }


        internal async Task ExecuteJob(string jobId)
        {
            if (!_jobs.TryGetValue(jobId, out var jobInfo))
                return;

            var startTime = DateTime.UtcNow;

            try
            {
                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Info, 
                    $"CronScheduler: Executing job '{jobId}'"
                );

                // Update execution tracking
                jobInfo.LastExecution = startTime;
                jobInfo.ExecutionCount++;

                // Execute the handler through synthetic content approach
                var result = await ExecuteHandlerConfig(jobInfo.HandlerConfig);

                var duration = DateTime.UtcNow - startTime;
                jobInfo.LastError = null;

                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Info, 
                    $"CronScheduler: Job '{jobId}' completed successfully in {duration.TotalMilliseconds}ms"
                );

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
                jobInfo.LastError = ex.Message;

                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Error, 
                    $"CronScheduler: Job '{jobId}' failed after {duration.TotalMilliseconds}ms: {ex.Message}"
                );

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

        public void Dispose()
        {
            if (_isRunning)
            {
                Stop().Wait();
            }
        }
    }

    // Quartz Job implementation
    public class CronHandlerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var jobId = context.JobDetail.JobDataMap.GetString("jobId");
            if (string.IsNullOrEmpty(jobId))
                return;

            try
            {
                var cronScheduler = ServiceLocator.Get<ICronService>() as CronScheduler;
                if (cronScheduler != null)
                {
                    await cronScheduler.ExecuteJob(jobId);
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>()?.Log(
                    LogType.Error, 
                    $"CronHandlerJob: Error executing job '{jobId}': {ex.Message}"
                );
                throw;
            }
        }
    }
}