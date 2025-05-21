using System.Threading.Tasks;
using System.Windows;

namespace WpfInteractionApp
{
    public partial class ConfirmationDialog : Window
    {
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        public string Title { get; }
        public string Message { get; }

        public ConfirmationDialog(string title, string message)
        {
            InitializeComponent();
            
            // Set window title and text blocks
            Title = title;
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            
            Owner = Application.Current.MainWindow;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(true);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(false);
            Close();
        }

        public Task<bool> ShowDialogAsync()
        {
            ShowDialog();
            return _tcs.Task;
        }
    }
}