using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfInteractionApp.Services;

namespace WpfInteractionApp.Pages
{
    public partial class HandlersPage : Page
    {
        private readonly Dictionary<string, TabItem> _tabs = new Dictionary<string, TabItem>();

        public HandlersPage()
        {
            InitializeComponent();

            UiPageRegistry.Instance.HandlersPage = this;

            Loaded += (_, _) =>
            {
                UiPageRegistry.Instance.HandlersPage = this;
                UpdateEmptyState();
            };

            Unloaded += (_, _) =>
            {
                if (ReferenceEquals(UiPageRegistry.Instance.HandlersPage, this))
                    UiPageRegistry.Instance.HandlersPage = null;
            };
        }

        public void AddOrUpdateTab(string screenId, string title, UIElement content, bool autoFocus = false)
        {
            string key = $"{screenId}_{title}";

            if (_tabs.TryGetValue(key, out var existingTab))
            {
                existingTab.Content = WrapContent(content);
                if (autoFocus)
                {
                    existingTab.IsSelected = true;
                    TabControl.SelectedItem = existingTab;
                }

                UpdateEmptyState();
                return;
            }

            var tabItem = new TabItem
            {
                Content = WrapContent(content),
                IsSelected = autoFocus
            };

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

            // Middle mouse button (wheel click) to close tab - Chrome-like behavior
            headerPanel.Tag = tabItem;
            headerPanel.MouseDown += TabHeader_MouseDown;
            headerPanel.ToolTip = "Middle-click to close tab";

            tabItem.Header = headerPanel;

            TabControl.Items.Add(tabItem);
            _tabs.Add(key, tabItem);

            if (autoFocus)
                TabControl.SelectedItem = tabItem;

            UpdateEmptyState();
        }

        private static Grid WrapContent(UIElement content)
        {
            return new Grid { Children = { content } };
        }

        private void UpdateEmptyState()
        {
            bool hasTabs = _tabs.Count > 0;
            EmptyStateCard.Visibility = hasTabs ? Visibility.Collapsed : Visibility.Visible;
            TabControl.Visibility = hasTabs ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TabItem tabItem)
            {
                CloseTabItem(tabItem);
            }
        }

        private void TabHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && sender is StackPanel panel && panel.Tag is TabItem tabItem)
            {
                CloseTabItem(tabItem);
                e.Handled = true;
            }
        }

        private void CloseTabItem(TabItem tabItem)
        {
            string key = _tabs.FirstOrDefault(x => x.Value == tabItem).Key;
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                if (tabItem.Content is Grid grid)
                {
                    // Dispose any IDisposable child (WebView2-based screens etc.)
                    foreach (var child in grid.Children.OfType<UIElement>().ToList())
                    {
                        if (child is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    grid.Children.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing tab content: {ex.Message}");
            }

            _tabs.Remove(key);
            TabControl.Items.Remove(tabItem);

            UpdateEmptyState();

            (Application.Current.MainWindow as MainWindow)?.AddLog(new LogEntry
            {
                Type = LogType.Debug,
                Message = $"Tab closed: {key}",
                Timestamp = DateTime.Now
            });
        }

        public void DisposeAllTabs()
        {
            try
            {
                foreach (var tab in _tabs.Values.ToList())
                {
                    CloseTabItem(tab);
                }

                _tabs.Clear();
                TabControl.Items.Clear();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisposeAllTabs failed: {ex}");
            }
        }

        public void NotifyThemeChanged(string theme)
        {
            try
            {
                foreach (var tab in _tabs.Values)
                {
                    if (tab.Content is not Grid grid)
                        continue;

                    foreach (var child in grid.Children)
                    {
                        if (child is not DependencyObject depObj)
                            continue;

                        foreach (var themeAware in FindThemeAwareChildren(depObj))
                        {
                            themeAware.OnThemeChanged(theme);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NotifyThemeChanged failed: {ex.Message}");
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

        private void ManageHandlers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new HandlerManagementWindow { Owner = Application.Current.MainWindow };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Handler management açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Marketplace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new HandlerExchangeWindow { Owner = Application.Current.MainWindow };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Marketplace açılamadı: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManualHandlers_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as MainWindow)?.ShowManualHandlersMenu(sender as FrameworkElement);
        }
    }
}


