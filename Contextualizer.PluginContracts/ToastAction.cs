using System;

namespace Contextualizer.PluginContracts
{
    public class ToastAction
    {
        public string Text { get; set; } = string.Empty;
        public Action Action { get; set; } = () => { };
        public ToastActionStyle Style { get; set; } = ToastActionStyle.Secondary;
        public bool CloseOnClick { get; set; } = true;
        public bool IsDefaultAction { get; set; } = false; // Timeout veya close button için default action
    }

    public enum ToastActionStyle
    {
        Primary,    // Main action button (e.g., "Yes", "Confirm")
        Secondary,  // Secondary action button (e.g., "No", "Cancel")
        Danger      // Destructive action (e.g., "Delete", "Remove")
    }

    public static class ToastActions
    {
        // Common action presets
        public static ToastAction Yes(Action action) => new ToastAction
        {
            Text = "Yes",
            Action = action,
            Style = ToastActionStyle.Primary
        };

        public static ToastAction No(Action action) => new ToastAction
        {
            Text = "No", 
            Action = action,
            Style = ToastActionStyle.Secondary
        };

        public static ToastAction Ok(Action action) => new ToastAction
        {
            Text = "OK",
            Action = action,
            Style = ToastActionStyle.Primary
        };

        public static ToastAction Cancel(Action action) => new ToastAction
        {
            Text = "Cancel",
            Action = action,
            Style = ToastActionStyle.Secondary
        };

        public static ToastAction Delete(Action action) => new ToastAction
        {
            Text = "Delete",
            Action = action,
            Style = ToastActionStyle.Danger
        };

        public static ToastAction Confirm(Action action) => new ToastAction
        {
            Text = "Confirm",
            Action = action,
            Style = ToastActionStyle.Primary
        };

        public static ToastAction Retry(Action action) => new ToastAction
        {
            Text = "Retry",
            Action = action,
            Style = ToastActionStyle.Primary
        };

        public static ToastAction Dismiss(Action action) => new ToastAction
        {
            Text = "Dismiss",
            Action = action,
            Style = ToastActionStyle.Secondary
        };

        // Default action variants (for timeout/close behavior)
        public static ToastAction DefaultYes(Action action) => new ToastAction
        {
            Text = "Yes",
            Action = action,
            Style = ToastActionStyle.Primary,
            IsDefaultAction = true
        };

        public static ToastAction DefaultNo(Action action) => new ToastAction
        {
            Text = "No",
            Action = action,
            Style = ToastActionStyle.Secondary,
            IsDefaultAction = true
        };

        public static ToastAction DefaultCancel(Action action) => new ToastAction
        {
            Text = "Cancel",
            Action = action,
            Style = ToastActionStyle.Secondary,
            IsDefaultAction = true
        };

        public static ToastAction DefaultDismiss(Action action) => new ToastAction
        {
            Text = "Dismiss",
            Action = action,
            Style = ToastActionStyle.Secondary,
            IsDefaultAction = true
        };
    }
}