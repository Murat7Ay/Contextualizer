using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using WpfInteractionApp.Settings;
using Contextualizer.Core.Services;

namespace WpfInteractionApp.Services
{
    public class SettingsService : ISettingsService
    {
        private const string SettingsFileName = "appsettings.json";
        private readonly string _settingsPath;
        private AppSettings _settings;

        public SettingsService()
        {
            // Use portable application directory structure
            _settingsPath = Path.Combine(
                @"C:\PortableApps\Contextualizer\Config",
                SettingsFileName
            );
            
            // Ensure portable directories exist
            CreatePortableDirectories();
            LoadSettings();
        }

        public AppSettings Settings => _settings;

        public string HandlersFilePath => _settings.HandlersFilePath;
        public string PluginsDirectory => _settings.PluginsDirectory;
        public string ExchangeDirectory => _settings.ExchangeDirectory;
        public int ClipboardWaitTimeout => _settings.ClipboardWaitTimeout;
        public int WindowActivationDelay => _settings.WindowActivationDelay;
        public int ClipboardClearDelay => _settings.ClipboardClearDelay;
        public string ShortcutKey => _settings.KeyboardShortcut.Key;

        public bool HasModifierKey(string modifier)
        {
            return _settings.KeyboardShortcut.HasModifier(modifier);
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _settings = new AppSettings();
                    SaveSettings();
                }
            }
            catch
            {
                _settings = new AppSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception("Failed to save settings", ex);
            }
        }

        private void CreatePortableDirectories()
        {
            const string baseDir = @"C:\PortableApps\Contextualizer";
            
            try
            {
                // Create main directories
                var directories = new[]
                {
                    Path.Combine(baseDir, "Config"),
                    Path.Combine(baseDir, "Data", "Exchange"),
                    Path.Combine(baseDir, "Data", "Installed"),
                    Path.Combine(baseDir, "Data", "Logs"),
                    Path.Combine(baseDir, "Plugins"),
                    Path.Combine(baseDir, "Temp")
                };

                foreach (var dir in directories)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }

                // Create default handlers.json if it doesn't exist
                var handlersPath = Path.Combine(baseDir, "Config", "handlers.json");
                if (!File.Exists(handlersPath))
                {
                    CreateDefaultHandlersFile(handlersPath);
                }

                // Create sample exchange handler if exchange directory is empty
                var exchangeDir = Path.Combine(baseDir, "Data", "Exchange");
                if (!Directory.GetFiles(exchangeDir, "*.json").Any())
                {
                    CreateSampleExchangeHandler(exchangeDir);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Failed to create portable directory structure: {ex.Message}");
            }
        }

        private void CreateDefaultHandlersFile(string handlersPath)
        {
            var defaultHandlers = new
            {
                handlers = new object[]
                {
                    new
                    {
                        name = "Welcome Handler",
                        type = "manual",
                        screen_id = "welcome_screen",
                        title = "Welcome to Contextualizer!",
                        description = "This is a sample handler to get you started. You can create your own handlers or install them from the marketplace.",
                        actions = new[]
                        {
                            new
                            {
                                name = "show_notification",
                                message = "Welcome to Contextualizer! 🎉\n\nThis portable installation is ready to use.\n\nYou can:\n- Create custom handlers\n- Install handlers from marketplace\n- Configure settings\n- View activity logs",
                                title = "Welcome",
                                duration = 10
                            }
                        }
                    }
                }
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var json = JsonSerializer.Serialize(defaultHandlers, options);
            File.WriteAllText(handlersPath, json);
        }

        private void CreateSampleExchangeHandler(string exchangeDir)
        {
            var sampleHandler = new
            {
                id = "sample-regex-handler",
                name = "Sample Regex Handler",
                version = "1.0.0",
                author = "Contextualizer Team",
                description = "A sample regex handler that processes email addresses from clipboard content",
                tags = new[] { "sample", "regex", "email" },
                handlerJson = new
                {
                    name = "Email Extractor",
                    type = "regex",
                    screen_id = "email_screen",
                    title = "Extract Email Addresses",
                    description = "Extracts email addresses from clipboard text",
                    regex = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
                    actions = new[]
                    {
                        new
                        {
                            name = "simple_print_key",
                            key = "_self"
                        }
                    },
                    output_format = "Found email: $(_match)"
                }
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var json = JsonSerializer.Serialize(sampleHandler, options);
            var samplePath = Path.Combine(exchangeDir, "sample-regex-handler.json");
            File.WriteAllText(samplePath, json);
        }
    }
} 