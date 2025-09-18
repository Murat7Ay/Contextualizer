using System;
using System.Text.Json.Serialization;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp.Settings
{
    public class AppSettings
    {
        [JsonPropertyName("handlers_file_path")]
        public string HandlersFilePath { get; set; } = @"C:\PortableApps\Contextualizer\Config\handlers.json";

        [JsonPropertyName("plugins_directory")]
        public string PluginsDirectory { get; set; } = @"C:\PortableApps\Contextualizer\Plugins";

        [JsonPropertyName("exchange_directory")]
        public string ExchangeDirectory { get; set; } = @"C:\PortableApps\Contextualizer\Data\Exchange";

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

        [JsonPropertyName("logging_settings")]
        public LoggingSettings LoggingSettings { get; set; } = new LoggingSettings();
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

        [JsonPropertyName("settings_window")]
        public WindowPosition SettingsWindow { get; set; } = new WindowPosition();

        [JsonPropertyName("exchange_window")]
        public WindowPosition ExchangeWindow { get; set; } = new WindowPosition();
    }

    public class WindowPosition
    {
        [JsonPropertyName("left")]
        public double Left { get; set; } = 0;

        [JsonPropertyName("top")]
        public double Top { get; set; } = 0;

        [JsonPropertyName("width")]
        public double Width { get; set; } = 0;

        [JsonPropertyName("height")]
        public double Height { get; set; } = 0;
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

    public class LoggingSettings
    {
        [JsonPropertyName("enable_local_logging")]
        public bool EnableLocalLogging { get; set; } = true;

        [JsonPropertyName("enable_usage_tracking")]
        public bool EnableUsageTracking { get; set; } = true;

        [JsonPropertyName("local_log_path")]
        public string LocalLogPath { get; set; } = "";

        [JsonPropertyName("usage_endpoint_url")]
        public string? UsageEndpointUrl { get; set; } = "http://localhost:5678/webhook/api/usage";

        [JsonPropertyName("minimum_log_level")]
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;

        [JsonPropertyName("max_log_file_size_mb")]
        public int MaxLogFileSizeMB { get; set; } = 10;

        [JsonPropertyName("max_log_file_count")]
        public int MaxLogFileCount { get; set; } = 5;

        [JsonPropertyName("enable_debug_mode")]
        public bool EnableDebugMode { get; set; } = false;

        public LoggingConfiguration ToLoggingConfiguration()
        {
            var defaultPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Contextualizer", "logs");
            return new LoggingConfiguration
            {
                EnableLocalLogging = EnableLocalLogging,
                EnableUsageTracking = EnableUsageTracking,
                LocalLogPath = string.IsNullOrEmpty(LocalLogPath) ? defaultPath : LocalLogPath,
                UsageEndpointUrl = UsageEndpointUrl,
                MinimumLogLevel = MinimumLogLevel,
                MaxLogFileSizeMB = MaxLogFileSizeMB,
                MaxLogFileCount = MaxLogFileCount,
                EnableDebugMode = EnableDebugMode
            };
        }

        public void FromLoggingConfiguration(LoggingConfiguration config)
        {
            EnableLocalLogging = config.EnableLocalLogging;
            EnableUsageTracking = config.EnableUsageTracking;
            LocalLogPath = config.LocalLogPath;
            UsageEndpointUrl = config.UsageEndpointUrl;
            MinimumLogLevel = config.MinimumLogLevel;
            MaxLogFileSizeMB = config.MaxLogFileSizeMB;
            MaxLogFileCount = config.MaxLogFileCount;
            EnableDebugMode = config.EnableDebugMode;
        }
    }
} 