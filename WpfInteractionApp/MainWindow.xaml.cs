﻿using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly ObservableCollection<LogEntry> _logs = new ObservableCollection<LogEntry>();
        private readonly ObservableCollection<LogEntry> _filteredLogs = new ObservableCollection<LogEntry>();
        private readonly Dictionary<string, TabItem> _tabs = new Dictionary<string, TabItem>();
        private HandlerManager? _handlerManager;

        // ✨ Dashboard Properties
        public int ActiveHandlerCount => _handlerManager?.GetHandlerCount() ?? 0;
        public int ActiveCronJobs => ServiceLocator.SafeGet<ICronService>()?.GetActiveJobCount() ?? 0;
        public bool ShowWelcomeDashboard => _tabs.Count == 0;

        // ✨ Log Filtering Properties
        private string _logSearchText = "";
        private LogType? _selectedLogLevel = null;

        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            LogListBox.ItemsSource = _filteredLogs;
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
            UpdateDashboard();
        }

        // ✨ Dashboard Methods
        private void UpdateDashboard()
        {
            // Update dashboard visibility and stats
            if (WelcomeDashboard != null)
            {
                WelcomeDashboard.Visibility = ShowWelcomeDashboard ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Update TabControl visibility (opposite of dashboard)
            if (TabControl != null)
            {
                TabControl.Visibility = ShowWelcomeDashboard ? Visibility.Collapsed : Visibility.Visible;
            }
            
            // Update stats if dashboard is visible
            if (ShowWelcomeDashboard)
            {
                UpdateDashboardStats();
            }
        }

        private void UpdateDashboardStats()
        {
            // Update handler count
            if (HandlerCountText != null)
                HandlerCountText.Text = ActiveHandlerCount.ToString();
            
            // Update cron jobs count  
            if (CronJobsCountText != null)
                CronJobsCountText.Text = ActiveCronJobs.ToString();
        }

        // ✨ Log Filtering Methods
        private void FilterLogs()
        {
            // ✅ Null-safe check for LogListBox (might be called before InitializeComponent)
            if (LogListBox == null) return;
            
            // ⚡ Performance optimization: Use batch operations to minimize UI updates
            var filtered = _logs.Where(log =>
            {
                // Text search filter
                bool matchesSearch = string.IsNullOrEmpty(_logSearchText) ||
                                   log.Message.Contains(_logSearchText, StringComparison.OrdinalIgnoreCase) ||
                                   (log.AdditionalInfo?.Contains(_logSearchText, StringComparison.OrdinalIgnoreCase) ?? false);
                
                // Log level filter
                bool matchesLevel = _selectedLogLevel == null || log.Type == _selectedLogLevel;
                
                return matchesSearch && matchesLevel;
            }).ToList();
            
            // ⚡ Clear and add in batch to minimize ObservableCollection notifications
            _filteredLogs.Clear();
            foreach (var log in filtered)
            {
                _filteredLogs.Add(log);
            }
            
            LogListBox.Items.Refresh();
            
            // Auto-select newest log if available
            if (_filteredLogs.Count > 0)
            {
                LogListBox.SelectedIndex = 0;
            }
        }

        private void InitializeManualHandlersMenu()
        {
            // Manual handlers are now handled by the toolbar button context menu
            // No need to populate a menu here anymore
        }

        public void AddLog(LogEntry log)
        {
            const int maxLength = 120;
            if (!string.IsNullOrEmpty(log.Message) && log.Message.Length > maxLength)
                log.Message = log.Message.Substring(0, maxLength) + "...";

            // ✅ Insert at the beginning so newest logs appear at top
            _logs.Insert(0, log);

            // ✅ Remove oldest logs from the end (ObservableCollection doesn't have RemoveRange)
            while (_logs.Count > 50)
                _logs.RemoveAt(_logs.Count - 1);

            // ✨ Apply filtering to update filtered logs
            FilterLogs();
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
            
            // ✅ Update dashboard visibility when tabs change
            UpdateDashboard();
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
                    
                    // ✅ Update dashboard visibility when tabs change
                    UpdateDashboard();
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

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
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

        // ✨ Dashboard Event Handlers
        private void ManageHandlers_Click(object sender, RoutedEventArgs e)
        {
            // Open handler management (existing functionality)
            SettingsMenuItem_Click(sender, e);
        }

        private void CronManager_Click(object sender, RoutedEventArgs e)
        {
            // Open cron manager (existing functionality)
            OpenCronManager_Click(sender, e);
        }

        private void Marketplace_Click(object sender, RoutedEventArgs e)
        {
            // Open handler exchange (existing functionality)
            OpenHandlerExchange_Click(sender, e);
        }

        // ✨ Log Panel Event Handlers
        private void LogSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _logSearchText = textBox.Text;
                FilterLogs();
            }
        }

        private void LogLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedLevel = selectedItem.Content.ToString();
                _selectedLogLevel = selectedLevel switch
                {
                    "Success" => LogType.Success,
                    "Error" => LogType.Error,
                    "Warning" => LogType.Warning,
                    "Info" => LogType.Info,
                    "Debug" => LogType.Debug,
                    "Critical" => LogType.Critical,
                    _ => null
                };
                FilterLogs();
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            _logs.Clear();
            FilterLogs();
        }

        // ✨ Toolbar Event Handlers
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            // Close all tabs to show dashboard
            TabControl.Items.Clear();
            _tabs.Clear();
            UpdateDashboard();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string theme = selectedItem.Tag?.ToString() ?? "Light";
                switch (theme)
                {
                    case "Light":
                        LightTheme_Click(sender, new RoutedEventArgs());
                        break;
                    case "Dark":
                        DarkTheme_Click(sender, new RoutedEventArgs());
                        break;
                    case "Dim":
                        DimTheme_Click(sender, new RoutedEventArgs());
                        break;
                }
            }
        }

        private void ManualHandlersButton_Click(object sender, RoutedEventArgs e)
        {
            // Create context menu for manual handlers
            var contextMenu = new ContextMenu()
            {
                Background = (Brush)FindResource("Carbon.Brush.Background.Primary"),
                BorderBrush = (Brush)FindResource("Carbon.Brush.Background.Tertiary"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = 0.2,
                    BlurRadius = 8,
                    ShadowDepth = 2
                }
            };
            
            if (_handlerManager != null)
            {
                var handlers = _handlerManager.GetManualHandlerNames();
                foreach (var handler in handlers)
                {
                    var menuItem = new MenuItem
                    {
                        Header = handler,
                        Style = (Style)FindResource("Carbon.MenuItem.Light")
                    };
                    menuItem.Click += async (s, args) =>
                    {
                        try
                        {
                            await _handlerManager.ExecuteManualHandlerAsync(handler);
                        }
                        catch (Exception ex)
                        {
                            AddLog(new LogEntry
                            {
                                Type = LogType.Error,
                                Message = $"Failed to execute manual handler '{handler}': {ex.Message}",
                                Timestamp = DateTime.Now
                            });
                        }
                    };
                    contextMenu.Items.Add(menuItem);
                }
            }
            
            if (contextMenu.Items.Count == 0)
            {
                var noHandlersItem = new MenuItem
                {
                    Header = "No manual handlers available",
                    IsEnabled = false
                };
                contextMenu.Items.Add(noHandlersItem);
            }
            
            contextMenu.PlacementTarget = sender as Button;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
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
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string AdditionalInfo { get; set; } = string.Empty;
    }
}