using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfInteractionApp
{
    public partial class NativeConfirmationDialog : Window
    {
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        public NativeConfirmationDialog(string title, string message)
        {
            InitializeComponent();
            
            Title = title;
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            
            // Always center on screen and be topmost
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            
            // Activate window when shown
            Loaded += (s, e) =>
            {
                Activate();
                Focus();
                OkButton.Focus();
            };
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_tcs.Task.IsCompleted)
                _tcs.SetResult(true);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_tcs.Task.IsCompleted)
                _tcs.SetResult(false);
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            // Ensure result is set if window is closed via X or Alt+F4
            if (!_tcs.Task.IsCompleted)
                _tcs.SetResult(false);
        }

        public Task<bool> ShowDialogAsync()
        {
            Show(); // Use Show instead of ShowDialog for async
            return _tcs.Task;
        }

        public bool ShowDialogSync()
        {
            ShowDialog();
            return _tcs.Task.Result;
        }
    }
}

