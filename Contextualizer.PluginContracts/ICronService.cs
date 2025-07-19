using System;
using System.Collections.Generic;

namespace Contextualizer.PluginContracts
{
    /// <summary>
    /// Service interface for managing cron-based scheduled tasks
    /// </summary>
    public interface ICronService
    {
        /// <summary>
        /// Schedule a new cron job
        /// </summary>
        /// <param name="jobId">Unique identifier for the job</param>
        /// <param name="cronExpression">Cron expression (e.g., "0 8 * * MON-FRI")</param>
        /// <param name="handlerConfig">Handler configuration to execute</param>
        /// <param name="timezone">Timezone for schedule (defaults to system timezone)</param>
        /// <returns>True if successfully scheduled</returns>
        bool ScheduleJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null);

        /// <summary>
        /// Remove a scheduled job
        /// </summary>
        /// <param name="jobId">Job identifier to remove</param>
        /// <returns>True if successfully removed</returns>
        bool UnscheduleJob(string jobId);

        /// <summary>
        /// Update an existing scheduled job
        /// </summary>
        /// <param name="jobId">Job identifier to update</param>
        /// <param name="cronExpression">New cron expression</param>
        /// <param name="handlerConfig">Updated handler configuration</param>
        /// <param name="timezone">Updated timezone</param>
        /// <returns>True if successfully updated</returns>
        bool UpdateJob(string jobId, string cronExpression, HandlerConfig handlerConfig, string timezone = null);

        /// <summary>
        /// Get all scheduled jobs
        /// </summary>
        /// <returns>Dictionary of job ID to job info</returns>
        Dictionary<string, CronJobInfo> GetScheduledJobs();

        /// <summary>
        /// Get information about a specific job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <returns>Job information or null if not found</returns>
        CronJobInfo GetJobInfo(string jobId);

        /// <summary>
        /// Manually trigger a job execution
        /// </summary>
        /// <param name="jobId">Job identifier to execute</param>
        /// <returns>True if successfully triggered</returns>
        bool TriggerJob(string jobId);

        /// <summary>
        /// Enable or disable a scheduled job
        /// </summary>
        /// <param name="jobId">Job identifier</param>
        /// <param name="enabled">Enable or disable the job</param>
        /// <returns>True if successfully updated</returns>
        bool SetJobEnabled(string jobId, bool enabled);

        /// <summary>
        /// Validate a cron expression
        /// </summary>
        /// <param name="cronExpression">Cron expression to validate</param>
        /// <returns>True if valid</returns>
        bool ValidateCronExpression(string cronExpression);

        /// <summary>
        /// Get the next execution time for a cron expression
        /// </summary>
        /// <param name="cronExpression">Cron expression</param>
        /// <param name="timezone">Timezone (defaults to system timezone)</param>
        /// <returns>Next execution time or null if invalid</returns>
        DateTime? GetNextExecution(string cronExpression, string timezone = null);

        /// <summary>
        /// Start the cron service
        /// </summary>
        Task Start();

        /// <summary>
        /// Stop the cron service
        /// </summary>
        Task Stop();

        /// <summary>
        /// Check if the service is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Event fired when a job is executed
        /// </summary>
        event EventHandler<CronJobExecutedEventArgs> JobExecuted;

        /// <summary>
        /// Event fired when a job execution fails
        /// </summary>
        event EventHandler<CronJobErrorEventArgs> JobExecutionFailed;
    }

    /// <summary>
    /// Information about a scheduled cron job
    /// </summary>
    public class CronJobInfo
    {
        public string JobId { get; set; }
        public string CronExpression { get; set; }
        public HandlerConfig HandlerConfig { get; set; }
        public string Timezone { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public int ExecutionCount { get; set; }
        public string LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Event arguments for job execution events
    /// </summary>
    public class CronJobExecutedEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public DateTime ExecutionTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Result { get; set; }
    }

    /// <summary>
    /// Event arguments for job execution error events
    /// </summary>
    public class CronJobErrorEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public DateTime ExecutionTime { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }
    }
}