using Contextualizer.PluginContracts;
using System;

namespace Contextualizer.Core
{
    /// <summary>
    /// Convenience class for showing user feedback in the UI activity panel
    /// This is separate from ILoggingService which is for system/technical logging
    /// </summary>
    public static class UserFeedback
    {
        /// <summary>
        /// Shows success feedback to user in UI activity panel
        /// </summary>
        public static void ShowSuccess(string message)
        {
            ServiceLocator.SafeExecute<IUserInteractionService>(ui => 
                ui.ShowActivityFeedback(LogType.Info, message));
        }

        /// <summary>
        /// Shows error feedback to user in UI activity panel
        /// </summary>
        public static void ShowError(string message)
        {
            ServiceLocator.SafeExecute<IUserInteractionService>(ui => 
                ui.ShowActivityFeedback(LogType.Error, message));
        }

        /// <summary>
        /// Shows warning feedback to user in UI activity panel
        /// </summary>
        public static void ShowWarning(string message)
        {
            ServiceLocator.SafeExecute<IUserInteractionService>(ui => 
                ui.ShowActivityFeedback(LogType.Warning, message));
        }

        /// <summary>
        /// Shows debug feedback to user in UI activity panel
        /// </summary>
        public static void ShowDebug(string message)
        {
            ServiceLocator.SafeExecute<IUserInteractionService>(ui => 
                ui.ShowActivityFeedback(LogType.Debug, message));
        }

        /// <summary>
        /// Shows general activity feedback to user in UI activity panel
        /// </summary>
        public static void ShowActivity(LogType type, string message)
        {
            ServiceLocator.SafeExecute<IUserInteractionService>(ui => 
                ui.ShowActivityFeedback(type, message));
        }
    }
}
