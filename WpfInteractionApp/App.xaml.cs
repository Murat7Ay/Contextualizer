using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace WpfInteractionApp
{
    public partial class App : Application
    {
        private HandlerManager _handlerManager;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            var userInteractionService = new WpfUserInteractionService(mainWindow);
            mainWindow.Show();

            try
            {
                userInteractionService.Log(LogType.Info, "HandlerManager başlatılıyor...");
                _handlerManager = new HandlerManager(userInteractionService, @"C:\Finder\handlers.json");
                await _handlerManager.StartAsync();
            }
            catch (Exception ex)
            {
                userInteractionService.ShowNotification($"HandlerManager başlatılamadı: {ex.Message}", LogType.Error);
            }
        }

        protected override void OnLoadCompleted(NavigationEventArgs e)
        {
            base.OnLoadCompleted(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_handlerManager != null)
            {
                _handlerManager.Dispose();
            }
            base.OnExit(e);
        }
    }
}