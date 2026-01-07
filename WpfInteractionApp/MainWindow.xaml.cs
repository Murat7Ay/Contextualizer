using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfInteractionApp.Pages;
using WpfInteractionApp.Services;
using Wpf.Ui.Controls;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MenuItem = System.Windows.Controls.MenuItem;

namespace WpfInteractionApp
{
    public partial class MainWindow : FluentWindow
    {
        private HandlerManager? _handlerManager;

        public MainWindow()
        {
            InitializeComponent();

            RootNavigation.Navigated += RootNavigation_Navigated;

            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            UpdateThemeIcon(ThemeManager.Instance.CurrentTheme);

            LoadWindowSettings();

            SizeChanged += MainWindow_SizeChanged;
            LocationChanged += MainWindow_LocationChanged;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;

            Loaded += (_, _) =>
            {
                // Default landing page
                RootNavigation.Navigate(typeof(DashboardPage));
            };
        }

        public void InitializeHandlerManager(HandlerManager handlerManager)
        {
            _handlerManager = handlerManager ?? throw new ArgumentNullException(nameof(handlerManager));

            // Allow other windows/pages to resolve it (HandlerManagementWindow already depends on this)
            if (ServiceLocator.SafeGet<HandlerManager>() == null)
            {
                ServiceLocator.Register<HandlerManager>(_handlerManager);
            }
        }

        public void NavigateTo(Type pageType)
        {
            try
            {
                RootNavigation.Navigate(pageType);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NavigateTo failed: {ex.Message}");
            }
        }

        public void AddOrUpdateTab(string screenId, string title, UIElement content, bool autoFocus = false)
        {
            Dispatcher.Invoke(() =>
            {
                // Keep behavior close to the legacy app: when a handler opens a screen, take user to it.
                RootNavigation.Navigate(typeof(HandlersPage));

                var handlersPage = UiPageRegistry.Instance.HandlersPage;
                if (handlersPage != null)
                {
                    handlersPage.AddOrUpdateTab(screenId, title, content, autoFocus);
                    return;
                }

                // Defer once if the page hasn't been instantiated yet.
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UiPageRegistry.Instance.HandlersPage?.AddOrUpdateTab(screenId, title, content, autoFocus);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            });
        }

        public void AddLog(LogEntry log)
        {
            // Persist to ActivityLogService
            ServiceLocator.SafeExecute<ActivityLogService>(svc => svc.Add(log));

            // Snackbars only for high-signal items (avoid spamming)
            if (log.Type == LogType.Error || log.Type == LogType.Critical)
                ShowSnackbar("Error", log.Message, ControlAppearance.Danger);
            else if (log.Type == LogType.Warning)
                ShowSnackbar("Warning", log.Message, ControlAppearance.Caution);
            else if (log.Type == LogType.Success)
                ShowSnackbar("Success", log.Message, ControlAppearance.Success);
        }

        public void ShowSnackbar(string title, string message, ControlAppearance appearance = ControlAppearance.Secondary)
        {
            Dispatcher.Invoke(() =>
            {
                if (RootSnackbarPresenter == null)
                    return;

                var snackbar = new Snackbar(RootSnackbarPresenter)
                {
                    Title = title,
                    Content = message,
                    Appearance = appearance,
                    Timeout = TimeSpan.FromSeconds(3),
                    IsCloseButtonEnabled = true
                };

                snackbar.Show();
            });
        }

