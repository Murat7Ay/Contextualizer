using Contextualizer.Core;
using System.Text.RegularExpressions;
using System.Windows;

namespace WpfInteractionApp
{
    public partial class UserInputDialog : Window
    {
        private readonly UserInputRequest _request;
        public string UserInput { get; private set; }

        public UserInputDialog(UserInputRequest request)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));
            Title = request.Title;
            MessageText.Text = request.Message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputTextBox.Text?.Trim();
            if (_request.IsRequired && string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Giriş zorunludur. Lütfen bir değer girin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(_request.ValidationRegex))
            {
                var regex = new Regex(_request.ValidationRegex);
                if (!regex.IsMatch(input))
                {
                    MessageBox.Show("Geçersiz giriş formatı. Lütfen beklenen formatı takip edin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            UserInput = input;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

}