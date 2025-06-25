using Contextualizer.Core;
using System;
using System.Windows;
using WpfInteractionApp.Services;
using System.Linq;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp
{
    public partial class App : Application
    {
        private HandlerManager? _handlerManager;
        private MainWindow? _mainWindow;
        private SettingsService? _settingsService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize settings service first
                _settingsService = new SettingsService();
                ServiceLocator.Register<SettingsService>(_settingsService);

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
            _handlerManager?.Dispose();
            base.OnExit(e);
        }
    }
}