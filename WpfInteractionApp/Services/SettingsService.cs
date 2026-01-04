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
            
            // Load settings first (creates default if not exists)
            LoadSettings();
            
            // Ensure portable directories exist
            CreatePortableDirectories();
            
            // Perform initial deployment from network path (first run only)
            PerformInitialDeployment();
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
                    var json = File.ReadAllText(_settingsPath, System.Text.Encoding.UTF8);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    
                    // Ensure nested objects are initialized (for backward compatibility with old config files)
                    _settings.UISettings ??= new UISettings();
                    _settings.UISettings.InitialDeploymentSettings ??= new InitialDeploymentSettings();
                    _settings.UISettings.NetworkUpdateSettings ??= new NetworkUpdateSettings();
                    _settings.WindowSettings ??= new WindowSettings();
                    _settings.LoggingSettings ??= new LoggingSettings();
                    _settings.ConfigSystem ??= new ConfigSystemSettings();
                    _settings.KeyboardShortcut ??= new KeyboardShortcut();
                    _settings.McpSettings ??= new McpSettings();
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
                // Ensure nested objects are initialized even in error case
                _settings.UISettings ??= new UISettings();
                _settings.UISettings.InitialDeploymentSettings ??= new InitialDeploymentSettings();
                _settings.UISettings.NetworkUpdateSettings ??= new NetworkUpdateSettings();
                _settings.McpSettings ??= new McpSettings();
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

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json, System.Text.Encoding.UTF8);
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
                // Only create samples if initial deployment is not enabled or completed
                var exchangeDir = Path.Combine(baseDir, "Data", "Exchange");
                var deploymentSettings = _settings?.UISettings?.InitialDeploymentSettings;
                
                if (!Directory.GetFiles(exchangeDir, "*.json").Any() && 
                    (deploymentSettings == null || !deploymentSettings.Enabled || deploymentSettings.IsCompleted))
                {
                    CreateSampleExchangeHandler(exchangeDir);
                    CreateJsonFormatterExchangeHandler(exchangeDir);
                    CreateXmlFormatterExchangeHandler(exchangeDir);
                    
                    // Add sample handlers to handlers.json
                    var installedDir = Path.Combine(baseDir, "Data", "Installed");
                    AddInstalledHandlersToConfig(installedDir, handlersPath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Failed to create portable directory structure: {ex.Message}");
            }
        }

        private void PerformInitialDeployment()
        {
            var deploymentSettings = _settings.UISettings.InitialDeploymentSettings;
            
            // Skip if disabled, already completed, or source path is empty
            if (!deploymentSettings.Enabled || deploymentSettings.IsCompleted || string.IsNullOrWhiteSpace(deploymentSettings.SourcePath))
            {
                return;
            }

            try
            {
                const string baseDir = @"C:\PortableApps\Contextualizer";
                var sourcePath = deploymentSettings.SourcePath;

                // Check if source path exists
                if (!Directory.Exists(sourcePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Initial deployment source path not found: {sourcePath}");
                    System.Diagnostics.Debug.WriteLine($"Skipping initial deployment. Files will not be copied from network.");
                    // Mark as completed so it doesn't try again (network path might be temporarily unavailable)
                    deploymentSettings.IsCompleted = true;
                    SaveSettings();
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Starting initial deployment from: {sourcePath}");
                int filesCopied = 0;

                // Copy Exchange handlers
                if (deploymentSettings.CopyExchangeHandlers)
                {
                    var sourceExchangeDir = Path.Combine(sourcePath, "Exchange");
                    var targetExchangeDir = Path.Combine(baseDir, "Data", "Exchange");
                    filesCopied += CopyDirectory(sourceExchangeDir, targetExchangeDir, "*.json");
                }

                // Copy Installed handlers
                if (deploymentSettings.CopyInstalledHandlers)
                {
                    var sourceInstalledDir = Path.Combine(sourcePath, "Installed");
                    var targetInstalledDir = Path.Combine(baseDir, "Data", "Installed");
                    filesCopied += CopyDirectory(sourceInstalledDir, targetInstalledDir, "*.json");
                    
                    // Add installed handlers to handlers.json
                    AddInstalledHandlersToConfig(targetInstalledDir, Path.Combine(baseDir, "Config", "handlers.json"));
                }

                // Copy Plugins
                if (deploymentSettings.CopyPlugins)
                {
                    var sourcePluginsDir = Path.Combine(sourcePath, "Plugins");
                    var targetPluginsDir = Path.Combine(baseDir, "Plugins");
                    filesCopied += CopyDirectory(sourcePluginsDir, targetPluginsDir, "*.*");
                }

                // Mark deployment as completed
                deploymentSettings.IsCompleted = true;
                SaveSettings();

                System.Diagnostics.Debug.WriteLine($"Initial deployment completed. {filesCopied} files copied.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial deployment failed: {ex.Message}");
                // Don't crash the app, just log the error
            }
        }

        private int CopyDirectory(string sourceDir, string targetDir, string searchPattern)
        {
            int filesCopied = 0;

            try
            {
                if (!Directory.Exists(sourceDir))
                {
                    System.Diagnostics.Debug.WriteLine($"Source directory not found: {sourceDir}");
                    return 0;
                }

                // Ensure target directory exists
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Copy all matching files
                var files = Directory.GetFiles(sourceDir, searchPattern);
                foreach (var sourceFile in files)
                {
                    var fileName = Path.GetFileName(sourceFile);
                    var targetFile = Path.Combine(targetDir, fileName);

                    // Only copy if file doesn't exist in target
                    if (!File.Exists(targetFile))
                    {
                        File.Copy(sourceFile, targetFile, overwrite: false);
                        filesCopied++;
                        System.Diagnostics.Debug.WriteLine($"Copied: {fileName} -> {targetDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error copying from {sourceDir}: {ex.Message}");
            }

            return filesCopied;
        }

        private void AddInstalledHandlersToConfig(string installedDir, string handlersJsonPath)
        {
            try
            {
                if (!Directory.Exists(installedDir))
                {
                    System.Diagnostics.Debug.WriteLine($"Installed directory not found: {installedDir}");
                    return;
                }

                // Read current handlers.json
                if (!File.Exists(handlersJsonPath))
                {
                    System.Diagnostics.Debug.WriteLine($"handlers.json not found: {handlersJsonPath}");
                    return;
                }

                var handlersJson = File.ReadAllText(handlersJsonPath, System.Text.Encoding.UTF8);
                var handlersDoc = JsonSerializer.Deserialize<JsonDocument>(handlersJson);
                if (handlersDoc == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to parse handlers.json");
                    return;
                }

                // Get existing handlers array
                var existingHandlers = new List<JsonElement>();
                if (handlersDoc.RootElement.TryGetProperty("handlers", out var handlersArray))
                {
                    existingHandlers.AddRange(handlersArray.EnumerateArray());
                }

                // Get existing handler names for duplicate check
                var existingHandlerNames = existingHandlers
                    .Where(h => h.TryGetProperty("name", out _))
                    .Select(h => h.GetProperty("name").GetString()?.ToLowerInvariant())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToHashSet();

                int handlersAdded = 0;
                var newHandlers = new List<object>();

                // Read all installed handler files
                var installedFiles = Directory.GetFiles(installedDir, "*.json");
                foreach (var installedFile in installedFiles)
                {
                    try
                    {
                        var installedJson = File.ReadAllText(installedFile, System.Text.Encoding.UTF8);
                        var installedDoc = JsonSerializer.Deserialize<JsonDocument>(installedJson);

                        if (installedDoc != null && installedDoc.RootElement.TryGetProperty("handlerJson", out var handlerJson))
                        {
                            // Check if handler already exists (by name)
                            if (handlerJson.TryGetProperty("name", out var nameElement))
                            {
                                var handlerName = nameElement.GetString()?.ToLowerInvariant();
                                if (!string.IsNullOrEmpty(handlerName) && !existingHandlerNames.Contains(handlerName))
                                {
                                    // Convert JsonElement to object for serialization
                                    var handlerObject = JsonSerializer.Deserialize<object>(handlerJson.GetRawText());
                                    if (handlerObject != null)
                                    {
                                        newHandlers.Add(handlerObject);
                                        existingHandlerNames.Add(handlerName);
                                        handlersAdded++;
                                        System.Diagnostics.Debug.WriteLine($"Added handler to config: {nameElement.GetString()}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing installed handler {Path.GetFileName(installedFile)}: {ex.Message}");
                    }
                }

                // If we have new handlers to add, update handlers.json
                if (handlersAdded > 0)
                {
                    // Combine existing and new handlers
                    var allHandlers = existingHandlers
                        .Select(h => JsonSerializer.Deserialize<object>(h.GetRawText()))
                        .Concat(newHandlers)
                        .ToList();

                    var updatedConfig = new { handlers = allHandlers };

                    var options = new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };

                    var updatedJson = JsonSerializer.Serialize(updatedConfig, options);
                    File.WriteAllText(handlersJsonPath, updatedJson, System.Text.Encoding.UTF8);

                    System.Diagnostics.Debug.WriteLine($"Updated handlers.json: {handlersAdded} new handlers added");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No new handlers to add to handlers.json");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding installed handlers to config: {ex.Message}");
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
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(defaultHandlers, options);
            File.WriteAllText(handlersPath, json, System.Text.Encoding.UTF8);
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
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(sampleHandler, options);
            var samplePath = Path.Combine(exchangeDir, "sample-regex-handler.json");
            File.WriteAllText(samplePath, json, System.Text.Encoding.UTF8);
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
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(jsonFormatterHandler, options);
            
            // Exchange klasörüne yaz
            var exchangeFilePath = Path.Combine(exchangeDir, "json-formatter-wpf.json");
            File.WriteAllText(exchangeFilePath, json, System.Text.Encoding.UTF8);
            
            // Installed klasörüne de yaz (yüklenmiş gibi görünsün)
            var installedDir = Path.Combine(Path.GetDirectoryName(exchangeDir), "Installed");
            var installedFilePath = Path.Combine(installedDir, "json-formatter-wpf.json");
            File.WriteAllText(installedFilePath, json, System.Text.Encoding.UTF8);
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
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(xmlFormatterHandler, options);
            
            // Exchange klasörüne yaz
            var exchangeFilePath = Path.Combine(exchangeDir, "xml-formatter-wpf.json");
            File.WriteAllText(exchangeFilePath, json, System.Text.Encoding.UTF8);
            
            // Installed klasörüne de yaz (yüklenmiş gibi görünsün)
            var installedDir = Path.Combine(Path.GetDirectoryName(exchangeDir), "Installed");
            var installedFilePath = Path.Combine(installedDir, "xml-formatter-wpf.json");
            File.WriteAllText(installedFilePath, json, System.Text.Encoding.UTF8);
        }
    }
} 