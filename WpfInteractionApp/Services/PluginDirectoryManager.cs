using System;
using System.IO;
using WpfInteractionApp.Settings;

namespace WpfInteractionApp.Services
{
    public class PluginDirectoryManager
    {
        private readonly ISettingsService _settingsService;
        
        public PluginDirectoryManager(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            InitializeDirectoryStructure();
        }

        private void InitializeDirectoryStructure()
        {
            var baseDir = _settingsService.ExchangeDirectory;
            var pluginSettings = _settingsService.Settings.PluginSettings;

            var directories = new[]
            {
                Path.Combine(baseDir, pluginSettings.InstalledDirectory),
                Path.Combine(baseDir, pluginSettings.CacheDirectory),
                Path.Combine(baseDir, pluginSettings.TempDirectory)
            };

            foreach (var dir in directories)
            {
                Directory.CreateDirectory(dir);
            }
        }

        public string GetPluginDirectory(string pluginName, string version)
        {
            return Path.Combine(
                _settingsService.ExchangeDirectory,
                _settingsService.Settings.PluginSettings.InstalledDirectory,
                pluginName,
                version
            );
        }

        public string GetPluginBinDirectory(string pluginName, string version)
        {
            return Path.Combine(GetPluginDirectory(pluginName, version), "bin");
        }

        public string GetPluginDocsDirectory(string pluginName, string version)
        {
            return Path.Combine(GetPluginDirectory(pluginName, version), "docs");
        }

        public string GetCacheDirectory()
        {
            return Path.Combine(
                _settingsService.ExchangeDirectory,
                _settingsService.Settings.PluginSettings.CacheDirectory
            );
        }

        public string GetTempDirectory()
        {
            return Path.Combine(
                _settingsService.ExchangeDirectory,
                _settingsService.Settings.PluginSettings.TempDirectory
            );
        }
    }
} 