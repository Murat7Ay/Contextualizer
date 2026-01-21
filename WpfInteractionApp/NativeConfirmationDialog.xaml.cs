using Contextualizer.PluginContracts;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class NativeConfirmationDialog : Window
    {
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        public NativeConfirmationDialog(string title, string message)
            : this(new ConfirmationRequest { Title = title, Message = message })
        {
        }

        public NativeConfirmationDialog(ConfirmationRequest request)
        {
            InitializeComponent();

            Title = request.Title;
            TitleBlock.Text = request.Title;
            MessageBlock.Text = request.Message;
            ApplyDetails(request.Details);
            
            // Always center on screen and be topmost
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
            
            // Activate window when shown
            Loaded += (s, e) =>
            {
                WindowActivationHelper.BringToFrontBestEffort(this);
                Activate();
                Focus();
                OkButton.Focus();
            };
        }

        private void ApplyDetails(ConfirmationDetails? details)
        {
            if (details == null)
            {
                DetailsExpander.Visibility = Visibility.Collapsed;
                return;
            }

            var format = (details.Format ?? "text").Trim().ToLowerInvariant();
            string text = details.Text ?? string.Empty;
            if (format == "json")
            {
                if (details.Json.HasValue)
                {
                    text = JsonSerializer.Serialize(details.Json.Value, new JsonSerializerOptions { WriteIndented = true });
                }
            }

            if (format == "markdown")
            {
                // Markdown deprecated for native confirm; render as plain text.
                format = "text";
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                DetailsExpander.Visibility = Visibility.Collapsed;
                return;
            }

            DetailsTextBox.Text = text;
            DetailsTextBox.Visibility = Visibility.Visible;
            DetailsExpander.Visibility = Visibility.Visible;
            DetailsExpander.IsExpanded = true;
        }

        // Table format removed; send markdown tables in details.text instead.

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

