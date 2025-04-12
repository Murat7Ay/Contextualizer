using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public interface IUserInteractionService
    {
        public void ShowNotification(
            string message,
            NotificationType notificationType = NotificationType.Info,
            string title = "",
            int durationInSeconds = 5,
            Action? onActionClicked = null);

        public void ShowActionableNotification(
            string message,
            string actionLabel,
            Action action,
            NotificationType notificationType = NotificationType.Info);

        public void Log(
            NotificationType notificationType,
            string message,
            DateTime? timestamp = null,
            string? additionalInfo = null);

        public Task<bool> ShowConfirmationAsync(string title, string message);
        public string? GetUserInput(UserInputRequest? request);
    }
}
