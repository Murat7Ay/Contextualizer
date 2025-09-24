using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Gets a configuration value by key from either config.ini or secrets.ini
        /// </summary>
        /// <param name="key">Configuration key (e.g., "jira_api_key", "database_connection")</param>
        /// <returns>Configuration value or null if not found</returns>
        string GetValue(string key);

        /// <summary>
        /// Checks if a configuration key exists
        /// </summary>
        /// <param name="key">Configuration key to check</param>
        /// <returns>True if key exists, false otherwise</returns>
        bool HasKey(string key);

        /// <summary>
        /// Reloads configuration files from disk
        /// </summary>
        void ReloadConfig();

        /// <summary>
        /// Sets a configuration value and writes to appropriate file
        /// </summary>
        /// <param name="fileType">File type: "secrets" or "config"</param>
        /// <param name="section">Section name</param>
        /// <param name="key">Key name</param>
        /// <param name="value">Value to set</param>
        void SetValue(string fileType, string section, string key, string value);

        /// <summary>
        /// Gets whether the configuration system is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets configuration values from a specific section
        /// </summary>
        /// <param name="sectionName">Section name (e.g., "api_keys", "endpoints")</param>
        /// <returns>Dictionary of key-value pairs in the section</returns>
        Dictionary<string, string> GetSection(string sectionName);

        /// <summary>
        /// Gets all available configuration keys
        /// </summary>
        /// <returns>List of all configuration keys</returns>
        List<string> GetAllKeys();
    }

    /// <summary>
    /// Configuration system settings
    /// </summary>
    public class ConfigSystemSettings
    {
        public bool Enabled { get; set; } = true;
        public string ConfigFilePath { get; set; } = string.Empty;
        public string SecretsFilePath { get; set; } = string.Empty;
        public bool AutoCreateFiles { get; set; } = true;
        public string FileFormat { get; set; } = "ini"; // "ini" or "json"
    }
}
