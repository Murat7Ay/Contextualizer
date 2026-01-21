using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WpfInteractionApp
{
    /// <summary>
    /// IUserInteractionService implementation for the React/WebView2 shell.
    /// - Activity/logging: forwarded to React Activity Log via WebView2 postMessage.
    /// - ShowWindow: opens a React tab via host message.
    /// - Prompts/toasts: rendered by the React UI (WebView2) via request/response messaging.
    /// </summary>
    public sealed class WebViewUserInteractionService : IUserInteractionService
    {
        private readonly ReactShellWindow _shellWindow;
        private static readonly TimeSpan UiTimeout = TimeSpan.FromMinutes(30);

        public WebViewUserInteractionService(ReactShellWindow shellWindow)
        {
            _shellWindow = shellWindow ?? throw new ArgumentNullException(nameof(shellWindow));
        }

        public void ShowActivityFeedback(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            try
            {
                _shellWindow.PostLogToUi(notificationType, message, additionalInfo);
            }
            catch
            {
                // ignore
            }
        }

        [Obsolete("Use ShowActivityFeedback instead for clarity")]
        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            ShowActivityFeedback(notificationType, message, timestamp, additionalInfo);
        }

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null, bool autoFocus = false, bool bringToFront = false)
        {
            _shellWindow.PostOpenTabToUi(screenId, title, context, actions, autoFocus, bringToFront);
        }

        public void ShowToastMessage(string message, int duration = 3)
        {
            ShowNotification(message, LogType.Info, string.Empty, duration, null);
        }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return ShowConfirmationAsync(new ConfirmationRequest { Title = title, Message = message });
        }

        public async Task<bool> ShowConfirmationAsync(ConfirmationRequest request)
        {
            try
            {
                return await _shellWindow.RequestConfirmAsync(request, UiTimeout);
            }
            catch (Exception ex)
            {
                ShowActivityFeedback(LogType.Error, $"Failed to show confirmation: {ex.Message}");
                return false;
            }
        }

        public string? GetUserInput(UserInputRequest? request)
        {
            if (request == null) return null;

            try
            {
                var response = WaitSync(_shellWindow.RequestUserInputAsync(request, null, UiTimeout));
                if (response.Cancelled)
                    return null;

                return response.Value;
            }
            catch (Exception ex)
            {
                ShowActivityFeedback(LogType.Error, $"Kullanıcı girişi alınamadı: {ex.Message}");
                return null;
            }
        }

        public NavigationResult GetUserInputWithNavigation(UserInputRequest request, Dictionary<string, string> context, bool canGoBack, int currentStep, int totalSteps)
        {
            try
            {
                return WaitSync(_shellWindow.RequestUserInputWithNavigationAsync(request, context, canGoBack, currentStep, totalSteps, UiTimeout));
            }
            catch (Exception ex)
            {
                ShowActivityFeedback(LogType.Error, $"Navigation kullanıcı girişi alınamadı: {ex.Message}");
                return new NavigationResult { Action = NavigationAction.Cancel };
            }
        }

        public void ShowNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action? onActionClicked = null)
        {
            try
            {
                // Show a toast in the React UI. (Optional onActionClicked is not used currently by callers.)
                _shellWindow.PostToastToUi(notificationType, message, string.IsNullOrWhiteSpace(title) ? null : title, durationInSeconds);
            }
            catch (Exception ex)
            {
                ShowActivityFeedback(LogType.Error, $"Bildirim gösterilemedi: {ex.Message}");
            }
        }

        public void ShowNotificationWithActions(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, params ToastAction[] actions)
        {
            try
            {
                // Actionable toast: render buttons in React and execute callbacks in host via WebView2 RPC.
                _shellWindow.PostToastToUi(notificationType, message, string.IsNullOrWhiteSpace(title) ? null : title, durationInSeconds, details: null, actions: actions);
            }
            catch (Exception ex)
            {
                ShowActivityFeedback(LogType.Error, $"Bildirim gösterilemedi: {ex.Message}");
            }
        }

        public async Task ShowActionableNotification(string message, string actionLabel, Action action, LogType notificationType = LogType.Info)
        {
            try
            {
                bool confirmed = await ShowConfirmationAsync("Eylem Gerekli", $"{message}\n\n{actionLabel} işlemini gerçekleştirmek istiyor musunuz?");
                if (confirmed)
                {
                    action?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ShowActivityFeedback(LogType.Error, $"Eyleme bağlı bildirim gösterilemedi: {ex.Message}");
            }
        }

        private static T WaitSync<T>(Task<T> task)
        {
            if (task.IsCompleted)
                return task.GetAwaiter().GetResult();

            // If we're not on the WPF dispatcher thread, a normal blocking wait is fine.
            if (Application.Current?.Dispatcher == null || !Application.Current.Dispatcher.CheckAccess())
                return task.GetAwaiter().GetResult();

            // We're on the dispatcher thread: pump a nested message loop (similar to ShowDialog)
            // so WebView2 message callbacks can still run and complete the task.
            var frame = new DispatcherFrame();
            T? result = default;
            Exception? error = null;

            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsCanceled)
                        error = new TaskCanceledException(t);
                    else if (t.IsFaulted)
                        error = t.Exception?.GetBaseException() ?? t.Exception;
                    else
                        result = t.Result;
                }
                finally
                {
                    frame.Continue = false;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            Dispatcher.PushFrame(frame);

            if (error != null)
                throw error;

            return result!;
        }
    }
}


