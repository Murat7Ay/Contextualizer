using Contextualizer.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfInteractionApp
{
    public partial class ToastNotification : UserControl
    {
        private readonly Window _toastWindow;
        private readonly DispatcherTimer _timer;
        private Point _dragOffset;

        private bool _isDragging = false;
        private Point _windowStartPoint;
        private Point _mouseStartPoint;

        // Konum ayarları için anahtarlar
        private const string PositionXKey = "ToastPositionX";
        private const string PositionYKey = "ToastPositionY";

        private int _remainingSeconds;
        private bool _isPaused = false;

        public ToastNotification(string message, int? durationInSeconds, string title = "", LogType notificationType = LogType.Info, Action? onActionClicked = null)
        {
            _remainingSeconds = durationInSeconds ?? 3;

            _toastWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = notificationType switch
                {
                    LogType.Info => new SolidColorBrush(Color.FromRgb(22, 22, 22)),
                    LogType.Warning => new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                    LogType.Error => new SolidColorBrush(Color.FromRgb(220, 20, 60)),
                    _ => new SolidColorBrush(Color.FromRgb(22, 22, 22))
                },
                Topmost = true,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var titleBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(title) ? notificationType.ToString() : title,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)),
                FontWeight = FontWeights.Bold
            };

            // Seçilebilir TextBox
            var messageBox = new TextBox
            {
                Text = message,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                IsReadOnly = true,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap,
                Cursor = Cursors.IBeam // Seçim için IBeam
            };

            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(messageBox);

            if (onActionClicked != null)
            {
                var button = new Button
                {
                    Content = "Eylem",
                    Margin = new Thickness(0, 5, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
                    Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 12
                };
                button.Click += (s, e) => { onActionClicked.Invoke(); _toastWindow.Close(); };
                stackPanel.Children.Add(button);
            }

            _toastWindow.Content = stackPanel;

            // Sürükle bırak eventleri
            stackPanel.PreviewMouseLeftButtonDown += StackPanel_PreviewMouseLeftButtonDown;
            stackPanel.PreviewMouseMove += StackPanel_PreviewMouseMove;
            stackPanel.PreviewMouseLeftButtonUp += StackPanel_PreviewMouseLeftButtonUp;

            // Timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                if (!_isPaused)
                {
                    _remainingSeconds--;
                    if (_remainingSeconds <= 0)
                    {
                        _toastWindow.Close();
                        _timer.Stop();
                    }
                }
            };
            _toastWindow.Closed += (s, e) => SavePosition();

            // Mouse enter/leave ile timer kontrolü
            stackPanel.MouseEnter += (s, e) => { _isPaused = true; };
            stackPanel.MouseLeave += (s, e) => { _isPaused = false; };
        }

        private void StackPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Eğer tıklanan yer bir TextBox ise sürükleme başlatma
            if (e.OriginalSource is DependencyObject depObj && FindParent<TextBox>(depObj) != null)
                return;

            _isDragging = true;
            _mouseStartPoint = _toastWindow.PointToScreen(e.GetPosition(_toastWindow));
            _windowStartPoint = new Point(_toastWindow.Left, _toastWindow.Top);
            ((UIElement)sender).CaptureMouse();
        }

        private void StackPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = _toastWindow.PointToScreen(e.GetPosition(_toastWindow));
                var offset = mousePos - _mouseStartPoint;
                double newLeft = _windowStartPoint.X + offset.X;
                double newTop = _windowStartPoint.Y + offset.Y;

                newLeft = Math.Max(0, Math.Min(SystemParameters.WorkArea.Width - _toastWindow.Width, newLeft));
                newTop = Math.Max(0, Math.Min(SystemParameters.WorkArea.Height - _toastWindow.Height, newTop));

                _toastWindow.Left = newLeft;
                _toastWindow.Top = newTop;
            }
        }

        private void StackPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
            SavePosition();
        }

        private void SavePosition()
        {
            Properties.Settings.Default[PositionXKey] = _toastWindow.Left;
            Properties.Settings.Default[PositionYKey] = _toastWindow.Top;
            Properties.Settings.Default.Save();
        }

        public void Show()
        {
            _timer.Start();
            _toastWindow.Show();

            // Boyut belirlendikten sonra konumlandır
            _toastWindow.Dispatcher.InvokeAsync(() =>
            {
                double left = Properties.Settings.Default[PositionXKey] is double lx ? lx : SystemParameters.WorkArea.Width - _toastWindow.ActualWidth - 10;
                double top = Properties.Settings.Default[PositionYKey] is double ly ? ly : SystemParameters.WorkArea.Height - _toastWindow.ActualHeight - 10;
                _toastWindow.Left = left;
                _toastWindow.Top = top;
            }, DispatcherPriority.Loaded);
        }

        // Yardımcı: Belirli bir tipte parent bulur
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}