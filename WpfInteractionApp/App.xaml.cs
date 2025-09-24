using Contextualizer.Core;
using Contextualizer.Core.Services;
using System;
using System.Diagnostics;
using System.Windows;
using WpfInteractionApp.Services;
using System.Linq;
using Contextualizer.PluginContracts;
using System.IO;

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
    }
}