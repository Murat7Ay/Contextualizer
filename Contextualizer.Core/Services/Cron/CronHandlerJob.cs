using Contextualizer.PluginContracts;
using Quartz;
using System;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services.Cron
{
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
                ServiceLocator.Get<IUserInteractionService>()?.ShowActivityFeedback(
                    LogType.Error,
                    $"CronHandlerJob: Error executing job '{jobId}': {ex.Message}");
                throw;
            }
        }
    }
}
