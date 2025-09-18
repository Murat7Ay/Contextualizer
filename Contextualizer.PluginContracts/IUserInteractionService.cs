using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface IUserInteractionService
    {
        public void ShowNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action? onActionClicked = null);

        public Task ShowActionableNotification(string message, string actionLabel, Action action, LogType notificationType = LogType.Info);

        /// <summary>
        /// Shows activity feedback to user in the UI activity log panel
        /// </summary>
        public void ShowActivityFeedback(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null);
        
        /// <summary>
        /// Legacy method - use ShowActivityFeedback instead
        /// </summary>
        [Obsolete("Use ShowActivityFeedback instead for clarity")]
        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null);

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, List<KeyValuePair<string,Action<Dictionary<string,string>>>>? actions = null);

        public void ShowToastMessage(string message, int duration = 3);

        public Task<bool> ShowConfirmationAsync(string title, string message);
        public string? GetUserInput(UserInputRequest? request);
        public NavigationResult GetUserInputWithNavigation(UserInputRequest request, Dictionary<string, string> context, bool canGoBack, int currentStep, int totalSteps);
        public void ShowNotificationWithActions(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, params ToastAction[] actions);
    }
}
