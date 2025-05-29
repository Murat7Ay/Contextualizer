using System;

namespace Contextualizer.Core.Services
{
    public interface ISettingsService
    {
        string HandlersFilePath { get; }
        string PluginsDirectory { get; }
        int ClipboardWaitTimeout { get; }
        int WindowActivationDelay { get; }
        int ClipboardClearDelay { get; }
        bool HasModifierKey(string modifier); // Ctrl, Alt, Shift, Win
        string ShortcutKey { get; }
    }
} 