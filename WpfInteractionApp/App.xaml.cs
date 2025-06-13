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
        private HandlerExchangeService? _handlerExchangeService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize and apply the saved theme
                ThemeManager.Instance.ApplyTheme(WpfInteractionApp.Properties.Settings.Default.Theme ?? "Light");

                // Load the base styles
                Resources.MergedDictionaries.Add(new ResourceDictionary 
                { 
                    Source = new Uri("/WpfInteractionApp;component/Themes/CarbonStyles.xaml", UriKind.Relative) 
                });

                // Initialize settings service
                _settingsService = new SettingsService();
                ServiceLocator.Register<SettingsService>(_settingsService);

                // Initialize plugin directory manager
                var directoryManager = new PluginDirectoryManager(_settingsService);
                ServiceLocator.Register<PluginDirectoryManager>(directoryManager);

                // Initialize handler exchange service
                _handlerExchangeService = new HandlerExchangeService(_settingsService, directoryManager);
                ServiceLocator.Register<HandlerExchangeService>(_handlerExchangeService);

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