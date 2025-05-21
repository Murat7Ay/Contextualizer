using Contextualizer.Core;
using System;
using System.Windows;
using WpfInteractionApp.Services;
using System.Linq;

namespace WpfInteractionApp
{
    public partial class App : Application
    {
        private HandlerManager? _handlerManager;
        private MainWindow? _mainWindow;
        private ThemeService? _themeService;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Clear only theme dictionaries, preserve converters
                var themeDicts = Resources.MergedDictionaries.ToList();
                foreach (var dict in themeDicts)
                {
                    Resources.MergedDictionaries.Remove(dict);
                }

                // First load the theme colors
                var initialTheme = WpfInteractionApp.Properties.Settings.Default.Theme;
                var themeType = Enum.Parse<ThemeType>(initialTheme, true);
                var themeUri = themeType == ThemeType.Dark
                    ? new Uri("/WpfInteractionApp;component/Themes/CarbonColors.xaml", UriKind.Relative)
                    : new Uri("/WpfInteractionApp;component/Themes/LightCarbonColors.xaml", UriKind.Relative);

                Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

                // Then load the styles that depend on the colors
                Resources.MergedDictionaries.Add(new ResourceDictionary 
                { 
                    Source = new Uri("/WpfInteractionApp;component/Themes/CarbonStyles.xaml", UriKind.Relative) 
                });

                // Initialize theme service
                _themeService = new ThemeService(this);
                ServiceLocator.Register(_themeService);

                // Initialize main window after resources are loaded
                _mainWindow = new MainWindow();
                MainWindow = _mainWindow;

                // Create WpfUserInteractionService
                var userInteractionService = new WpfUserInteractionService(_mainWindow);
                ServiceLocator.Register<IUserInteractionService>(userInteractionService);

                // Now create the HandlerManager
                _handlerManager = new HandlerManager(
                    userInteractionService,
                    @"C:\Finder\handlers.json" // TODO: Make this configurable
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