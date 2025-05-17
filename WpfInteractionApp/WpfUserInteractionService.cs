using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Linq; // Activator ve tip arama için gerekli
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfInteractionApp
{
    public class WpfUserInteractionService : IUserInteractionService
    {
        private readonly MainWindow _mainWindow;
        private readonly Dictionary<string, Func<IDynamicScreen>> _screenFactories = new();

        public WpfUserInteractionService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
             //_screenFactories["markdown"] = () => new MarkdownViewer();
        }

        private IDynamicScreen? CreateScreenById(string screenId)
        {
            if (_screenFactories.TryGetValue(screenId, out var factory))
                return factory();

            var screenType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    typeof(IDynamicScreen).IsAssignableFrom(t) &&
                    typeof(UserControl).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    t.GetProperty("ScreenId") != null &&
                    t.GetConstructor(Type.EmptyTypes) != null &&
                    (string)(t.GetProperty("ScreenId")?.GetValue(Activator.CreateInstance(t))) == screenId
                );

            if (screenType != null)
                return (IDynamicScreen)Activator.CreateInstance(screenType);

            return null;
        }

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var screen = CreateScreenById(screenId);
                    if (screen != null)
                    {
                        screen.SetScreenInformation(context);

                        UIElement content = (UIElement)screen;
                        if (actions != null && actions.Count > 0)
                        {
                            var stackPanel = new StackPanel { Margin = new Thickness(10) };
                            stackPanel.Children.Add(content);

                            foreach (var action in actions)
                            {
                                var button = new Button
                                {
                                    Content = action.Key,
                                    Margin = new Thickness(0, 5, 0, 5),
                                    Background = new SolidColorBrush(Color.FromRgb(22, 22, 22)),
                                    Foreground = new SolidColorBrush(Color.FromRgb(244, 244, 244)),
                                    FontFamily = new FontFamily("Segoe UI"),
                                    FontSize = 14,
                                    Padding = new Thickness(10, 5, 10, 5)
                                };
                                button.Click += (s, e) => action.Value?.Invoke(context);
                                stackPanel.Children.Add(button);
                            }
                            // screenId+title ile benzersiz sekme aç
                            _mainWindow.AddOrUpdateTab($"{screenId}_{title}", title, stackPanel);
                        }
                        else
                        {
                            _mainWindow.AddOrUpdateTab($"{screenId}_{title}", title, content);
                        }
                        return;
                    }

                    var fallback = new TextBlock { Text = $"Ekran bulunamadı: {screenId}" };
                    _mainWindow.AddOrUpdateTab($"{screenId}_{title}", title, fallback);
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
                    ConfirmationDialog dialog = new ConfirmationDialog(title, message)
                    {
                        Owner = _mainWindow, // Ana pencereyi owner olarak ayarla
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Topmost = true // (İsteğe bağlı) Her zaman en üstte olsun
                    };
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