using Contextualizer.PluginContracts;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Contextualizer.Core.Services.Cron
{
    internal class CronJobManager
    {
        private readonly ConcurrentDictionary<string, CronJobInfo> _jobs;
        private IScheduler? _scheduler;

        public CronJobManager()
        {
            _jobs = new ConcurrentDictionary<string, CronJobInfo>();
        }

        public void SetScheduler(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public bool ScheduleJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null)
        {
            if (string.IsNullOrEmpty(jobId) || handlerConfig == null || _scheduler == null)
                return false;

            try
            {
                // Unschedule existing job if it exists (for handler reload scenarios)
                var jobKey = new JobKey(jobId);
                if (_scheduler.CheckExists(jobKey).Result)
                {
                    _scheduler.DeleteJob(jobKey).Wait();
                    _jobs.TryRemove(jobId, out _);
                }

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
                    .WithCronSchedule(cronExpression, x =>
                    {
                        if (!string.IsNullOrEmpty(timezone))
                        {
                            x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timezone));
                        }
                    });

                var trigger = triggerBuilder.Build();

                // Schedule the job
                _scheduler.ScheduleJob(job, trigger).Wait();

                return true;
            }
            catch
            {
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

        public bool UpdateJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string? timezone = null)
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

        public CronJobInfo? GetJobInfo(string jobId)
        {
            return _jobs.TryGetValue(jobId, out var job) ? job : null;
        }

        internal CronJobInfo? GetJobInfoInternal(string jobId)
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

        public int GetActiveJobCount()
        {
            return _jobs.Values.Count(job => job.Enabled);
        }
    }
}
