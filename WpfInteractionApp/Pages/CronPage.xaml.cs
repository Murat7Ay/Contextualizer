using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp.Pages
{
    public partial class CronPage : Page
    {
        public CronPage()
        {
            InitializeComponent();
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
    }
}


