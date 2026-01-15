using System;

namespace Contextualizer.PluginContracts
{
    /// <summary>
    /// Provides a way to show notifications using the native WPF layer (outside the WebView/React UI).
    /// This is useful when the main app UI is in the background but you still want a visible toast-like prompt.
    /// </summary>
    public interface INativeNotificationService
    {
        void ShowNativeNotification(
            string message,
            LogType notificationType = LogType.Info,
            string title = "",
            int durationInSeconds = 5,
            Action? onActionClicked = null);
    }
}


