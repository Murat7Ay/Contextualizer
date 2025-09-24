using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfInteractionApp.Settings;
using WpfInteractionApp.Services;
using Contextualizer.Core;

namespace WpfInteractionApp
{
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private readonly AppSettings _settings;
        private bool _isCtrlChecked;
        private bool _isAltChecked;
        private bool _isShiftChecked;
        private bool _isWinChecked;
        private string _key;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string HandlersFilePath
        {
            get => _settings.HandlersFilePath;
            set
            {
                _settings.HandlersFilePath = value;
                OnPropertyChanged(nameof(HandlersFilePath));
            }
        }

        public string PluginsDirectory
        {
            get => _settings.PluginsDirectory;
            set
            {
                _settings.PluginsDirectory = value;
                OnPropertyChanged(nameof(PluginsDirectory));
            }
        }

        public string ExchangeDirectory
        {
            get => _settings.ExchangeDirectory;
            set
            {
                _settings.ExchangeDirectory = value;
                OnPropertyChanged(nameof(ExchangeDirectory));
            }
        }

        public bool IsCtrlChecked
        {
            get => _isCtrlChecked;
            set
            {
                _isCtrlChecked = value;
                _settings.KeyboardShortcut.SetModifier("Ctrl", value);
                OnPropertyChanged(nameof(IsCtrlChecked));
            }
        }

        public bool IsAltChecked
        {
            get => _isAltChecked;
            set
            {
                _isAltChecked = value;
                _settings.KeyboardShortcut.SetModifier("Alt", value);
                OnPropertyChanged(nameof(IsAltChecked));
            }
        }

        public bool IsShiftChecked
        {
            get => _isShiftChecked;
            set
            {
                _isShiftChecked = value;
                _settings.KeyboardShortcut.SetModifier("Shift", value);
                OnPropertyChanged(nameof(IsShiftChecked));
            }
        }

        public bool IsWinChecked
        {
            get => _isWinChecked;
            set
            {
                _isWinChecked = value;
                _settings.KeyboardShortcut.SetModifier("Win", value);
                OnPropertyChanged(nameof(IsWinChecked));
            }
        }

