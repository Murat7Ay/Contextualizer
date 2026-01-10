using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WpfInteractionApp.Services
{
    /// <summary>
    /// Routes user interaction requests to either WebView-based or Native WPF implementations.
    /// Allows runtime switching between modes and provides a unified API.
    /// 
    /// Usage:
    /// - Default mode is WebView (shown inside React UI)
    /// - Call UseNativeMode() for system-level notifications outside WebView
    /// - Call UseWebViewMode() to switch back
    /// - Use ForceNative() for one-off native calls without changing default mode
    /// </summary>
    public sealed class UserInteractionServiceRouter : IUserInteractionService
    {
        private readonly WebViewUserInteractionService _webViewService;
        private readonly NativeUserInteractionService _nativeService;
        
        private UserInteractionMode _currentMode = UserInteractionMode.WebView;

        public UserInteractionServiceRouter(WebViewUserInteractionService webViewService, NativeUserInteractionService nativeService)
        {
            _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
            _nativeService = nativeService ?? throw new ArgumentNullException(nameof(nativeService));
        }

        /// <summary>
        /// Current interaction mode (WebView or Native)
        /// </summary>
        public UserInteractionMode CurrentMode => _currentMode;

        /// <summary>
        /// Switch to WebView mode - UI shown inside React
        /// </summary>
        public void UseWebViewMode() => _currentMode = UserInteractionMode.WebView;

        /// <summary>
        /// Switch to Native mode - UI shown as WPF windows outside WebView
        /// </summary>
        public void UseNativeMode() => _currentMode = UserInteractionMode.Native;

        /// <summary>
        /// Get direct access to WebView service
        /// </summary>
        public WebViewUserInteractionService WebView => _webViewService;

        /// <summary>
        /// Get direct access to Native service
        /// </summary>
        public NativeUserInteractionService Native => _nativeService;

        private IUserInteractionService Current => _currentMode == UserInteractionMode.Native 
            ? _nativeService 
            : _webViewService;

        #region IUserInteractionService Implementation

        public void ShowNotification(string message, LogType notificationType = LogType.Info, 
            string title = "", int durationInSeconds = 5, Action? onActionClicked = null)
        {
            Current.ShowNotification(message, notificationType, title, durationInSeconds, onActionClicked);
        }

        public Task ShowActionableNotification(string message, string actionLabel, Action action, 
            LogType notificationType = LogType.Info)
        {
            return Current.ShowActionableNotification(message, actionLabel, action, notificationType);
        }

        public void ShowActivityFeedback(LogType notificationType, string message, 
            DateTime? timestamp = null, string? additionalInfo = null)
        {
            Current.ShowActivityFeedback(notificationType, message, timestamp, additionalInfo);
        }

        public void Log(LogType notificationType, string message, DateTime? timestamp = null, 
            string? additionalInfo = null)
        {
            Current.Log(notificationType, message, timestamp, additionalInfo);
        }

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, 
            List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null, 
            bool autoFocus = false, bool bringToFront = false)
        {
            // ShowWindow always goes to WebView (for React tabs)
            _webViewService.ShowWindow(screenId, title, context, actions, autoFocus, bringToFront);
        }

        public void ShowToastMessage(string message, int duration = 3)
        {
            Current.ShowToastMessage(message, duration);
        }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            return Current.ShowConfirmationAsync(title, message);
        }

        public string? GetUserInput(UserInputRequest? request)
        {
            return Current.GetUserInput(request);
        }

        public NavigationResult GetUserInputWithNavigation(UserInputRequest request, 
            Dictionary<string, string> context, bool canGoBack, int currentStep, int totalSteps)
        {
            return Current.GetUserInputWithNavigation(request, context, canGoBack, currentStep, totalSteps);
        }

        public void ShowNotificationWithActions(string message, LogType notificationType = LogType.Info, 
            string title = "", int durationInSeconds = 5, params ToastAction[] actions)
        {
            Current.ShowNotificationWithActions(message, notificationType, title, durationInSeconds, actions);
        }

        #endregion

        #region Force Mode Methods (for one-off calls)

        /// <summary>
        /// Show a native toast notification without changing the current mode
        /// </summary>
        public void ShowNativeToast(string message, LogType notificationType = LogType.Info, 
            string title = "", int durationInSeconds = 5, params ToastAction[] actions)
        {
            _nativeService.ShowNotificationWithActions(message, notificationType, title, durationInSeconds, actions);
        }

        /// <summary>
        /// Show a native user input dialog without changing the current mode
        /// </summary>
        public string? GetNativeUserInput(UserInputRequest request)
        {
            return _nativeService.GetUserInput(request);
        }

        /// <summary>
        /// Show a native confirmation dialog without changing the current mode
        /// </summary>
        public Task<bool> ShowNativeConfirmationAsync(string title, string message)
        {
            return _nativeService.ShowConfirmationAsync(title, message);
        }

        #endregion
    }

    /// <summary>
    /// User interaction display mode
    /// </summary>
    public enum UserInteractionMode
    {
        /// <summary>
        /// Show UI inside WebView2 (React-based)
        /// </summary>
        WebView,
        
        /// <summary>
        /// Show UI as native WPF windows (outside WebView2)
        /// </summary>
        Native
    }
}

