using Contextualizer.Core;
using Contextualizer.Core.Services;
using System;
using System.Windows;
using WpfInteractionApp.Services;
using System.Linq;
using Contextualizer.PluginContracts;
using System.IO;

namespace WpfInteractionApp
{
    //dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:PublishTrimmed=false -o "\\laptop-qtnej1vb\PortableApps\deploy"

    public partial class App : Application
    {
        private HandlerManager? _handlerManager;
        private MainWindow? _mainWindow;
        private SettingsService? _settingsService;
        private CronScheduler? _cronScheduler;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize settings service first
                _settingsService = new SettingsService();
                ServiceLocator.Register<SettingsService>(_settingsService);

                // Initialize and register logging service using settings
                var loggingService = new LoggingService();
                var loggingConfig = _settingsService.Settings.LoggingSettings.ToLoggingConfiguration();
                loggingService.SetConfiguration(loggingConfig);
                ServiceLocator.Register<ILoggingService>(loggingService);

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

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during startup: {ex.Message}\n\nDetails: {ex}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Save settings before exit
            try
            {
                _settingsService?.SaveSettings();
            }
            catch (Exception ex)
            {
                // Log error but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Error saving settings on exit: {ex.Message}");
            }

            _handlerManager?.Dispose();
            _cronScheduler?.Stop().Wait();
            _cronScheduler?.Dispose();
            base.OnExit(e);
        }
    }
}