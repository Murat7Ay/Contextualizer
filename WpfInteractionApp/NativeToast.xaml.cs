using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class NativeToast : Window
    {
        private readonly DispatcherTimer _timer;
        private int _remainingSeconds;
        private int _totalSeconds;
        private bool _isPaused = false;
        private bool _isDragging = false;
        private Point _windowStartPoint;
        private Point _mouseStartPoint;
        private ToastAction[] _actions;
        private ToastAction? _defaultAction;

        public NativeToast(string message, int durationInSeconds = 5, string title = "", LogType notificationType = LogType.Info, params ToastAction[] actions)
        {
            InitializeComponent();
            
            _remainingSeconds = durationInSeconds;
            _totalSeconds = _remainingSeconds;
            _actions = actions ?? Array.Empty<ToastAction>();
            _defaultAction = _actions.FirstOrDefault(a => a.IsDefaultAction);

            // Set title and message
            TitleBlock.Text = string.IsNullOrEmpty(title) ? notificationType.ToString() : title;
            MessageBlock.Text = FormatMessage(message);
            MessageBlock.CaretIndex = 0;

            // Setup notification type styling
            SetupNotificationStyle(notificationType);

            // Initialize timer display
            UpdateTimerDisplay();

            // Add action buttons if provided
            if (_actions.Length > 0)
            {
                AddActionButtons(_actions);
            }

            // Timer setup
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                if (!_isPaused)
                {
                    _remainingSeconds--;
                    UpdateTimerDisplay();
                    
                    if (_remainingSeconds <= 0)
                    {
                        _timer.Stop();
                        ExecuteDefaultActionAndClose();
                    }
                }
            };

            // Event handlers
            Closed += (s, e) => SavePosition();
            MouseEnter += (s, e) => { _isPaused = true; };
            MouseLeave += (s, e) => { _isPaused = false; };
            MouseLeftButtonDown += Window_MouseLeftButtonDown;
            MouseMove += Window_MouseMove;
            MouseLeftButtonUp += Window_MouseLeftButtonUp;
        }

        private static string FormatMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            try { return Regex.Unescape(message); }
            catch { return message; }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _mouseStartPoint = PointToScreen(e.GetPosition(this));
            _windowStartPoint = new Point(Left, Top);
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = PointToScreen(e.GetPosition(this));
                var offset = mousePos - _mouseStartPoint;
                double newLeft = Math.Max(0, Math.Min(SystemParameters.WorkArea.Width - ActualWidth, _windowStartPoint.X + offset.X));
                double newTop = Math.Max(0, Math.Min(SystemParameters.WorkArea.Height - ActualHeight, _windowStartPoint.Y + offset.Y));
                Left = newLeft;
                Top = newTop;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            SavePosition();
        }

        private void SavePosition()
        {
            try
            {
                var settingsService = ServiceLocator.SafeGet<SettingsService>();
                if (settingsService != null && IsValidPosition(Left) && IsValidPosition(Top))
                {
                    settingsService.Settings.UISettings.ToastPositionX = Left;
                    settingsService.Settings.UISettings.ToastPositionY = Top;
                    settingsService.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not save toast position: {ex.Message}");
            }
        }

        private static bool IsValidPosition(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;

        public new void Show()
        {
            SetInitialPosition();
            _timer.Start();
            base.Show();
        }

        private void SetInitialPosition()
        {
            double left, top;
            try
            {
                var settingsService = ServiceLocator.SafeGet<SettingsService>();
                left = settingsService?.Settings.UISettings.ToastPositionX ?? 0;
                top = settingsService?.Settings.UISettings.ToastPositionY ?? 0;
                
                if ((left == 0 && top == 0) || !IsValidPosition(left) || !IsValidPosition(top) ||
                    left > SystemParameters.WorkArea.Width || top > SystemParameters.WorkArea.Height)
                {
                    left = SystemParameters.WorkArea.Width - 420;
                    top = SystemParameters.WorkArea.Height - 150;
                }
            }
            catch
            {
                left = SystemParameters.WorkArea.Width - 420;
                top = SystemParameters.WorkArea.Height - 150;
            }
            
            Left = Math.Max(0, Math.Min(SystemParameters.WorkArea.Width - 100, left));
            Top = Math.Max(0, Math.Min(SystemParameters.WorkArea.Height - 50, top));
        }

        private void SetupNotificationStyle(LogType notificationType)
        {
            Color iconColor, backgroundColor;
            string iconText;

            switch (notificationType)
            {
                case LogType.Error:
                    iconColor = Color.FromRgb(239, 68, 68);
                    backgroundColor = Color.FromRgb(254, 242, 242);
                    iconText = "✕";
                    break;
                case LogType.Warning:
                    iconColor = Color.FromRgb(245, 158, 11);
                    backgroundColor = Color.FromRgb(255, 251, 235);
                    iconText = "⚠";
                    break;
                case LogType.Success:
                    iconColor = Color.FromRgb(34, 197, 94);
                    backgroundColor = Color.FromRgb(240, 253, 244);
                    iconText = "✓";
                    break;
                default:
                    iconColor = Color.FromRgb(59, 130, 246);
                    backgroundColor = Color.FromRgb(239, 246, 255);
                    iconText = "ℹ";
                    break;
            }

            IconText.Text = iconText;
            IconText.Foreground = new SolidColorBrush(iconColor);
            IconBorder.Background = new SolidColorBrush(backgroundColor);
        }

        private void UpdateTimerDisplay()
        {
            TimerText.Text = $"{_remainingSeconds}s";
            double progress = (_totalSeconds - _remainingSeconds) / (double)_totalSeconds;
            
            Dispatcher.BeginInvoke(() =>
            {
                if (ActualWidth > 0 && ProgressBar != null)
                {
                    if (ProgressBar.RenderTransform == null || ProgressBar.RenderTransform is not ScaleTransform)
                    {
                        ProgressBar.RenderTransform = new ScaleTransform();
                        ProgressBar.RenderTransformOrigin = new Point(0, 0.5);
                    }
                    
                    var scaleTransform = (ScaleTransform)ProgressBar.RenderTransform;
                    var scaleAnimation = new DoubleAnimation
                    {
                        To = progress,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                }
            });
            
            UpdateCircularProgress(progress);
        }

        private void UpdateCircularProgress(double progress)
        {
            if (ProgressArc == null) return;

            double angle = progress * 360;
            double radius = 10;
            Point center = new Point(12, 12);
            
            Point startPoint = new Point(center.X, center.Y - radius);
            Point endPoint = new Point(
                center.X + radius * Math.Sin(angle * Math.PI / 180),
                center.Y - radius * Math.Cos(angle * Math.PI / 180)
            );

            bool isLargeArc = angle > 180;
            
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = startPoint };
            
            if (angle > 0)
            {
                pathFigure.Segments.Add(new ArcSegment
                {
                    Point = endPoint,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = isLargeArc
                });
            }
            
            pathGeometry.Figures.Add(pathFigure);
            ProgressArc.Data = pathGeometry;
        }

        private void AddActionButtons(ToastAction[] actions)
        {
            ActionButtonsPanel.Children.Clear();
            ActionButtonsPanel.Visibility = Visibility.Visible;

            foreach (var action in actions)
            {
                var button = new Button
                {
                    Content = action.Text,
                    Style = GetButtonStyle(action.Style)
                };

                button.Click += (s, e) =>
                {
                    try { action.Action?.Invoke(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Toast action error: {ex.Message}"); }
                    finally { if (action.CloseOnClick) Close(); }
                };

                ActionButtonsPanel.Children.Add(button);
            }
        }

        private Style GetButtonStyle(ToastActionStyle actionStyle)
        {
            string styleKey = actionStyle switch
            {
                ToastActionStyle.Primary => "ToastActionButton.Primary",
                ToastActionStyle.Danger => "ToastActionButton.Danger",
                _ => "ToastActionButton.Secondary"
            };
            return (Style)FindResource(styleKey);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteDefaultActionAndClose();
        }

        private void ExecuteDefaultActionAndClose()
        {
            _timer?.Stop();
            try { _defaultAction?.Action?.Invoke(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Default action error: {ex.Message}"); }
            finally { Close(); }
        }
    }
}

