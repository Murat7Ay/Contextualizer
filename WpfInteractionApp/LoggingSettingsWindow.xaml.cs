using Contextualizer.Core;
using Contextualizer.PluginContracts;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class LoggingSettingsWindow : Window, INotifyPropertyChanged
    {
        private LoggingConfiguration _config;
        private readonly ILoggingService _loggingService;

        public event PropertyChangedEventHandler? PropertyChanged;

        #region Properties

        public bool EnableLocalLogging
        {
            get => _config.EnableLocalLogging;
            set
            {
                _config.EnableLocalLogging = value;
                OnPropertyChanged(nameof(EnableLocalLogging));
            }
        }

        public bool EnableUsageTracking
        {
            get => _config.EnableUsageTracking;
            set
            {
                _config.EnableUsageTracking = value;
                OnPropertyChanged(nameof(EnableUsageTracking));
            }
        }

        public string LocalLogPath
        {
            get => _config.LocalLogPath;
            set
            {
                _config.LocalLogPath = value;
                OnPropertyChanged(nameof(LocalLogPath));
                RefreshStatistics();
            }
        }

        public string? UsageEndpointUrl
        {
            get => _config.UsageEndpointUrl;
            set
            {
                _config.UsageEndpointUrl = value;
                OnPropertyChanged(nameof(UsageEndpointUrl));
            }
        }

        public LogLevel MinimumLogLevel
        {
            get => _config.MinimumLogLevel;
            set
            {
                _config.MinimumLogLevel = value;
                OnPropertyChanged(nameof(MinimumLogLevel));
            }
        }

        public int MaxLogFileSizeMB
        {
            get => _config.MaxLogFileSizeMB;
            set
            {
                _config.MaxLogFileSizeMB = value;
                OnPropertyChanged(nameof(MaxLogFileSizeMB));
            }
        }

        public int MaxLogFileCount
        {
            get => _config.MaxLogFileCount;
            set
            {
                _config.MaxLogFileCount = value;
                OnPropertyChanged(nameof(MaxLogFileCount));
            }
        }

        public bool EnableDebugMode
        {
            get => _config.EnableDebugMode;
            set
            {
                _config.EnableDebugMode = value;
                OnPropertyChanged(nameof(EnableDebugMode));
            }
        }

        // Statistics Properties
        private string _currentLogDirectory = "";
        public string CurrentLogDirectory
        {
            get => _currentLogDirectory;
            set
            {
                _currentLogDirectory = value;
                OnPropertyChanged(nameof(CurrentLogDirectory));
            }
        }

        private string _totalLogFiles = "0";
        public string TotalLogFiles
        {
            get => _totalLogFiles;
            set
            {
                _totalLogFiles = value;
                OnPropertyChanged(nameof(TotalLogFiles));
            }
        }

        private string _totalLogSize = "0 MB";
        public string TotalLogSize
        {
            get => _totalLogSize;
            set
            {
                _totalLogSize = value;
                OnPropertyChanged(nameof(TotalLogSize));
            }
        }

        #endregion

        public LoggingSettingsWindow()
        {
            _loggingService = ServiceLocator.SafeGet<ILoggingService>();
            _config = _loggingService?.GetConfiguration() ?? new LoggingConfiguration();

            InitializeComponent();
            DataContext = this;
            
            InitializeComboBoxes();
            RefreshStatistics();
        }

        private void InitializeComboBoxes()
        {
            // SelectedValue binding will handle this automatically
            // No manual initialization needed
        }

        private void RefreshStatistics()
        {
            try
            {
                CurrentLogDirectory = _config.LocalLogPath;

                if (Directory.Exists(_config.LocalLogPath))
                {
                    var logFiles = Directory.GetFiles(_config.LocalLogPath, "*.log");
                    TotalLogFiles = logFiles.Length.ToString();

                    var totalSize = logFiles.Sum(file => new FileInfo(file).Length);
                    TotalLogSize = FormatFileSize(totalSize);
                }
                else
                {
                    TotalLogFiles = "0";
                    TotalLogSize = "0 MB";
                }
            }
            catch (Exception ex)
            {
                TotalLogFiles = "Error";
                TotalLogSize = "Error";
                _loggingService?.LogError("Failed to refresh log statistics", ex);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        #region Event Handlers

        private void BrowseLogDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    ValidateNames = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Select Folder",
                    Title = "Select Log Directory"
                };

                if (dialog.ShowDialog() == true)
                {
                    LocalLogPath = System.IO.Path.GetDirectoryName(dialog.FileName) ?? LocalLogPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService?.LogError("Failed to open folder browser", ex);
            }
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_config.LocalLogPath))
                {
                    Directory.CreateDirectory(_config.LocalLogPath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _config.LocalLogPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open log folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService?.LogError("Failed to open log folder", ex);
            }
        }

        private void ClearOldLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete all old log files? This action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (Directory.Exists(_config.LocalLogPath))
                    {
                        var logFiles = Directory.GetFiles(_config.LocalLogPath, "*.log");
                        var deletedCount = 0;

                        foreach (var file in logFiles)
                        {
                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                            }
                            catch
                            {
                                // Skip files that can't be deleted
                            }
                        }

                        MessageBox.Show($"Deleted {deletedCount} log files.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshStatistics();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService?.LogError("Failed to clear old logs", ex);
            }
        }

        private void RefreshStatistics_Click(object sender, RoutedEventArgs e)
        {
            RefreshStatistics();
        }

        private void TestErrorLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService?.LogError("Test error log message", new Exception("This is a test exception"));
                _loggingService?.LogWarning("Test warning log message");
                _loggingService?.LogInfo("Test info log message");
                _loggingService?.LogDebug("Test debug log message");

                MessageBox.Show("Test log messages have been written. Check the log files.", "Test Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestUsageAnalytics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_config.EnableUsageTracking || string.IsNullOrEmpty(_config.UsageEndpointUrl))
                {
                    MessageBox.Show("Usage tracking is disabled or endpoint URL is not set.", "Test Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _loggingService?.LogUserActivityAsync("test_activity", new System.Collections.Generic.Dictionary<string, object>
                {
                    ["test_parameter"] = "test_value",
                    ["timestamp"] = DateTime.UtcNow
                })!;

                MessageBox.Show("Test usage analytics data has been sent.", "Test Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error testing usage analytics: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService?.LogError("Failed to test usage analytics", ex);
            }
        }

        private void ExportConfiguration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"logging-config-{DateTime.Now:yyyy-MM-dd}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(_config, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show("Configuration exported successfully.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService?.LogError("Failed to export configuration", ex);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // SelectedValue binding will automatically update MinimumLogLevel property
                _loggingService?.SetConfiguration(_config);
                
                // Save to settings file as well
                try
                {
                    var settingsService = ServiceLocator.Get<SettingsService>();
                    settingsService.Settings.LoggingSettings.FromLoggingConfiguration(_config);
                    settingsService.SaveSettings();
                }
                catch (Exception settingsEx)
                {
                    _loggingService?.LogError("Failed to save logging settings to file", settingsEx);
                }
                
                MessageBox.Show("Configuration applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                _loggingService?.LogInfo("Logging configuration updated via settings window");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _loggingService?.LogError("Failed to apply logging configuration", ex);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyButton_Click(sender, e);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}