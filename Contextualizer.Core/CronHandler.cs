using Contextualizer.PluginContracts;
using Contextualizer.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    /// <summary>
    /// Handler that manages cron-based scheduled tasks using SyntheticHandler pattern
    /// </summary>
    public class CronHandler : SyntheticHandler, IHandler
    {
        public static string TypeName => "Cron";

        public CronHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            UserFeedback.ShowActivity(LogType.Info, $"CronHandler created for: {HandlerConfig.Name}");
            
            // Register this handler with the cron scheduler if it has cron properties
            if (!string.IsNullOrEmpty(HandlerConfig.CronExpression))
            {
                UserFeedback.ShowActivity(LogType.Info, $"Registering cron job for: {HandlerConfig.Name} with expression: {HandlerConfig.CronExpression}");
                RegisterCronJob();
            }
            else
            {
                UserFeedback.ShowWarning($"CronHandler {HandlerConfig.Name} has no cron expression");
            }
        }


        private void RegisterCronJob()
        {
            try
            {
                var cronService = ServiceLocator.Get<ICronService>();
                var jobId = !string.IsNullOrEmpty(HandlerConfig.CronJobId) 
                    ? HandlerConfig.CronJobId 
                    : $"cron_{HandlerConfig.Name.Replace(" ", "_").ToLower()}";
                
                // Create a modified config for the actual handler type
                var actualConfig = CreateActualHandlerConfig();
                
                var success = cronService.ScheduleJob(
                    jobId, 
                    HandlerConfig.CronExpression!, 
                    actualConfig, 
                    HandlerConfig.CronTimezone
                );

                if (success)
                {
                    UserFeedback.ShowSuccess($"Cron job scheduled: {HandlerConfig.Name} (ID: {jobId}) with expression '{HandlerConfig.CronExpression}'");
                }
                else
                {
                    UserFeedback.ShowError($"Failed to schedule cron job: {HandlerConfig.Name}");
                }
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error registering cron job for {HandlerConfig.Name}: {ex.Message}");
            }

            //ExecuteNow();
        }

        private HandlerConfig CreateActualHandlerConfig()
        {
            // ✅ Create a new config based on the actual handler type
            // This config is used by CronScheduler and should have complete properties
            var actualConfig = new HandlerConfig
            {
                Name = HandlerConfig.Name,
                Type = HandlerConfig.ActualType ?? "synthetic",
                Description = HandlerConfig.Description,
                Title = HandlerConfig.Title,
                ScreenId = HandlerConfig.ScreenId,
                Validator = HandlerConfig.Validator,
                ContextProvider = HandlerConfig.ContextProvider,
                
                // Copy all type-specific properties
                Regex = HandlerConfig.Regex,
                Groups = HandlerConfig.Groups,
                ConnectionString = HandlerConfig.ConnectionString,
                Query = HandlerConfig.Query,
                Connector = HandlerConfig.Connector,
                CommandTimeoutSeconds = HandlerConfig.CommandTimeoutSeconds,
                ConnectionTimeoutSeconds = HandlerConfig.ConnectionTimeoutSeconds,
                MaxPoolSize = HandlerConfig.MaxPoolSize,
                MinPoolSize = HandlerConfig.MinPoolSize,
                DisablePooling = HandlerConfig.DisablePooling,
                
                Url = HandlerConfig.Url,
                Method = HandlerConfig.Method,
                Headers = HandlerConfig.Headers,
                RequestBody = HandlerConfig.RequestBody,
                ContentType = HandlerConfig.ContentType,
                TimeoutSeconds = HandlerConfig.TimeoutSeconds,
                
                Path = HandlerConfig.Path,
                Delimiter = HandlerConfig.Delimiter,
                KeyNames = HandlerConfig.KeyNames,
                ValueNames = HandlerConfig.ValueNames,
                FileExtensions = HandlerConfig.FileExtensions,
                
                // Copy action/output properties
                Actions = HandlerConfig.Actions,
                OutputFormat = HandlerConfig.OutputFormat,
                Seeder = HandlerConfig.Seeder,
                ConstantSeeder = HandlerConfig.ConstantSeeder,
                UserInputs = HandlerConfig.UserInputs,
                RequiresConfirmation = HandlerConfig.RequiresConfirmation,
                
                // Copy synthetic properties (keep for reference)
                ReferenceHandler = HandlerConfig.ReferenceHandler,
                ActualType = HandlerConfig.ActualType,
                SyntheticInput = HandlerConfig.SyntheticInput,
                
                // Copy cron properties (keep for reference)
                CronJobId = HandlerConfig.CronJobId,
                CronExpression = HandlerConfig.CronExpression,
                CronTimezone = HandlerConfig.CronTimezone,
                CronEnabled = HandlerConfig.CronEnabled,
                
                // Copy UI behavior properties
                AutoFocusTab = HandlerConfig.AutoFocusTab,
                BringWindowToFront = HandlerConfig.BringWindowToFront,
                
                // ✅ Copy state properties (IMPORTANT for Enable/Disable functionality)
                Enabled = HandlerConfig.Enabled
            };

            return actualConfig;
        }

        /// <summary>
        /// Manual execution method for testing cron jobs
        /// </summary>
        public void ExecuteNow()
        {
            try
            {
                var cronService = ServiceLocator.Get<ICronService>();
                var jobId = !string.IsNullOrEmpty(HandlerConfig.CronJobId) 
                    ? HandlerConfig.CronJobId 
                    : $"cron_{HandlerConfig.Name.Replace(" ", "_").ToLower()}";
                
                var success = cronService.TriggerJob(jobId);
                if (success)
                {
                    UserFeedback.ShowActivity(LogType.Info, $"Manually triggered cron job: {HandlerConfig.Name} (ID: {jobId})");
                }
                else
                {
                    UserFeedback.ShowWarning($"Cron job not found for manual execution: {HandlerConfig.Name} (ID: {jobId})");
                }
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error manually executing cron job {HandlerConfig.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable the cron job
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            try
            {
                var cronService = ServiceLocator.Get<ICronService>();
                var jobId = !string.IsNullOrEmpty(HandlerConfig.CronJobId) 
                    ? HandlerConfig.CronJobId 
                    : $"cron_{HandlerConfig.Name.Replace(" ", "_").ToLower()}";
                
                var success = cronService.SetJobEnabled(jobId, enabled);
                if (success)
                {
                    UserFeedback.ShowActivity(LogType.Info, $"Cron job {(enabled ? "enabled" : "disabled")}: {HandlerConfig.Name} (ID: {jobId})");
                }
                else
                {
                    UserFeedback.ShowWarning($"Cron job not found for state change: {HandlerConfig.Name} (ID: {jobId})");
                }
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error setting cron job enabled state for {HandlerConfig.Name}: {ex.Message}");
            }
        }
    }
}