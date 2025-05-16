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
                // Mevcut sekme içeriğini güncelle
                _tabs[key].Content = content;
                _tabs[key].IsSelected = true;
            }
            else
            {
                // Yeni sekme oluştur
                var tabItem = new TabItem
                {
                    Header = title,
                    Content = content,
                    IsSelected = true,
                };
                TabControl.Items.Add(tabItem);
                _tabs.Add(key, tabItem);
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