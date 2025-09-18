using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfInteractionApp.Services;
using Contextualizer.Core.Services;

namespace WpfInteractionApp
{
    public partial class MainWindow : Window
    {
        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly Dictionary<string, TabItem> _tabs = new Dictionary<string, TabItem>();
        private HandlerManager? _handlerManager;

        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            LogListBox.ItemsSource = _logs;
            LoadWindowSettings();
            
            // Subscribe to window events for saving settings
            this.SizeChanged += MainWindow_SizeChanged;
            this.LocationChanged += MainWindow_LocationChanged;
            this.StateChanged += MainWindow_StateChanged;
        }

        public void InitializeHandlerManager(HandlerManager handlerManager)
        {
            _handlerManager = handlerManager ?? throw new ArgumentNullException(nameof(handlerManager));
            InitializeManualHandlersMenu();
        }

        private void InitializeManualHandlersMenu()
        {
            if (_handlerManager == null) return;

            ManualHandlersMenu.Items.Clear();
            var handlers = _handlerManager.GetManualHandlerNames();

            foreach (var handler in handlers)
            {
                var menuItem = new MenuItem
                {
                    Header = handler,
                    Style = (Style)FindResource("Carbon.MenuItem.Light")
                };
                menuItem.Click += async (s, e) =>
                {
                    var dialog = new ConfirmationDialog($"Execute {handler}", $"Are you sure you want to execute the {handler}?");
                    if (await dialog.ShowDialogAsync())
                    {
                        _handlerManager.ExecuteManualHandler(handler);
                    }
                };
                ManualHandlersMenu.Items.Add(menuItem);
            }
        }

        public void AddLog(LogEntry log)
        {
            const int maxLength = 120;
            if (!string.IsNullOrEmpty(log.Message) && log.Message.Length > maxLength)
                log.Message = log.Message.Substring(0, maxLength) + "...";

            _logs.Add(log);

            if (_logs.Count > 50)
                _logs.RemoveRange(0, _logs.Count - 50);

            LogListBox.Items.Refresh();
        }

        public void AddOrUpdateTab(string screenId, string title, UIElement content)
        {
            string key = $"{screenId}_{title}";
            if (_tabs.ContainsKey(key))
            {
                _tabs[key].Content = content;
                _tabs[key].IsSelected = true;
            }
            else
            {
                var tabItem = new TabItem
                {
                    Content = new Grid
                    {
                        Children = { content }
                    },
                    IsSelected = true,
                };

                // Header için StackPanel oluştur
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                headerPanel.Children.Add(new TextBlock
                {
                    Text = title,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                });

                var closeButton = new Button
                {
                    Style = (Style)FindResource("CloseButtonStyle"),
                    Tag = tabItem,
                    ToolTip = "Close Tab"
                };
                closeButton.Click += CloseTab_Click;
                headerPanel.Children.Add(closeButton);

                tabItem.Header = headerPanel;

                TabControl.Items.Add(tabItem);
                _tabs.Add(key, tabItem);
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TabItem tabItem)
            {
                string key = _tabs.FirstOrDefault(x => x.Value == tabItem).Key;
                if (!string.IsNullOrEmpty(key))
                {
                    _tabs.Remove(key);
                    TabControl.Items.Remove(tabItem);
                }
            }
        }

        private void OnThemeChanged(object? sender, string theme)
        {
            Debug.WriteLine($"Theme changed to: {theme}");
            AddLog(new LogEntry
            {
                Type = LogType.Info,
                Message = $"Theme changed to: {theme}",
                Timestamp = DateTime.Now
            });

            foreach (var tab in _tabs.Values)
            {
                if (tab.Content is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is DependencyObject depObj)
                        {
                            foreach (var themeAware in FindThemeAwareChildren(depObj))
                            {
                                themeAware.OnThemeChanged(theme);
                            }
                        }
                    }
                }
            }
        }
        private static IEnumerable<IThemeAware> FindThemeAwareChildren(DependencyObject parent)
        {
            if (parent is IThemeAware aware)
                yield return aware;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                foreach (var descendant in FindThemeAwareChildren(child))
                    yield return descendant;
            }
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.CycleTheme();
        }

        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme("Light");
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme("Dark");
        }

        private void DimTheme_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ApplyTheme("Dim");
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = ServiceLocator.Get<SettingsService>();
            var settingsWindow = new SettingsWindow(settingsService.Settings);

            if (settingsWindow.ShowDialog() == true)
            {
                settingsService.SaveSettings();
                // TODO: Restart the application or reload settings
                MessageBox.Show("Settings saved. Please restart the application for changes to take effect.",
                    "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoggingSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loggingSettingsWindow = new LoggingSettingsWindow
                {
                    Owner = this
                };

                loggingSettingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logging settings: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Log the error if logging service is available
                ServiceLocator.SafeExecute<ILoggingService>(logger => 
                    logger.LogError("Failed to open logging settings window", ex));
            }
        }

        private void OpenHandlerExchange_Click(object sender, RoutedEventArgs e)
        {
            var exchangeWindow = new HandlerExchangeWindow();
            exchangeWindow.Show();
        }

        private void OpenCronManager_Click(object sender, RoutedEventArgs e)
        {
            var cronManagerWindow = new CronManagerWindow();
            cronManagerWindow.Owner = this;
            cronManagerWindow.Show();
        }

        private void LoadWindowSettings()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowSettings = settingsService.Settings.WindowSettings;

                // Set window size
                this.Width = windowSettings.Width;
                this.Height = windowSettings.Height;

                // Set window position if valid
                if (!double.IsNaN(windowSettings.Left) && !double.IsNaN(windowSettings.Top))
                {
                    this.Left = windowSettings.Left;
                    this.Top = windowSettings.Top;
                }
                else
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // Set window state
                if (Enum.TryParse<WindowState>(windowSettings.WindowState, out var state))
                {
                    this.WindowState = state;
                }

                // Set grid splitter position
                if (this.FindName("GridSplitter") is GridSplitter splitter)
                {
                    var grid = splitter.Parent as Grid;
                    if (grid != null && grid.RowDefinitions.Count > 2)
                    {
                        grid.RowDefinitions[2].Height = new GridLength(windowSettings.GridSplitterPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                // If settings loading fails, use defaults
                AddLog(new LogEntry
                {
                    Type = LogType.Warning,
                    Message = $"Failed to load window settings: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowSettings = settingsService.Settings.WindowSettings;

                // Save window size and position
                windowSettings.Width = this.Width;
                windowSettings.Height = this.Height;
                windowSettings.Left = this.Left;
                windowSettings.Top = this.Top;
                windowSettings.WindowState = this.WindowState.ToString();

                // Save grid splitter position
                if (this.FindName("GridSplitter") is GridSplitter splitter)
                {
                    var grid = splitter.Parent as Grid;
                    if (grid != null && grid.RowDefinitions.Count > 2)
                    {
                        windowSettings.GridSplitterPosition = grid.RowDefinitions[2].Height.Value;
                    }
                }

                settingsService.SaveSettings();
            }
            catch (Exception ex)
            {
                AddLog(new LogEntry
                {
                    Type = LogType.Error,
                    Message = $"Failed to save window settings: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SaveWindowSettings();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            SaveWindowSettings();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            SaveWindowSettings();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SaveWindowSettings();
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            
            // Unsubscribe from events
            this.SizeChanged -= MainWindow_SizeChanged;
            this.LocationChanged -= MainWindow_LocationChanged;
            this.StateChanged -= MainWindow_StateChanged;
        }
    }

    public class LogEntry
    {
        public LogType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string AdditionalInfo { get; set; }
    }
}