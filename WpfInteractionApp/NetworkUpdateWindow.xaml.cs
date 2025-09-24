using System;
using System.Threading.Tasks;
using System.Windows;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class NetworkUpdateWindow : Window
    {
        private readonly NetworkUpdateInfo _updateInfo;
        private readonly NetworkUpdateService _updateService;
        private bool _isInstalling = false;

        public NetworkUpdateResult Result { get; private set; } = NetworkUpdateResult.RemindLater;

        public NetworkUpdateWindow(NetworkUpdateInfo updateInfo, NetworkUpdateService updateService)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateService = updateService;
            
            LoadUpdateInfo();
            SetupMandatoryUpdate();
        }

        private void LoadUpdateInfo()
        {
            CurrentVersionText.Text = _updateInfo.CurrentVersion;
            LatestVersionText.Text = _updateInfo.LatestVersion;
            ReleaseDateText.Text = _updateInfo.ReleaseDate.ToString("MMMM dd, yyyy");
            FileSizeText.Text = FormatFileSize(_updateInfo.FileSize);
            NetworkPathText.Text = _updateInfo.NetworkPath;
            
            ReleaseNotesText.Text = string.IsNullOrEmpty(_updateInfo.ReleaseNotes) 
                ? "No release notes available." 
                : _updateInfo.ReleaseNotes;
        }

        private void SetupMandatoryUpdate()
        {
            if (_updateInfo.IsMandatory)
            {
                MandatoryUpdateBorder.Visibility = Visibility.Visible;
                RemindLaterButton.Visibility = Visibility.Collapsed;
                
                // For mandatory updates, user cannot close window
                this.Closing += (s, e) =>
                {
                    if (!_isInstalling && Result != NetworkUpdateResult.Installed)
                    {
                        var result = MessageBox.Show(
                            "This is a mandatory update. The application cannot continue without this update.\n\n" +
                            "Do you want to install the update now?",
                            "Mandatory Update Required",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.No)
                        {
                            e.Cancel = true;
                        }
                    }
                };
            }
        }

        private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            try
            {
                _isInstalling = true;
                SetInstallingState(true);

                var progress = new Progress<CopyProgress>(OnCopyProgress);
                var success = await _updateService.InstallNetworkUpdateAsync(_updateInfo, progress);

                if (success)
                {
                    Result = NetworkUpdateResult.Installed;
                    // The app will restart, so we don't need to close this window
                }
                else
                {
                    MessageBox.Show(
                        "Failed to install the network update. Please check:\n\n" +
                        "• Network connectivity to update server\n" +
                        "• File permissions\n" +
                        "• Available disk space\n\n" +
                        "Contact your IT administrator if the problem persists.",
                        "Update Installation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    
                    SetInstallingState(false);
                    _isInstalling = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred during update installation:\n\n{ex.Message}\n\n" +
                    "Contact your IT administrator for assistance.",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                SetInstallingState(false);
                _isInstalling = false;
            }
        }

        private void OnCopyProgress(CopyProgress progress)
        {
            Dispatcher.Invoke(() =>
            {
                CopyProgressBar.Value = progress.ProgressPercentage;
                ProgressText.Text = $"Copying update from network... {progress.ProgressPercentage}%";
                ProgressDetailText.Text = $"{FormatFileSize(progress.BytesCopied)} / {FormatFileSize(progress.TotalBytes)}";
            });
        }

        private void SetInstallingState(bool isInstalling)
        {
            ProgressPanel.Visibility = isInstalling ? Visibility.Visible : Visibility.Collapsed;
            InstallUpdateButton.IsEnabled = !isInstalling;
            RemindLaterButton.IsEnabled = !isInstalling;
            TestNetworkButton.IsEnabled = !isInstalling;

            if (isInstalling)
            {
                InstallUpdateButton.Content = "Installing...";
            }
            else
            {
                InstallUpdateButton.Content = "Install Update";
                CopyProgressBar.Value = 0;
                ProgressText.Text = "Copying update from network...";
                ProgressDetailText.Text = "";
            }
        }

        private void RemindLaterButton_Click(object sender, RoutedEventArgs e)
        {
            Result = NetworkUpdateResult.RemindLater;
            DialogResult = false;
        }

        private async void TestNetworkButton_Click(object sender, RoutedEventArgs e)
        {
            TestNetworkButton.IsEnabled = false;
            TestNetworkButton.Content = "Testing...";

            try
            {
                var status = await _updateService.GetNetworkUpdateStatusAsync();
                
                MessageBox.Show(
                    $"Network Update Status:\n\n{status}",
                    "Network Test Results",
                    MessageBoxButton.OK,
                    status.StartsWith("✅") ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Network test failed:\n\n{ex.Message}",
                    "Network Test Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                TestNetworkButton.IsEnabled = true;
                TestNetworkButton.Content = "Test Network";
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public enum NetworkUpdateResult
    {
        RemindLater,
        Installed
    }
}
