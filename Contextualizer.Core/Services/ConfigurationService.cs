using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Contextualizer.Core.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly ConfigSystemSettings _settings;
        private readonly Dictionary<string, string> _configValues;
        private readonly Dictionary<string, string> _secretValues;
        private DateTime _configLastModified;
        private DateTime _secretsLastModified;

        public ConfigurationService(ConfigSystemSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _configValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _secretValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (_settings.Enabled)
            {
                Initialize();
            }
        }

        public bool IsEnabled => _settings.Enabled;

        public string GetValue(string key)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(key))
                return null;

            CheckForFileChanges();

            // Keys are stored as "section.key" format from INI files
            // First check secrets, then config (secrets have priority)
            if (_secretValues.TryGetValue(key, out var secretValue))
                return secretValue;

            if (_configValues.TryGetValue(key, out var configValue))
                return configValue;

            return null;
        }

        public bool HasKey(string key)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(key))
                return false;

            CheckForFileChanges();
            return _secretValues.ContainsKey(key) || _configValues.ContainsKey(key);
        }

        public void ReloadConfig()
        {
            if (!IsEnabled) return;

            _configValues.Clear();
            _secretValues.Clear();
            LoadConfigFiles();
        }

        public void SetValue(string fileType, string section, string key, string value)
        {
            if (!IsEnabled || string.IsNullOrEmpty(fileType) || string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key))
                return;

            var filePath = fileType.ToLower() == "secrets" ? _settings.SecretsFilePath : _settings.ConfigFilePath;
            var fullKey = $"{section}.{key}";
            
            // Update in-memory values
            var targetDictionary = fileType.ToLower() == "secrets" ? _secretValues : _configValues;
            targetDictionary[fullKey] = value;
            
            // Write to file
            WriteToIniFile(filePath, section, key, value);
        }

        public Dictionary<string, string> GetSection(string sectionName)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(sectionName))
                return new Dictionary<string, string>();

            CheckForFileChanges();

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var sectionPrefix = $"{sectionName}.";

            // Combine both dictionaries, with secrets taking priority
            foreach (var kvp in _configValues.Where(x => x.Key.StartsWith(sectionPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                var keyWithoutSection = kvp.Key.Substring(sectionPrefix.Length);
                result[keyWithoutSection] = kvp.Value;
            }

            foreach (var kvp in _secretValues.Where(x => x.Key.StartsWith(sectionPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                var keyWithoutSection = kvp.Key.Substring(sectionPrefix.Length);
                result[keyWithoutSection] = kvp.Value; // Overwrite config with secret
            }

            return result;
        }

        public List<string> GetAllKeys()
        {
            if (!IsEnabled) return new List<string>();

            CheckForFileChanges();
            return _configValues.Keys.Concat(_secretValues.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void Initialize()
        {
            try
            {
                CreateDirectoriesIfNeeded();
                
                if (_settings.AutoCreateFiles)
                {
                    CreateDefaultConfigFiles();
                }

                LoadConfigFiles();
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Configuration service initialization failed: {ex.Message}");
            }
        }

        private void CreateDirectoriesIfNeeded()
        {
            if (!string.IsNullOrEmpty(_settings.ConfigFilePath))
            {
                var configDir = Path.GetDirectoryName(_settings.ConfigFilePath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
            }

            if (!string.IsNullOrEmpty(_settings.SecretsFilePath))
            {
                var secretsDir = Path.GetDirectoryName(_settings.SecretsFilePath);
                if (!string.IsNullOrEmpty(secretsDir) && !Directory.Exists(secretsDir))
                {
                    Directory.CreateDirectory(secretsDir);
                }
            }
        }

        private void CreateDefaultConfigFiles()
        {
            // Create config.ini if it doesn't exist
            if (!string.IsNullOrEmpty(_settings.ConfigFilePath) && !File.Exists(_settings.ConfigFilePath))
            {
                var defaultConfig = @"; Contextualizer Configuration File
; This file contains non-sensitive configuration values

[endpoints]
; jira_base_url=https://company.atlassian.net
; github_api_url=https://api.github.com
; database_server=localhost\SQLEXPRESS

[settings]
timeout=30000
retry_count=3
log_level=Info

[paths]
; temp_directory=C:\Temp\Contextualizer
; output_directory=C:\Finder\output

; Example usage in handlers:
; ""url"": ""$config:endpoints.jira_base_url/rest/api/2/issue/$(issue_key)""
; ""connectionString"": ""$config:database.connection_string""
";

                File.WriteAllText(_settings.ConfigFilePath, defaultConfig);
            }

            // Create secrets.ini if it doesn't exist
            if (!string.IsNullOrEmpty(_settings.SecretsFilePath) && !File.Exists(_settings.SecretsFilePath))
            {
                var defaultSecrets = @"; Contextualizer Secrets File
; This file contains sensitive information and should NOT be committed to git
; Add this file to .gitignore

[api_keys]
; jira_api_key=your_jira_api_key_here
; github_token=ghp_your_github_token_here
; openai_api_key=sk-your_openai_key_here

[credentials]
; database_password=your_database_password
; smtp_password=your_email_password
; ftp_password=your_ftp_password

[connections]
; database_connection=Server=localhost\SQLEXPRESS;Database=YourDB;User Id=user;Password=pass;TrustServerCertificate=True;

; Example usage in handlers:
; ""headers"": { ""Authorization"": ""Bearer $config:api_keys.jira_api_key"" }
; ""connectionString"": ""$config:connections.database_connection""
";

                File.WriteAllText(_settings.SecretsFilePath, defaultSecrets);
            }
        }

        private void LoadConfigFiles()
        {
            // Load config file
            if (!string.IsNullOrEmpty(_settings.ConfigFilePath) && File.Exists(_settings.ConfigFilePath))
            {
                LoadIniFile(_settings.ConfigFilePath, _configValues);
                _configLastModified = File.GetLastWriteTime(_settings.ConfigFilePath);
            }

            // Load secrets file
            if (!string.IsNullOrEmpty(_settings.SecretsFilePath) && File.Exists(_settings.SecretsFilePath))
            {
                LoadIniFile(_settings.SecretsFilePath, _secretValues);
                _secretsLastModified = File.GetLastWriteTime(_settings.SecretsFilePath);
            }
        }

        private void LoadIniFile(string filePath, Dictionary<string, string> targetDictionary)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                string currentSection = "";

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    // Section header
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        continue;
                    }

                    // Key-value pair
                    var equalIndex = trimmedLine.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        var key = trimmedLine.Substring(0, equalIndex).Trim();
                        var value = trimmedLine.Substring(equalIndex + 1).Trim();

                        // Create full key with section prefix
                        var fullKey = string.IsNullOrEmpty(currentSection) ? key : $"{currentSection}.{key}";
                        targetDictionary[fullKey] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error loading config file '{filePath}': {ex.Message}");
            }
        }

        private void WriteToIniFile(string filePath, string section, string key, string value)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                
                var lines = File.Exists(filePath) ? File.ReadAllLines(filePath).ToList() : new List<string>();
                
                int sectionIndex = -1;
                int keyIndex = -1;
                
                // Find section
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i].Trim();
                    if (line == $"[{section}]")
                    {
                        sectionIndex = i;
                        
                        // Find key in this section
                        for (int j = i + 1; j < lines.Count; j++)
                        {
                            var keyLine = lines[j].Trim();
                            if (keyLine.StartsWith("[")) break; // Next section
                            if (keyLine.StartsWith($"{key}="))
                            {
                                keyIndex = j;
                                break;
                            }
                        }
                        break;
                    }
                }
                
                // Update or add
                if (keyIndex >= 0)
                {
                    lines[keyIndex] = $"{key}={value}";
                }
                else if (sectionIndex >= 0)
                {
                    lines.Insert(sectionIndex + 1, $"{key}={value}");
                }
                else
                {
                    if (lines.Count > 0) lines.Add("");
                    lines.Add($"[{section}]");
                    lines.Add($"{key}={value}");
                }
                
                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Config dosyası yazılamadı '{filePath}': {ex.Message}");
            }
        }

        private void CheckForFileChanges()
        {
            bool needsReload = false;

            // Check config file
            if (!string.IsNullOrEmpty(_settings.ConfigFilePath) && File.Exists(_settings.ConfigFilePath))
            {
                var lastModified = File.GetLastWriteTime(_settings.ConfigFilePath);
                if (lastModified != _configLastModified)
                {
                    needsReload = true;
                }
            }

            // Check secrets file
            if (!string.IsNullOrEmpty(_settings.SecretsFilePath) && File.Exists(_settings.SecretsFilePath))
            {
                var lastModified = File.GetLastWriteTime(_settings.SecretsFilePath);
                if (lastModified != _secretsLastModified)
                {
                    needsReload = true;
                }
            }

            if (needsReload)
            {
                ReloadConfig();
            }
        }
    }
}