        public string Key
        {
            get => _key;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _key = "W";
                    _settings.KeyboardShortcut.Key = "W";
                }
                else
                {
                    // Sadece son karakteri al ve büyük harfe çevir
                    string lastChar = value.ToUpper().Last().ToString();
                    if (Regex.IsMatch(lastChar, "^[A-Z]$"))
                    {
                        _key = lastChar;
                        _settings.KeyboardShortcut.Key = lastChar;
                    }
                }
                OnPropertyChanged(nameof(Key));
            }
        }

        public ConfigSystemSettings ConfigSystem
        {
            get => _settings.ConfigSystem;
            set
            {
                _settings.ConfigSystem = value;
                OnPropertyChanged(nameof(ConfigSystem));
            }
        }

        public UISettings UISettings
        {
            get => _settings.UISettings;
            set
            {
                _settings.UISettings = value;
                OnPropertyChanged(nameof(UISettings));
            }
        }

        public int ClipboardWaitTimeout
        {
            get => _settings.ClipboardWaitTimeout;
            set
            {
                _settings.ClipboardWaitTimeout = value;
                OnPropertyChanged(nameof(ClipboardWaitTimeout));
            }
        }

        public int WindowActivationDelay
        {
            get => _settings.WindowActivationDelay;
            set
            {
                _settings.WindowActivationDelay = value;
                OnPropertyChanged(nameof(WindowActivationDelay));
            }
        }

        public int ClipboardClearDelay
        {
            get => _settings.ClipboardClearDelay;
            set
            {
                _settings.ClipboardClearDelay = value;
                OnPropertyChanged(nameof(ClipboardClearDelay));
            }
        }

        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;

            // Initialize modifier key checkboxes
            IsCtrlChecked = _settings.KeyboardShortcut.HasModifier("Ctrl");
            IsAltChecked = _settings.KeyboardShortcut.HasModifier("Alt");
            IsShiftChecked = _settings.KeyboardShortcut.HasModifier("Shift");
            IsWinChecked = _settings.KeyboardShortcut.HasModifier("Win");

            // Initialize key
            _key = _settings.KeyboardShortcut.Key;

            DataContext = this;

            // KeyTextBox'a PreviewKeyDown event handler'ı ekle
            KeyTextBox.PreviewKeyDown += KeyTextBox_PreviewKeyDown;

            // Restore window position
            RestoreWindowPosition();

            // Save window position when closing
            Closing += SettingsWindow_Closing;
        }

        private void KeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Sadece harflere ve rakamlara izin ver
            if (!((e.Key >= System.Windows.Input.Key.A && e.Key <= System.Windows.Input.Key.Z) || 
                  (e.Key >= System.Windows.Input.Key.D0 && e.Key <= System.Windows.Input.Key.D9) || 
                  (e.Key >= System.Windows.Input.Key.NumPad0 && e.Key <= System.Windows.Input.Key.NumPad9)))
            {
                e.Handled = true;
            }
        }

        private void BrowseHandlersFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(HandlersFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                HandlersFilePath = dialog.FileName;
            }
        }

        private void BrowsePluginsDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = PluginsDirectory
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PluginsDirectory = dialog.SelectedPath;
            }
        }

        private void BrowseExchangeDir_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = ExchangeDirectory
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ExchangeDirectory = dialog.SelectedPath;
            }
        }

        private void BrowseConfigFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Config File",
                Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*",
                FileName = Path.GetFileName(_settings.ConfigSystem.ConfigFilePath),
                InitialDirectory = Path.GetDirectoryName(_settings.ConfigSystem.ConfigFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                _settings.ConfigSystem.ConfigFilePath = dialog.FileName;
                OnPropertyChanged("ConfigSystem");
            }
        }

        private void BrowseSecretsFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Secrets File",
                Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*",
                FileName = Path.GetFileName(_settings.ConfigSystem.SecretsFilePath),
                InitialDirectory = Path.GetDirectoryName(_settings.ConfigSystem.SecretsFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                _settings.ConfigSystem.SecretsFilePath = dialog.FileName;
                OnPropertyChanged("ConfigSystem");
            }
        }

        private void BrowseNetworkUpdatePathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Network Update Path",
                SelectedPath = _settings.UISettings.NetworkUpdateSettings.NetworkUpdatePath
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _settings.UISettings.NetworkUpdateSettings.NetworkUpdatePath = dialog.SelectedPath;
                OnPropertyChanged("UISettings");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RestoreWindowPosition()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowPos = settingsService.Settings.WindowSettings.SettingsWindow;
                
                if (!double.IsNaN(windowPos.Left) && !double.IsNaN(windowPos.Top))
                {
                    Left = windowPos.Left;
                    Top = windowPos.Top;
                }
                
                if (!double.IsNaN(windowPos.Width) && windowPos.Width > 0)
                {
                    Width = windowPos.Width;
                }
                
                if (!double.IsNaN(windowPos.Height) && windowPos.Height > 0)
                {
                    Height = windowPos.Height;
                }
            }
            catch (InvalidOperationException)
            {
                // ServiceLocator not initialized or SettingsService not registered
            }
        }

        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowPosition();
        }

        private void SaveWindowPosition()
        {
            try
            {
                var settingsService = ServiceLocator.Get<SettingsService>();
                var windowPos = settingsService.Settings.WindowSettings.SettingsWindow;
                
                // Only update in memory, don't save to disk yet
                if (IsValidPosition(Left))
                    windowPos.Left = Left;
                if (IsValidPosition(Top))
                    windowPos.Top = Top;
                if (IsValidSize(Width))
                    windowPos.Width = Width;
                if (IsValidSize(Height))
                    windowPos.Height = Height;
                
                // Settings will be saved when application exits
            }
            catch (InvalidOperationException)
            {
                // ServiceLocator not initialized or SettingsService not registered
            }
        }

        private static bool IsValidPosition(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValidSize(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 