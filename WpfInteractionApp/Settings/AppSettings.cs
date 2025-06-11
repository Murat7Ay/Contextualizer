using System;
using System.Text.Json.Serialization;

namespace WpfInteractionApp.Settings
{
    public class AppSettings
    {
        [JsonPropertyName("handlers_file_path")]
        public string HandlersFilePath { get; set; } = @"C:\Finder\handlers.json";

        [JsonPropertyName("plugins_directory")]
        public string PluginsDirectory { get; set; } = @"C:\Finder\Plugins";

        [JsonPropertyName("marketplace_directory")]
        public string MarketplaceDirectory { get; set; } = @"C:\Finder\Marketplace";

        [JsonPropertyName("keyboard_shortcut")]
        public KeyboardShortcut KeyboardShortcut { get; set; } = new KeyboardShortcut();

        [JsonPropertyName("clipboard_wait_timeout")]
        public int ClipboardWaitTimeout { get; set; } = 5;

        [JsonPropertyName("window_activation_delay")]
        public int WindowActivationDelay { get; set; } = 100;

        [JsonPropertyName("clipboard_clear_delay")]
        public int ClipboardClearDelay { get; set; } = 800;
    }

    public class KeyboardShortcut
    {
        [JsonPropertyName("modifier_keys")]
        public string[] ModifierKeys { get; set; } = new[] { "Ctrl" };

        [JsonPropertyName("key")]
        public string Key { get; set; } = "W";

        public bool HasModifier(string modifier)
        {
            return ModifierKeys.Contains(modifier, StringComparer.OrdinalIgnoreCase);
        }

        public void SetModifier(string modifier, bool value)
        {
            var modifiers = ModifierKeys.ToList();
            if (value && !modifiers.Contains(modifier, StringComparer.OrdinalIgnoreCase))
            {
                modifiers.Add(modifier);
            }
            else if (!value)
            {
                modifiers.RemoveAll(m => m.Equals(modifier, StringComparison.OrdinalIgnoreCase));
            }
            ModifierKeys = modifiers.ToArray();
        }
    }
} 