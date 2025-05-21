using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfInteractionApp.Services;
using System.Diagnostics;

namespace WpfInteractionApp
{
    public partial class MainWindow : Window
    {
        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly Dictionary<string, TabItem> _tabs = new Dictionary<string, TabItem>();
        private HandlerManager? _handlerManager;
        private readonly ThemeService _themeService;

        public MainWindow()
        {
            InitializeComponent();
            
            _themeService = ServiceLocator.Get<ThemeService>();
            _themeService.ThemeChanged += OnThemeChanged;

            LogListBox.ItemsSource = _logs;
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
                    Style = (Style)FindResource("Carbon.MenuItem")
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
                    Content = content,
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

        private void OnThemeChanged(object? sender, ThemeType theme)
        {
            Debug.WriteLine($"Theme changed to: {theme}");
            AddLog(new LogEntry 
            { 
                Type = LogType.Info, 
                Message = $"Theme changed to: {theme}", 
                Timestamp = DateTime.Now 
            });
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ToggleTheme_Click called");
            AddLog(new LogEntry 
            { 
                Type = LogType.Info, 
                Message = $"Toggling theme from: {_themeService.CurrentTheme}", 
                Timestamp = DateTime.Now 
            });
            _themeService.SwitchTheme();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _themeService.ThemeChanged -= OnThemeChanged;
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