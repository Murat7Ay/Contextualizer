using Contextualizer.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using WpfInteractionApp.Services;

namespace WpfInteractionApp.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var settingsWindow = new SettingsWindow(settingsService.Settings)
                {
                    Owner = Application.Current.MainWindow
                };

                if (settingsWindow.ShowDialog() == true)
                {
                    settingsService.SaveSettings();
                    MessageBox.Show(
                        "Settings saved. Please restart the application for changes to take effect.",
                        "Settings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Settings açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenLoggingSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loggingSettingsWindow = new LoggingSettingsWindow
                {
                    Owner = Application.Current.MainWindow
                };
                loggingSettingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logging settings açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThemeLight_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme("Light");
        }

        private void ThemeDark_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme("Dark");
        }

        private void ThemeDim_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme("Dim");
        }
    }
}


