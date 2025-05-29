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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 