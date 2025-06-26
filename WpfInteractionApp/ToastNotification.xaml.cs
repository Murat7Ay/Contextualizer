using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
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
    public partial class ToastNotification : Window
    {
        private readonly DispatcherTimer _timer;
        private Point _dragOffset;

        private bool _isDragging = false;
        private Point _windowStartPoint;
        private Point _mouseStartPoint;


        private int _remainingSeconds;
        private int _totalSeconds;
        private bool _isPaused = false;
        private ToastAction[] _actions;
        private ToastAction _defaultAction;

        private string FormatMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;

            return Regex.Unescape(message);
        }

        public ToastNotification(string message, int? durationInSeconds, string title = "", LogType notificationType = LogType.Info, params ToastAction[] actions)
        {
            InitializeComponent();
            
            _remainingSeconds = durationInSeconds ?? 3;
            _totalSeconds = _remainingSeconds;
            _actions = actions;
            
            // Find default action
            _defaultAction = actions?.FirstOrDefault(a => a.IsDefaultAction);

            // Set window properties
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Topmost = true;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.WidthAndHeight;

            // Set title and message
            TitleBlock.Text = string.IsNullOrEmpty(title) ? notificationType.ToString() : title;
            MessageBlock.Text = FormatMessage(message);
            MessageBlock.CaretIndex = 0;

            // Setup notification type styling
            SetupNotificationStyle(notificationType);

            // Initialize timer display
            UpdateTimerDisplay();

            // Add action buttons if provided
            if (actions != null && actions.Length > 0)
            {
                AddActionButtons(actions);
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
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                
                // Only save valid positions
                if (IsValidPosition(Left) && IsValidPosition(Top))
                {
                    settingsService.Settings.UISettings.ToastPositionX = Left;
                    settingsService.Settings.UISettings.ToastPositionY = Top;
                    settingsService.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                // If settings service is not available, use default position (no fallback needed)
                System.Diagnostics.Debug.WriteLine($"Could not save toast position: {ex.Message}");
            }
        }

        private static bool IsValidPosition(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
        }

        public new void Show()
        {
            _timer.Start();
            base.Show();

            // Position the window after it's shown
            Dispatcher.InvokeAsync(() =>
            {
                double left, top;
                
                try
                {
                    var settingsService = ServiceLocator.Get<SettingsService>();
                    left = settingsService.Settings.UISettings.ToastPositionX;
                    top = settingsService.Settings.UISettings.ToastPositionY;
                    
                    // Use default position if coordinates are 0 (not set) or invalid
                    if (left == 0 && top == 0 || double.IsNaN(left) || double.IsNaN(top) || 
                        left < 0 || top < 0 || left > SystemParameters.WorkArea.Width || top > SystemParameters.WorkArea.Height)
                    {
                        left = SystemParameters.WorkArea.Width - ActualWidth - 10;
                        top = SystemParameters.WorkArea.Height - ActualHeight - 10;
                    }
                }
                catch
                {
                    // Use default position if settings service is not available
                    left = SystemParameters.WorkArea.Width - ActualWidth - 10;
                    top = SystemParameters.WorkArea.Height - ActualHeight - 10;
                }
                
                Left = left;
                Top = top;
            }, DispatcherPriority.Loaded);
        }

        private void SetupNotificationStyle(LogType notificationType)
        {
            Color iconColor, backgroundColor;
            string iconText;

            switch (notificationType)
            {
                case LogType.Error:
                    iconColor = Color.FromRgb(239, 68, 68); // Red
                    backgroundColor = Color.FromRgb(254, 242, 242); // Light red
                    iconText = "✕";
                    break;
                case LogType.Warning:
                    iconColor = Color.FromRgb(245, 158, 11); // Yellow
                    backgroundColor = Color.FromRgb(255, 251, 235); // Light yellow  
                    iconText = "⚠";
                    break;
                case LogType.Success:
                    iconColor = Color.FromRgb(34, 197, 94); // Green
                    backgroundColor = Color.FromRgb(240, 253, 244); // Light green
                    iconText = "✓";
                    break;
                default: // Info
                    iconColor = Color.FromRgb(59, 130, 246); // Blue
                    backgroundColor = Color.FromRgb(239, 246, 255); // Light blue
                    iconText = "ℹ";
                    break;
            }

            // Set icon
            IconText.Text = iconText;
            IconText.Foreground = new SolidColorBrush(iconColor);
            IconBorder.Background = new SolidColorBrush(backgroundColor);
        }

        private void UpdateTimerDisplay()
        {
            TimerText.Text = $"{_remainingSeconds}s";
            
            // Update progress with animation
            double progress = (_totalSeconds - _remainingSeconds) / (double)_totalSeconds;
            
            Dispatcher.BeginInvoke(() =>
            {
                if (ActualWidth > 0) // Make sure window is loaded
                {
                    // Animate progress bar using ScaleTransform
                    if (ProgressBar.RenderTransform == null || !(ProgressBar.RenderTransform is ScaleTransform))
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
            
            // Update circular progress
            UpdateCircularProgress(progress);
        }

        private void UpdateCircularProgress(double progress)
        {
            if (ProgressArc == null) return;

            double angle = progress * 360;
            double radius = 10; // Half of 24px diameter minus stroke
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
                    try
                    {
                        action.Action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Toast action error: {ex.Message}");
                    }
                    finally
                    {
                        if (action.CloseOnClick)
                        {
                            Close();
                        }
                    }
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
            _timer?.Stop(); // Timer'ı durdur
            try
            {
                _defaultAction?.Action?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Default action execution error: {ex.Message}");
            }
            finally
            {
                Close();
            }
        }
    }
}