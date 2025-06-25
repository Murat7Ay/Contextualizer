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

        [JsonPropertyName("window_settings")]
        public WindowSettings WindowSettings { get; set; } = new WindowSettings();

        [JsonPropertyName("ui_settings")]
        public UISettings UISettings { get; set; } = new UISettings();
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

    public class WindowSettings
    {
        [JsonPropertyName("width")]
        public double Width { get; set; } = 1200;

        [JsonPropertyName("height")]
        public double Height { get; set; } = 800;

        [JsonPropertyName("left")]
        public double Left { get; set; } = double.NaN;

        [JsonPropertyName("top")]
        public double Top { get; set; } = double.NaN;

        [JsonPropertyName("window_state")]
        public string WindowState { get; set; } = "Normal";

        [JsonPropertyName("grid_splitter_position")]
        public double GridSplitterPosition { get; set; } = 200;
    }

    public class UISettings
    {
        [JsonPropertyName("toast_position_x")]
        public double ToastPositionX { get; set; } = 0;

        [JsonPropertyName("toast_position_y")]
        public double ToastPositionY { get; set; } = 0;

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "Dark";
    }
} 