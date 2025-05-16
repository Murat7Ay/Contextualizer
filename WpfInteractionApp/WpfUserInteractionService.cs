using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfInteractionApp
{
    public class WpfUserInteractionService : IUserInteractionService
    {
        private readonly MainWindow _mainWindow;

        public WpfUserInteractionService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        public void ShowWindow(string screenId, string title, string body, Dictionary<string, string> context, List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var markdownText = body;
                    var markdownViewer = new MarkdownViewer { Text = markdownText };

                    if (actions != null && actions.Count > 0)
                    {
                        var stackPanel = new StackPanel { Margin = new Thickness(10) };
                        stackPanel.Children.Add(markdownViewer);

                        if (actions != null)
                        {
                            foreach (var action in actions)
                            {
                                var button = new Button
                                {
                                    Content = action.Key,
                                    Margin = new Thickness(0, 5, 0, 5),
                                    Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)), // Carbon: #161616
                                    Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)), // Carbon: #F4F4F4
                                    FontFamily = new FontFamily("Segoe UI"),
                                    FontSize = 14,
                                    Padding = new Thickness(10, 5, 10, 5)
                                };
                                button.Click += (s, e) => action.Value?.Invoke(context);
                                stackPanel.Children.Add(button);
                            }
                        }

                        _mainWindow.AddOrUpdateTab(screenId, title, stackPanel);
                    }
                    else
                    {
                        _mainWindow.AddOrUpdateTab(screenId, title, markdownViewer);
                    }
                }
                catch (Exception ex)
                {
                    Log(LogType.Error, $"Sekme açılamadı: {ex.Message}");
                }
            });
        }

        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string additionalInfo = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var logEntry = new LogEntry
                    {
                        Type = notificationType,
                        Message = message,
                        Timestamp = timestamp ?? DateTime.Now,
                        AdditionalInfo = additionalInfo
                    };
                    _mainWindow.AddLog(logEntry);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Log kaydedilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            try
            {
                // Ensure the dialog is shown on the main thread.
                return await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ConfirmationDialog dialog = new ConfirmationDialog(title, message);
                    return dialog.ShowDialogAsync();
                }).Result;
            }
            catch (Exception ex)
            {
                // Log the error with a more specific message and include the title/message.
                Log(LogType.Error, $"Failed to show confirmation dialog. Title: {title}, Message: {message}, Error: {ex.Message}");
                return false; // Return a default value indicating failure.
            }
        }

        public void ShowNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action onActionClicked = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var toast = new ToastNotification(message, durationInSeconds, title, notificationType, onActionClicked);
                    toast.Show();
                }
                catch (Exception ex)
                {
                    Log(LogType.Error, $"Bildirim gösterilemedi: {ex.Message}");
                }
            });
        }

        public async Task ShowActionableNotification(string message, string actionLabel, Action action, LogType notificationType = LogType.Info)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    bool isConfirmed = await ShowConfirmationAsync("Eylem Gerekli", $"{message}\n\n{actionLabel} işlemini gerçekleştirmek istiyor musunuz?");
                    if (isConfirmed)
                    {
                        action?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Log(LogType.Error, $"Eyleme bağlı bildirim gösterilemedi: {ex.Message}");
                }
            });
        }

        public string? GetUserInput(UserInputRequest request)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new UserInputDialog(request);
                    return dialog.ShowDialog() == true ? dialog.UserInput : null;
                }
                catch (Exception ex)
                {
                    Log(LogType.Error, $"Kullanıcı girişi alınamadı: {ex.Message}");
                    return null;
                }
            });
        }

        public void ShowToastMessage(string message, int duration = 3)
        {
            throw new NotImplementedException();
        }
    }
}