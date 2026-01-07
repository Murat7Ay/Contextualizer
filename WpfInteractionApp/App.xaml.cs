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
        private McpServerHost? _mcpServerHost;

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

                // UI activity log (consumed by LogViewerPage + snackbar)
                var activityLogService = new ActivityLogService();
                ServiceLocator.Register<ActivityLogService>(activityLogService);

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

                // Start MCP server if enabled (localhost only)
                await StartMcpServerIfEnabledAsync(_settingsService, userInteractionService);

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

                    // Stop MCP server early to avoid serving requests during shutdown
                    try
                    {
                        if (_mcpServerHost != null)
                        {
                            var stopTask = _mcpServerHost.StopAsync();
                            if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                            {
                                _loggingService?.LogWarning("MCP server stop timed out");
                                System.Diagnostics.Debug.WriteLine("MCP server stop timed out");
                            }

                            _mcpServerHost = null;
                            _loggingService?.LogInfo("MCP server stopped");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error stopping MCP server", ex);
                        System.Diagnostics.Debug.WriteLine($"Error stopping MCP server: {ex.Message}");
                    }

                    // Dispose services in reverse order with timeout protection
                    try
                    {
                        _handlerManager?.Dispose();
                        _loggingService?.LogInfo("HandlerManager disposed");
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error disposing HandlerManager", ex);
                        System.Diagnostics.Debug.WriteLine($"Error disposing HandlerManager: {ex.Message}");
                    }

                    try
                    {
                        if (_cronScheduler != null)
                        {
                            var stopTask = _cronScheduler.Stop();
                            // Use timeout to prevent hanging
                            if (!stopTask.Wait(TimeSpan.FromSeconds(5)))
                            {
                                _loggingService?.LogWarning("CronScheduler stop timed out");
                                System.Diagnostics.Debug.WriteLine("CronScheduler stop timed out");
                            }
                            
                            _cronScheduler.Dispose();
                            _loggingService?.LogInfo("CronScheduler stopped and disposed");
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error disposing CronScheduler", ex);
                        System.Diagnostics.Debug.WriteLine($"Error disposing CronScheduler: {ex.Message}");
                    }

                    // Dispose MainWindow and its WebView2 controls
                    try
                    {
                        _mainWindow?.Close();
                        _loggingService?.LogInfo("MainWindow closed");
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.LogError("Error closing MainWindow", ex);
                        System.Diagnostics.Debug.WriteLine($"Error closing MainWindow: {ex.Message}");
                    }

                    // Force garbage collection to ensure all finalizers run
                    try
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        _loggingService?.LogInfo("Garbage collection completed");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error during GC: {ex.Message}");
                    }

                    // Wait for WebView2 browser processes to terminate gracefully
                    try
                    {
                        // First wait after MainWindow cleanup
                        System.Threading.Thread.Sleep(500);
                        
                        // Force one more GC cycle to ensure WebView2 finalizers run
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        
                        // Extended wait for all WebView2 sub-processes (GPU, Renderer, etc.)
                        System.Threading.Thread.Sleep(1000);
                        _loggingService?.LogInfo("Waited for WebView2 processes to terminate");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error waiting for WebView2: {ex.Message}");
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

        private async Task StartMcpServerIfEnabledAsync(SettingsService settingsService, IUserInteractionService userInteractionService)
        {
            try
            {
                if (settingsService.Settings.McpSettings == null || !settingsService.Settings.McpSettings.Enabled)
                    return;

                var port = settingsService.Settings.McpSettings.Port;
                if (port < 1 || port > 65535)
                {
                    userInteractionService.ShowActivityFeedback(LogType.Warning, $"MCP server not started: invalid port {port}");
                    return;
                }

                _mcpServerHost = new McpServerHost();
                await _mcpServerHost.StartAsync(port);

                userInteractionService.ShowActivityFeedback(LogType.Info, $"MCP server started: http://127.0.0.1:{port}/mcp/sse");
            }
            catch (Exception ex)
            {
                userInteractionService.ShowActivityFeedback(LogType.Error, $"Failed to start MCP server: {ex.Message}");
                _loggingService?.LogError("Failed to start MCP server", ex);
                _mcpServerHost = null;
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