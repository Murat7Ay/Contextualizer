using Contextualizer.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfInteractionApp
{
    public partial class ToastNotification : UserControl
    {
        private readonly Window _toastWindow;
        private readonly DispatcherTimer _timer;

        public ToastNotification(string message, int? durationInSeconds, string title = "", LogType notificationType = LogType.Info, Action? onActionClicked = null)
        {
            _toastWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = notificationType switch
                {
                    LogType.Info => new SolidColorBrush(Color.FromRgb(22, 22, 22)), // Carbon: #161616
                    LogType.Warning => new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Turuncu
                    LogType.Error => new SolidColorBrush(Color.FromRgb(220, 20, 60)), // Kırmızı
                    _ => new SolidColorBrush(Color.FromRgb(22, 22, 22))
                },
                Width = 300,
                Height = 100,
                Topmost = true,
                ShowInTaskbar = false
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            var titleBlock = new TextBlock
            {
                Text = string.IsNullOrEmpty(title) ? notificationType.ToString() : title,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)), // Carbon: #F4F4F4
                FontWeight = FontWeights.Bold
            };
            var messageBlock = new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(titleBlock);
            stackPanel.Children.Add(messageBlock);

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
            _toastWindow.SizeToContent = SizeToContent.WidthAndHeight;
            _toastWindow.Left = SystemParameters.WorkArea.Width - _toastWindow.Width - 10;
            _toastWindow.Top = SystemParameters.WorkArea.Height - _toastWindow.Height - 10;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationInSeconds.Value)
            };
            _timer.Tick += (s, e) => { _toastWindow.Close(); _timer.Stop(); };
        }

        public void Show()
        {
            _timer.Start();
            _toastWindow.Show();
        }
    }
}