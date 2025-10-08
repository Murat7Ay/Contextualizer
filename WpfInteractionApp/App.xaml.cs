using Contextualizer.Core;
using Contextualizer.Core.Services;
using System;
using System.Diagnostics;
using System.Windows;
using WpfInteractionApp.Services;
using System.Linq;
using Contextualizer.PluginContracts;
using System.IO;
using System.Threading.Tasks;

namespace WpfInteractionApp
{
    public partial class App : Application
    {
        private HandlerManager? _handlerManager;
        private MainWindow? _mainWindow;
        private SettingsService? _settingsService;
        private CronScheduler? _cronScheduler;
        private ILoggingService? _loggingService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize settings service first
                _settingsService = new SettingsService();
                ServiceLocator.Register<SettingsService>(_settingsService);

                // Initialize configuration service
                var configSystemSettings = new Contextualizer.PluginContracts.ConfigSystemSettings
                {
                    Enabled = _settingsService.Settings.ConfigSystem.Enabled,
                    ConfigFilePath = _settingsService.Settings.ConfigSystem.ConfigFilePath,
                    SecretsFilePath = _settingsService.Settings.ConfigSystem.SecretsFilePath,
                    AutoCreateFiles = _settingsService.Settings.ConfigSystem.AutoCreateFiles,
                    FileFormat = _settingsService.Settings.ConfigSystem.FileFormat
                };
                var configService = new Contextualizer.Core.Services.ConfigurationService(configSystemSettings);
                ServiceLocator.Register<IConfigurationService>(configService);

                // Initialize and register logging service using settings
                _loggingService = new LoggingService();
                var loggingConfig = _settingsService.Settings.LoggingSettings.ToLoggingConfiguration();
                _loggingService.SetConfiguration(loggingConfig);
                ServiceLocator.Register<ILoggingService>(_loggingService);

                // Log application startup
                using (_loggingService.BeginScope("ApplicationStartup", new Dictionary<string, object>
                {
                    ["startup_time"] = DateTime.UtcNow,
                    ["version"] = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
                    ["args"] = e.Args
                }))
                {
                    _loggingService.LogInfo("Application startup initiated");
                }

                // Initialize and apply the saved theme from AppSettings
                ThemeManager.Instance.ApplyTheme(_settingsService.Settings.UISettings.Theme);

                // Load the base styles
                Resources.MergedDictionaries.Add(new ResourceDictionary 
                { 
                    Source = new Uri("/WpfInteractionApp;component/Themes/CarbonStyles.xaml", UriKind.Relative) 
                });


                // Initialize main window
                _mainWindow = new MainWindow();
                MainWindow = _mainWindow;

                // Create WpfUserInteractionService
                var userInteractionService = new WpfUserInteractionService(_mainWindow);
                ServiceLocator.Register<IUserInteractionService>(userInteractionService);

                // Initialize network update service
                var networkUpdateService = new Services.NetworkUpdateService(_settingsService, _settingsService.Settings.UISettings.NetworkUpdateSettings.NetworkUpdatePath);
                ServiceLocator.Register<Services.NetworkUpdateService>(networkUpdateService);
                
