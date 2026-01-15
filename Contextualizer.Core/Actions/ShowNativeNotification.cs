using Contextualizer.PluginContracts;
using System.Threading.Tasks;

namespace Contextualizer.Core.Actions
{
    /// <summary>
    /// Shows a notification using the native WPF layer (outside WebView/React UI).
    /// Useful when the application is in the background and WebView-based UI is not visible.
    /// </summary>
    public class ShowNativeNotification : IAction
    {
        private IPluginServiceProvider? _pluginServiceProvider;

        public string Name => "show_native_notification";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            var titleNotification = context.TryGetValue(ContextKey._notification_title, out var title)
                ? title
                : "Notification";

            var titleDuration = context.TryGetValue(ContextKey._duration, out var duration) && int.TryParse(duration, out var parsedDuration)
                ? parsedDuration
                : 5;

            var message = context[action.Key];

            // Prefer native notification service when available; fall back to standard ShowNotification.
            var native = _pluginServiceProvider?.GetService<INativeNotificationService>();
            if (native != null)
            {
                native.ShowNativeNotification(message, LogType.Info, titleNotification, titleDuration, null);
                return Task.CompletedTask;
            }

            var userInteractionService = _pluginServiceProvider?.GetService<IUserInteractionService>();
            userInteractionService?.ShowNotification(message, LogType.Info, titleNotification, durationInSeconds: titleDuration, onActionClicked: null);

            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _pluginServiceProvider = serviceProvider;
        }
    }
}


