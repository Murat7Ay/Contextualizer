using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public partial class MainWindow : Window
    {
        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly Dictionary<string, TabItem> _tabs = new Dictionary<string, TabItem>();

        public MainWindow()
        {
            InitializeComponent();
            LogListBox.ItemsSource = _logs;
        }

        public void AddLog(LogEntry log)
        {
            _logs.Add(log);
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
    }

    public class LogEntry
    {
        public LogType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string AdditionalInfo { get; set; }
    }

}