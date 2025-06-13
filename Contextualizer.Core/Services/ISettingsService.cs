using System;
using WpfInteractionApp.Settings;

namespace Contextualizer.Core.Services
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        string HandlersFilePath { get; }
        string PluginsDirectory { get; }
        string ExchangeDirectory { get; }
        int ClipboardWaitTimeout { get; }
        int WindowActivationDelay { get; }
        int ClipboardClearDelay { get; }
        bool HasModifierKey(string modifier); // Ctrl, Alt, Shift, Win
        string ShortcutKey { get; }
    }
} 