using Contextualizer.PluginContracts;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using WpfInteractionApp;
using WpfInteractionApp.Services;

namespace WpfInteractionApp.Pages
{
    public partial class LogViewerPage : Page
    {
        private ActivityLogService? _logService;
        private ICollectionView? _view;

        private string _searchText = string.Empty;
        private LogType? _selectedLogLevel = null;

        public LogViewerPage()
        {
            InitializeComponent();
            Loaded += (_, _) => Initialize();
        }

        private void Initialize()
        {
            _logService = Contextualizer.Core.ServiceLocator.SafeGet<ActivityLogService>();
            if (_logService == null)
            {
                // Fallback (shouldn't normally happen because App registers it)
                _logService = new ActivityLogService();
            }

            _view = CollectionViewSource.GetDefaultView(_logService.Logs);
            _view.Filter = Filter;
            LogList.ItemsSource = _view;
            _view.Refresh();
        }

        private bool Filter(object obj)
        {
            if (obj is not LogEntry log)
                return false;

            bool matchesSearch =
                string.IsNullOrWhiteSpace(_searchText) ||
                (log.Message?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (log.AdditionalInfo?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);

            bool matchesLevel = _selectedLogLevel == null || log.Type == _selectedLogLevel;

            return matchesSearch && matchesLevel;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text ?? string.Empty;
            _view?.Refresh();
        }

        private void LevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LevelFilter.SelectedItem is not ComboBoxItem item)
                return;

            string selected = item.Content?.ToString() ?? "All";
            _selectedLogLevel = selected switch
            {
                "Success" => LogType.Success,
                "Error" => LogType.Error,
                "Warning" => LogType.Warning,
                "Info" => LogType.Info,
                "Debug" => LogType.Debug,
                "Critical" => LogType.Critical,
                _ => null
            };

            _view?.Refresh();
        }

        private void Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _logService?.Clear();
            _view?.Refresh();
        }
    }
}