                // Check for network updates after UI is loaded (async, non-blocking)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000); // Wait 3 seconds after startup
                    await CheckForNetworkUpdatesAsync(networkUpdateService, _settingsService);
                });

                // Initialize and register CronScheduler
                _cronScheduler = new CronScheduler();

                // Start the CronScheduler
                await _cronScheduler.Start();

                ServiceLocator.Register<ICronService>(_cronScheduler);

                // Create the HandlerManager
                _handlerManager = new HandlerManager(
                    userInteractionService,
                    _settingsService
                );

                // Initialize HandlerManager in MainWindow
                _mainWindow.InitializeHandlerManager(_handlerManager);
                
                // Show the window after everything is initialized
                _mainWindow.Show();

                // Start the HandlerManager
                await _handlerManager.StartAsync();

                _loggingService.LogInfo("Application startup completed successfully");
            }
            catch (Exception ex)
            {
                // Try to log the error if logging service is available
                try
                {
                    _loggingService?.LogError("Critical error during application startup", ex, new Dictionary<string, object>
                    {
                        ["startup_phase"] = "initialization",
                        ["fatal"] = true
                    });
                }
                catch
                {
                    // If logging fails, continue with showing the message box
                }

                MessageBox.Show($"Error during startup: {ex.Message}\n\nDetails: {ex}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Log application shutdown
                using (_loggingService?.BeginScope("ApplicationShutdown", new Dictionary<string, object>
                {
                    ["shutdown_time"] = DateTime.UtcNow,
                    ["exit_code"] = e.ApplicationExitCode,
                    ["session_duration"] = DateTime.UtcNow.Subtract(Process.GetCurrentProcess().StartTime).TotalMinutes
                }))
                {
                    _loggingService?.LogInfo("Application shutdown initiated");

                    // Save settings before exit
                    try
                    {
                        _settingsService?.SaveSettings();
                        _loggingService?.LogInfo("Settings saved successfully");
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error saving settings on exit", ex);
                        System.Diagnostics.Debug.WriteLine($"Error saving settings on exit: {ex.Message}");
                    }

                    // Dispose services in reverse order
                    try
                    {
                        _handlerManager?.Dispose();
                        _loggingService?.LogInfo("HandlerManager disposed");
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error disposing HandlerManager", ex);
                    }

                    try
                    {
                        _cronScheduler?.Stop().Wait();
                        _cronScheduler?.Dispose();
                        _loggingService?.LogInfo("CronScheduler stopped and disposed");
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error disposing CronScheduler", ex);
                    }

                    _loggingService?.LogInfo("Application shutdown completed");
                }
            }
            finally
            {
                // Dispose logging service last to capture all shutdown logs
                try
                {
                    _loggingService?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing LoggingService: {ex.Message}");
                }

                base.OnExit(e);
            }
        }

        private async Task CheckForNetworkUpdatesAsync(Services.NetworkUpdateService networkUpdateService, SettingsService settingsService)
        {
            try
            {
                var networkSettings = settingsService.Settings.UISettings.NetworkUpdateSettings;
                
                // Only check if network updates are enabled
                if (!networkSettings.EnableNetworkUpdates)
                {
                    Debug.WriteLine("Network updates are disabled");
                    return;
                }
                
                var networkUpdateInfo = await networkUpdateService.CheckForUpdatesAsync();
                
                if (networkUpdateInfo?.IsUpdateAvailable == true)
                {
                    await ShowNetworkUpdateNotificationAsync(networkUpdateInfo, networkUpdateService, settingsService);
                }
                else if (!string.IsNullOrEmpty(networkUpdateInfo?.ErrorMessage))
                {
                    Debug.WriteLine($"Network update check failed: {networkUpdateInfo.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                // Silent fail for update check - don't bother user
                Debug.WriteLine($"Network update check failed: {ex.Message}");
            }
        }

        private async Task ShowNetworkUpdateNotificationAsync(Services.NetworkUpdateInfo updateInfo, 
            Services.NetworkUpdateService updateService, SettingsService settingsService)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // Check if user has skipped this version before (only for non-mandatory)
                if (!updateInfo.IsMandatory)
                {
                    var skippedVersion = GetSkippedVersion();
                    if (skippedVersion == updateInfo.LatestVersion)
                        return; // User chose to skip this version
                }

                var updateWindow = new NetworkUpdateWindow(updateInfo, updateService)
                {
                    Owner = _mainWindow
                };

                var result = updateWindow.ShowDialog();
                
                if (updateWindow.Result == NetworkUpdateResult.RemindLater && !updateInfo.IsMandatory)
                {
                    // Schedule reminder for next startup
                    SaveLastUpdateCheck(DateTime.Now);
                }
            });
        }


        private string? GetSkippedVersion()
        {
            try
            {
                var settingsService = ServiceLocator.SafeGet<SettingsService>();
                return settingsService?.Settings.UISettings.SkippedUpdateVersion;
            }
            catch
            {
                return null;
            }
        }

        private void SaveSkippedVersion(string version)
        {
            try
            {
                var settingsService = ServiceLocator.SafeGet<SettingsService>();
                if (settingsService != null)
                {
                    settingsService.Settings.UISettings.SkippedUpdateVersion = version;
                    settingsService.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save skipped version: {ex.Message}");
            }
        }

        private void SaveLastUpdateCheck(DateTime checkTime)
        {
            try
            {
                var settingsService = ServiceLocator.SafeGet<SettingsService>();
                if (settingsService != null)
                {
                    settingsService.Settings.UISettings.LastUpdateCheck = checkTime;
                    settingsService.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save last update check: {ex.Message}");
            }
        }
    }
}