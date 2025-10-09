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

                // Create sample exchange handlers if exchange directory is empty
                var exchangeDir = Path.Combine(baseDir, "Data", "Exchange");
                if (!Directory.GetFiles(exchangeDir, "*.json").Any())
                {
                    CreateSampleExchangeHandler(exchangeDir);
                    CreateJsonFormatterExchangeHandler(exchangeDir);
                    CreateXmlFormatterExchangeHandler(exchangeDir);
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
                        name = "Hoş Geldiniz",
                        type = "manual",
                        description = "Başlangıç için örnek bir handler. Kendi handler'larınızı oluşturabilir veya exchange'den yükleyebilirsiniz.",
                        actions = new[]
                        {
                            new
                            {
                                name = "show_notification",
                                key = "message",
                                seeder = new
                                {
                                    message = "Contextualizer'a Hoş Geldiniz! 🎉\n\nTaşınabilir kurulum kullanıma hazır.\n\nYapabilecekleriniz:\n- Özel handler'lar oluşturun\n- Exchange'den handler yükleyin\n- Ayarları yapılandırın\n- Aktivite loglarını görüntüleyin",
                                    _notification_title = "Hoş Geldiniz",
                                    _duration = "10"
                                }
                            }
                        }
                    },
                    new
                    {
                        name = "Dokümantasyon",
                        type = "manual",
                        screen_id = "url_viewer",
                        title = "Dokümantasyon",
                        actions = new[]
                        {
                            new
                            {
                                name = "show_window",
                                key = "url"
                            }
                        },
                        constant_seeder = new
                        {
                            url = "file:///G:/_IBTECHOrtak/Nakit Yönetimi/Contextualizer/docs/index.html"
                        }
                    },
                    new
                    {
                        name = "Json formatter wpf",
                        type = "custom",
                        context_provider = "jsonvalidator",
                        validator = "jsonvalidator",
                        screen_id = "jsonformatter",
                        title = "JSON Formatter",
                        actions = new[]
                        {
                            new
                            {
                                name = "show_window",
                                key = "_input"
                            },
                            new
                            {
                                name = "copytoclipboard",
                                key = "_formatted_output"
                            }
                        }
                    },
                    new
                    {
                        name = "Xml formatter wpf",
                        type = "custom",
                        context_provider = "xmlvalidator",
                        screen_id = "xmlformatter",
                        validator = "xmlvalidator",
                        title = "XML Formatter",
                        actions = new[]
                        {
                            new
                            {
                                name = "show_window",
                                key = "_input"
                            },
                            new
                            {
                                name = "copytoclipboard",
                                key = "_formatted_output"
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
                    name = "Örnek Regex Handler",
                    version = "1.0.0",
                    author = "Contextualizer Ekibi",
                    description = "Panodaki içeriği işleyen örnek bir regex handler",
                    tags = new[] { "örnek", "regex", "merhaba" },
                    handlerJson = new
                    {
                        name = "Merhaba",
                        title = "İlk Handler",
                        type = "regex",
                        screen_id = "markdown2",
                        bring_window_to_front = true,
                        auto_focus_tab = true,
                        regex = @"\b\w{3,}\b",
                        actions = new[]
                        {
                            new
                            {
                                name = "show_window",
                                key = "_formatted_output"
                            }
                        },
                        output_format = "👋 Merhaba $func:{{$(_match) | string.upper}}<br>Hoş geldiniz!!"
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

        private void CreateJsonFormatterExchangeHandler(string exchangeDir)
        {
            var jsonFormatterHandler = new
            {
                id = "json-formatter-wpf",
                name = "Json formatter wpf",
                version = "1.0.0",
                author = "Murat",
                description = "JSON içeriğini formatlar ve doğrular",
                tags = new[] { "custom", "json", "formatter", "validator" },
                dependencies = new[] { "show_window", "copytoclipboard", "show_notification" },
                handlerJson = new
                {
                    name = "Json formatter wpf",
                    type = "custom",
                    context_provider = "jsonvalidator",
                    validator = "jsonvalidator",
                    screen_id = "jsonformatter",
                    title = "JSON Formatter",
                    actions = new[]
                    {
                        new
                        {
                            name = "show_window",
                            key = "_input"
                        },
                        new
                        {
                            name = "copytoclipboard",
                            key = "_formatted_output"
                        }
                    }
                },
                metadata = new
                {
                    category = "Utility",
                    handlerType = "custom"
                }
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var json = JsonSerializer.Serialize(jsonFormatterHandler, options);
            
            // Exchange klasörüne yaz
            var exchangeFilePath = Path.Combine(exchangeDir, "json-formatter-wpf.json");
            File.WriteAllText(exchangeFilePath, json);
            
            // Installed klasörüne de yaz (yüklenmiş gibi görünsün)
            var installedDir = Path.Combine(Path.GetDirectoryName(exchangeDir), "Installed");
            var installedFilePath = Path.Combine(installedDir, "json-formatter-wpf.json");
            File.WriteAllText(installedFilePath, json);
        }

        private void CreateXmlFormatterExchangeHandler(string exchangeDir)
        {
            var xmlFormatterHandler = new
            {
                id = "xml-formatter-wpf",
                name = "Xml formatter wpf",
                version = "1.0.0",
                author = "Murat",
                description = "XML içeriğini formatlar ve doğrular",
                tags = new[] { "custom", "xml", "formatter", "validator" },
                dependencies = new[] { "show_window" },
                handlerJson = new
                {
                    name = "Xml formatter wpf",
                    type = "custom",
                    context_provider = "xmlvalidator",
                    screen_id = "xmlformatter",
                    validator = "xmlvalidator",
                    title = "XML Formatter",
                    actions = new[]
                    {
                        new
                        {
                            name = "show_window",
                            key = "_input"
                        },
                        new
                        {
                            name = "copytoclipboard",
                            key = "_formatted_output"
                        }
                    }
                },
                metadata = new
                {
                    category = "Utility",
                    handlerType = "custom"
                }
            };

            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var json = JsonSerializer.Serialize(xmlFormatterHandler, options);
            
            // Exchange klasörüne yaz
            var exchangeFilePath = Path.Combine(exchangeDir, "xml-formatter-wpf.json");
            File.WriteAllText(exchangeFilePath, json);
            
            // Installed klasörüne de yaz (yüklenmiş gibi görünsün)
            var installedDir = Path.Combine(Path.GetDirectoryName(exchangeDir), "Installed");
            var installedFilePath = Path.Combine(installedDir, "xml-formatter-wpf.json");
            File.WriteAllText(installedFilePath, json);
        }
    }
} 