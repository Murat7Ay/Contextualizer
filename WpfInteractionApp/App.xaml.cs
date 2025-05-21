using Contextualizer.Core;
using System;
using System.Windows;

namespace WpfInteractionApp
{
    public partial class App : Application
    {
        private HandlerManager? _handlerManager;
        private MainWindow? _mainWindow;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                _mainWindow = new MainWindow(null!); // Temporarily pass null
                MainWindow = _mainWindow; // Set as Application.Current.MainWindow
                _mainWindow.Show();

                // Now create the HandlerManager with proper MainWindow reference
                _handlerManager = new HandlerManager(
                    new WpfUserInteractionService(_mainWindow),
                    @"C:\Finder\handlers.json" // TODO: Make this configurable
                );

                // Update MainWindow's HandlerManager reference
                _mainWindow.InitializeHandlerManager(_handlerManager);

                // Start the HandlerManager
                await _handlerManager.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize the application: {ex.Message}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
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