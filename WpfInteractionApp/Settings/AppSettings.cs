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

        [JsonPropertyName("exchange_directory")]
        public string ExchangeDirectory { get; set; } = @"C:\Finder\Exchange";

        [JsonPropertyName("keyboard_shortcut")]
        public KeyboardShortcut KeyboardShortcut { get; set; } = new KeyboardShortcut();

        [JsonPropertyName("clipboard_wait_timeout")]
        public int ClipboardWaitTimeout { get; set; } = 5;

        [JsonPropertyName("window_activation_delay")]
        public int WindowActivationDelay { get; set; } = 100;

        [JsonPropertyName("clipboard_clear_delay")]
        public int ClipboardClearDelay { get; set; } = 800;

        [JsonPropertyName("plugin_settings")]
        public PluginSettings PluginSettings { get; set; } = new PluginSettings();
    }

    public class PluginSettings
    {
        [JsonPropertyName("installed_directory")]
        public string InstalledDirectory { get; set; } = "Installed";

        [JsonPropertyName("cache_directory")]
        public string CacheDirectory { get; set; } = "Cache";

        [JsonPropertyName("temp_directory")]
        public string TempDirectory { get; set; } = "Temp";

        [JsonPropertyName("auto_update")]
        public bool AutoUpdate { get; set; } = true;

        [JsonPropertyName("validate_on_load")]
        public bool ValidateOnLoad { get; set; } = true;
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