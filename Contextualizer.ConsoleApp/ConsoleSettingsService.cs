using Contextualizer.Core.Services;

namespace Contextualizer.ConsoleApp
{
    internal class ConsoleSettingsService : ISettingsService
    {
        public string HandlersFilePath => "handlers.json";
        public string PluginsDirectory => "plugins";
        public string ExchangeDirectory => "exchange";
        public int ClipboardWaitTimeout => 1000;
        public int WindowActivationDelay => 500;
        public int ClipboardClearDelay => 100;
        public string ShortcutKey => "F12";

        public bool HasModifierKey(string modifier)
        {
            // Default console implementation - no modifiers
            return false;
        }
    }
}