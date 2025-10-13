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
        public int ClipboardWaitTimeout { get; set; } = 5000;

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

        [JsonPropertyName("config_system")]
        public ConfigSystemSettings ConfigSystem { get; set; } = new ConfigSystemSettings();
    }

    public class ConfigSystemSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("config_file_path")]
        public string ConfigFilePath { get; set; } = @"C:\PortableApps\Contextualizer\Config\config.ini";

        [JsonPropertyName("secrets_file_path")]
        public string SecretsFilePath { get; set; } = @"C:\PortableApps\Contextualizer\Config\secrets.ini";

        [JsonPropertyName("auto_create_files")]
        public bool AutoCreateFiles { get; set; } = true;

        [JsonPropertyName("file_format")]
        public string FileFormat { get; set; } = "ini";
    }

    public class KeyboardShortcut
    {
        [JsonPropertyName("modifier_keys")]
        public string[] ModifierKeys { get; set; } = new[] { "Win" };

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

        [JsonPropertyName("skipped_update_version")]
        public string? SkippedUpdateVersion { get; set; } = null;

        [JsonPropertyName("last_update_check")]
        public DateTime? LastUpdateCheck { get; set; } = null;

        [JsonPropertyName("network_update_settings")]
        public NetworkUpdateSettings NetworkUpdateSettings { get; set; } = new NetworkUpdateSettings();

        [JsonPropertyName("initial_deployment_settings")]
        public InitialDeploymentSettings InitialDeploymentSettings { get; set; } = new InitialDeploymentSettings();
    }

    public class InitialDeploymentSettings
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("source_path")]
        public string SourcePath { get; set; } = @"G:\_IBTECHOrtak\Nakit Yönetimi\Contextualizer\InitialFiles";

        [JsonPropertyName("is_completed")]
        public bool IsCompleted { get; set; } = false;

        [JsonPropertyName("copy_exchange_handlers")]
        public bool CopyExchangeHandlers { get; set; } = true;

        [JsonPropertyName("copy_installed_handlers")]
        public bool CopyInstalledHandlers { get; set; } = true;

        [JsonPropertyName("copy_plugins")]
        public bool CopyPlugins { get; set; } = true;
    }

    public class LoggingSettings
    {
        [JsonPropertyName("enable_local_logging")]
        public bool EnableLocalLogging { get; set; } = true;

        [JsonPropertyName("enable_usage_tracking")]
        public bool EnableUsageTracking { get; set; } = true;

        [JsonPropertyName("local_log_path")]
        public string LocalLogPath { get; set; } = @"C:\PortableApps\Contextualizer\Data\Logs";

        [JsonPropertyName("usage_endpoint_url")]
        public string? UsageEndpointUrl { get; set; } = "https://coe-n8n.seip-vip-tst-ocpdev51.qnb.com.tr/webhook/contextualizer/api/usage";

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
            return new LoggingConfiguration
            {
                EnableLocalLogging = this.EnableLocalLogging,
                EnableUsageTracking = this.EnableUsageTracking,
                LocalLogPath = this.LocalLogPath,
                UsageEndpointUrl = this.UsageEndpointUrl,
                MinimumLogLevel = this.MinimumLogLevel,
                MaxLogFileSizeMB = this.MaxLogFileSizeMB,
                MaxLogFileCount = this.MaxLogFileCount,
                EnableDebugMode = this.EnableDebugMode
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

    public class NetworkUpdateSettings
    {
        [JsonPropertyName("enable_network_updates")]
        public bool EnableNetworkUpdates { get; set; } = true;

        [JsonPropertyName("network_update_path")]
        public string NetworkUpdatePath { get; set; } = @"G:\_IBTECHOrtak\Nakit Yönetimi\Contextualizer\Updates";
 
        [JsonPropertyName("update_script_path")]
        public string UpdateScriptPath { get; set; } = @"G:\_IBTECHOrtak\Nakit Yönetimi\Contextualizer\Updates\install_update.bat";
        [JsonPropertyName("check_interval_hours")]
        public int CheckIntervalHours { get; set; } = 24;

        [JsonPropertyName("auto_install_non_mandatory")]
        public bool AutoInstallNonMandatory { get; set; } = false;

        [JsonPropertyName("auto_install_mandatory")]
        public bool AutoInstallMandatory { get; set; } = true;
    }
} 