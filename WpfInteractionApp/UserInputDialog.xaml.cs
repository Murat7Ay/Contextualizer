using Contextualizer.Core;
using System;
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
            
            // Set window title and message
            Title = request.Title;
            MessageText.Text = request.Message;

            // Set owner window
            if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Set initial focus to input box
            Loaded += (s, e) => InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputTextBox.Text?.Trim();
            if (_request.IsRequired && string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Input is required. Please enter a value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                InputTextBox.Focus();
                return;
            }

            if (!string.IsNullOrEmpty(_request.ValidationRegex))
            {
                var regex = new Regex(_request.ValidationRegex);
                if (!regex.IsMatch(input))
                {
                    MessageBox.Show("Invalid input format. Please follow the expected format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    InputTextBox.Focus();
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