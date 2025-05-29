using System;
using System.IO;
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
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Contextualizer",
                SettingsFileName
            );
            LoadSettings();
        }

        public AppSettings Settings => _settings;

        public string HandlersFilePath => _settings.HandlersFilePath;
        public string PluginsDirectory => _settings.PluginsDirectory;
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
    }
} 