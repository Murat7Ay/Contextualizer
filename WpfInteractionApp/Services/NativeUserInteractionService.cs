using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WpfInteractionApp.Services
{
    /// <summary>
    /// Native WPF implementation of IUserInteractionService.
    /// Shows Toast notifications and User Input dialogs as native WPF windows (outside WebView2).
    /// Use this when you need UI to appear even when WebView2 is not focused or for system-level notifications.
    /// </summary>
    public sealed class NativeUserInteractionService : IUserInteractionService, INativeNotificationService
    {
        private readonly Window? _ownerWindow;
        private readonly System.Windows.Threading.Dispatcher _dispatcher;

        public NativeUserInteractionService(Window? ownerWindow = null)
        {
            _ownerWindow = ownerWindow ?? Application.Current?.MainWindow;
            _dispatcher = Application.Current?.Dispatcher ?? System.Windows.Threading.Dispatcher.CurrentDispatcher;
        }

        public void ShowActivityFeedback(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            // Native service doesn't have an activity log panel - just debug output
            System.Diagnostics.Debug.WriteLine($"[{notificationType}] {message}");
            
            // Optionally also forward to WebView if available
            try
            {
                var webViewService = ServiceLocator.SafeGet<WebViewUserInteractionService>();
                webViewService?.ShowActivityFeedback(notificationType, message, timestamp, additionalInfo);
            }
            catch { /* ignore */ }
        }

        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            ShowActivityFeedback(notificationType, message, timestamp, additionalInfo);
        }

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, 
            List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null, 
            bool autoFocus = false, bool bringToFront = false)
        {
            // Forward to WebView service for tab-based display
            try
            {
                var webViewService = ServiceLocator.SafeGet<WebViewUserInteractionService>();
                webViewService?.ShowWindow(screenId, title, context, actions, autoFocus, bringToFront);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.ShowWindow error: {ex.Message}");
            }
        }

        public void ShowToastMessage(string message, int duration = 3)
        {
            ShowNotification(message, LogType.Info, "", duration, null);
        }

        public void ShowNotification(string message, LogType notificationType = LogType.Info, 
            string title = "", int durationInSeconds = 5, Action? onActionClicked = null)
        {
            _dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var toast = new NativeToast(message, durationInSeconds, title, notificationType);
                    toast.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.ShowNotification error: {ex.Message}");
                }
            });
        }

        public void ShowNativeNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action? onActionClicked = null)
        {
            // Alias for native-only notifications (outside WebView UI)
            ShowNotification(message, notificationType, title, durationInSeconds, onActionClicked);
        }

        public void ShowNotificationWithActions(string message, LogType notificationType = LogType.Info, 
            string title = "", int durationInSeconds = 5, params ToastAction[] actions)
        {
            _dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var toast = new NativeToast(message, durationInSeconds, title, notificationType, actions);
                    toast.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.ShowNotificationWithActions error: {ex.Message}");
                }
            });
        }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return ShowConfirmationAsync(new ConfirmationRequest { Title = title, Message = message });
        }

        public async Task<bool> ShowConfirmationAsync(ConfirmationRequest request)
        {
            try
            {
                var result = _dispatcher.Invoke(() =>
                {
                    var dialog = new NativeConfirmationDialog(request);
                    return dialog.ShowDialogSync();
                });
                return await Task.FromResult(result);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.ShowConfirmationAsync error: {ex.Message}");
                return await Task.FromResult(false);
            }
        }

        public string? GetUserInput(UserInputRequest? request)
        {
            if (request == null) return null;

            return _dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new NativeUserInputDialog(request);
                    // Don't set Owner - it can interfere with Topmost behavior
                    dialog.Topmost = true;
                    if (dialog.ShowDialog() == true)
                        return dialog.UserInput;
                    
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.GetUserInput error: {ex.Message}");
                    return null;
                }
            });
        }

        public NavigationResult GetUserInputWithNavigation(UserInputRequest request, 
            Dictionary<string, string> context, bool canGoBack, int currentStep, int totalSteps)
        {
            return _dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new NativeUserInputDialog(request, context, canGoBack, currentStep, totalSteps);
                    // Don't set Owner - it can interfere with Topmost behavior
                    // Dialog is already set to Topmost and CenterScreen in XAML
                    dialog.Topmost = true;
                    return dialog.ShowNavigationDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.GetUserInputWithNavigation error: {ex.Message}");
                    return new NavigationResult { Action = NavigationAction.Cancel };
                }
            });
        }

        public async Task ShowActionableNotification(string message, string actionLabel, Action action, 
            LogType notificationType = LogType.Info)
        {
            var actions = new[]
            {
                new ToastAction
                {
                    Text = actionLabel,
                    Action = action,
                    Style = ToastActionStyle.Primary,
                    CloseOnClick = true
                },
                new ToastAction
                {
                    Text = "Dismiss",
                    Action = () => { },
                    Style = ToastActionStyle.Secondary,
                    CloseOnClick = true,
                    IsDefaultAction = true
                }
            };

            await _dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var toast = new NativeToast(message, 10, "Action Required", notificationType, actions);
                    toast.Show();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"NativeUserInteractionService.ShowActionableNotification error: {ex.Message}");
                }
            });
        }
    }
}

