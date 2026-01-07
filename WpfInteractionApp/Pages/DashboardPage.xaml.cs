using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            Loaded += (_, _) => UpdateStats();
        }

        private void UpdateStats()
        {
            try
            {
                var handlerManager = ServiceLocator.SafeGet<HandlerManager>();
                HandlersCountText.Text = (handlerManager?.GetHandlerCount() ?? 0).ToString();

                var cron = ServiceLocator.SafeGet<ICronService>();
                CronCountText.Text = (cron?.GetActiveJobCount() ?? 0).ToString();
            }
            catch
            {
                // Best-effort UI; ignore.
            }
        }

        private void ManageHandlers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new HandlerManagementWindow
                {
                    Owner = Application.Current.MainWindow
                };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Handler management açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenCronManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new CronManagerWindow
                {
                    Owner = Application.Current.MainWindow
                };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cron manager açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenMarketplace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new HandlerExchangeWindow
                {
                    Owner = Application.Current.MainWindow
                };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Marketplace açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReloadHandlers_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.ReloadHandlers();
            UpdateStats();
        }

        private void ManualHandlers_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.ShowManualHandlersMenu(sender as FrameworkElement);
        }

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.NavigateTo(typeof(LogViewerPage));
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.NavigateTo(typeof(SettingsPage));
        }
    }
}