        private void RootNavigation_Navigated(object sender, RoutedEventArgs e)
        {
            if (RootNavigation.SelectedItem is NavigationViewItem item)
            {
                PageTitleText.Text = item.Content?.ToString() ?? string.Empty;
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.CycleTheme();
        }

        private void OnThemeChanged(object? sender, string theme)
        {
            Debug.WriteLine($"Theme changed to: {theme}");

            UpdateThemeIcon(theme);

            // Notify theme-aware handler screens (MarkdownViewer2, PlSqlEditor, ...)
            UiPageRegistry.Instance.HandlersPage?.NotifyThemeChanged(theme);
        }

        private void UpdateThemeIcon(string theme)
        {
            // Light -> sun, Dark/Dim -> moon (Dim is a dark variant)
            bool isLight = theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
            ThemeToggleIcon.Symbol = isLight ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24;
        }

        private void ReloadHandlers_Click(object sender, RoutedEventArgs e)
        {
            ReloadHandlers();
        }

        public void ReloadHandlers()
        {
            try
            {
                var handlerManager = _handlerManager ?? ServiceLocator.SafeGet<HandlerManager>();
                if (handlerManager == null)
                {
                    MessageBox.Show("Handler manager is not initialized.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    "Reload handlers and plugins?",
                    "Reload",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                var (handlersReloaded, newPluginsLoaded) = handlerManager.ReloadHandlers(reloadPlugins: true);

                ShowSnackbar(
                    "Reload complete",
                    $"Reloaded: {handlersReloaded} handlers, {newPluginsLoaded} plugins",
                    ControlAppearance.Success);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reload handlers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                ServiceLocator.SafeExecute<ILoggingService>(logger =>
                    logger.LogError("Failed to reload handlers", ex));
            }
        }

        private void OpenLoggingSettings_Click(object sender, RoutedEventArgs e)
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

                ServiceLocator.SafeExecute<ILoggingService>(logger =>
                    logger.LogError("Failed to open logging settings window", ex));
            }
        }

        private void ManualHandlersButton_Click(object sender, RoutedEventArgs e)
        {
            ShowManualHandlersMenu(sender as FrameworkElement);
        }

        public void ShowManualHandlersMenu(FrameworkElement? placementTarget)
        {
            var contextMenu = new ContextMenu
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

            var handlerManager = _handlerManager ?? ServiceLocator.SafeGet<HandlerManager>();
            if (handlerManager != null)
            {
                var handlers = handlerManager.GetManualHandlerNames();
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
                            await handlerManager.ExecuteManualHandlerAsync(handler);
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
                contextMenu.Items.Add(new MenuItem
                {
                    Header = "No manual handlers available",
                    IsEnabled = false
                });
            }

            contextMenu.PlacementTarget = placementTarget ?? ManualHandlersButton;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void LoadWindowSettings()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowSettings = settingsService.Settings.WindowSettings;

                Width = windowSettings.Width;
                Height = windowSettings.Height;

                if (!double.IsNaN(windowSettings.Left) && !double.IsNaN(windowSettings.Top))
                {
                    Left = windowSettings.Left;
                    Top = windowSettings.Top;
                }
                else
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                if (Enum.TryParse<WindowState>(windowSettings.WindowState, out var state))
                {
                    WindowState = state;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load window settings: {ex.Message}");
            }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowSettings = settingsService.Settings.WindowSettings;

                windowSettings.Width = Width;
                windowSettings.Height = Height;
                windowSettings.Left = Left;
                windowSettings.Top = Top;
                windowSettings.WindowState = WindowState.ToString();

                settingsService.SaveSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save window settings: {ex.Message}");
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SaveWindowSettings();
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            SaveWindowSettings();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            SaveWindowSettings();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                UiPageRegistry.Instance.HandlersPage?.DisposeAllTabs();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error disposing handler tabs: {ex.Message}");
            }
        }

        /// <summary>
        /// Brings the main window to the front and activates it
        /// </summary>
        public void BringToFront()
        {
            try
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
                Topmost = true;
                Topmost = false;
                Focus();
            }
            catch (Exception ex)
            {
                AddLog(new LogEntry
                {
                    Type = LogType.Warning,
                    Message = $"Could not bring window to front: {ex.Message}",
                    Timestamp = DateTime.Now
                });
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SaveWindowSettings();
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;

            SizeChanged -= MainWindow_SizeChanged;
            LocationChanged -= MainWindow_LocationChanged;
            StateChanged -= MainWindow_StateChanged;
        }
    }
}

 
