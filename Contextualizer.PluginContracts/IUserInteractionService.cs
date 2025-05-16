using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public interface IUserInteractionService
    {
        public void ShowNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action? onActionClicked = null);

        public Task ShowActionableNotification(string message, string actionLabel, Action action, LogType notificationType = LogType.Info);

        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null);

        public void ShowWindow(string screenId, string title, string body, Dictionary<string, string> context, List<KeyValuePair<string,Action<Dictionary<string,string>>>>? actions = null);

        public void ShowToastMessage(string message, int duration = 3);

        public Task<bool> ShowConfirmationAsync(string title, string message);
        public string? GetUserInput(UserInputRequest? request);
    }
}
