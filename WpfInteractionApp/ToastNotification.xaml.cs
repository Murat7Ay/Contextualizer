using Contextualizer.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfInteractionApp
{
    public partial class ToastNotification : Window
    {
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
            InitializeComponent();
            
            _remainingSeconds = durationInSeconds ?? 3;

            // Set window properties
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.WidthAndHeight;

            // Set title and message
            TitleBlock.Text = string.IsNullOrEmpty(title) ? notificationType.ToString() : title;
            MessageBlock.Text = message.Replace("\\r\\n", Environment.NewLine)
                                     .Replace("\\n", Environment.NewLine)
                                     .Replace("\\r", Environment.NewLine);

            // Add action button if needed
            if (onActionClicked != null)
            {
                var button = new Button
                {
                    Content = "Eylem",
                    Style = (Style)FindResource("Carbon.Button.Base"),
                    Margin = new Thickness(0, 12, 0, 0)
                };
                button.Click += (s, e) => { onActionClicked.Invoke(); Close(); };
                MainPanel.Children.Add(button);
            }

            // Timer setup
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
                        Close();
                        _timer.Stop();
                    }
                }
            };

            // Event handlers
            Closed += (s, e) => SavePosition();
            MouseEnter += (s, e) => { _isPaused = true; };
            MouseLeave += (s, e) => { _isPaused = false; };

            // Mouse events for dragging
            MouseLeftButtonDown += Window_MouseLeftButtonDown;
            MouseMove += Window_MouseMove;
            MouseLeftButtonUp += Window_MouseLeftButtonUp;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _mouseStartPoint = PointToScreen(e.GetPosition(this));
            _windowStartPoint = new Point(Left, Top);
            DragMove();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = PointToScreen(e.GetPosition(this));
                var offset = mousePos - _mouseStartPoint;
                double newLeft = _windowStartPoint.X + offset.X;
                double newTop = _windowStartPoint.Y + offset.Y;

                newLeft = Math.Max(0, Math.Min(SystemParameters.WorkArea.Width - ActualWidth, newLeft));
                newTop = Math.Max(0, Math.Min(SystemParameters.WorkArea.Height - ActualHeight, newTop));

                Left = newLeft;
                Top = newTop;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            SavePosition();
        }

        private void SavePosition()
        {
            Properties.Settings.Default[PositionXKey] = Left;
            Properties.Settings.Default[PositionYKey] = Top;
            Properties.Settings.Default.Save();
        }

        public new void Show()
        {
            _timer.Start();
            base.Show();

            // Position the window after it's shown
            Dispatcher.InvokeAsync(() =>
            {
                double left = Properties.Settings.Default[PositionXKey] is double lx ? lx : SystemParameters.WorkArea.Width - ActualWidth - 10;
                double top = Properties.Settings.Default[PositionYKey] is double ly ? ly : SystemParameters.WorkArea.Height - ActualHeight - 10;
                Left = left;
                Top = top;
            }, DispatcherPriority.Loaded);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}