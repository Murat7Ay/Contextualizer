using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp.Pages
{
    public partial class MarketplacePage : Page
    {
        public MarketplacePage()
        {
            InitializeComponent();
        }

        private void OpenExchange_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show($"Exchange açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}


