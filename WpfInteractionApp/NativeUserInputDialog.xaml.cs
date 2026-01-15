using Contextualizer.PluginContracts;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class NativeUserInputDialog : Window
    {
        private readonly UserInputRequest _request;
        public string UserInput { get; private set; } = string.Empty;
        public List<string> SelectedValues { get; private set; } = new List<string>();
        public NavigationResult NavigationResult { get; private set; } = new NavigationResult { Action = NavigationAction.Cancel };
        
        public bool CanGoBack { get; set; } = false;
        public int CurrentStep { get; set; } = 0;
        public int TotalSteps { get; set; } = 0;

        public NativeUserInputDialog(UserInputRequest request, Dictionary<string, string>? context = null, 
            bool canGoBack = false, int currentStep = 0, int totalSteps = 0)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));
            
            CanGoBack = canGoBack;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            
            TitleText.Text = request.Title ?? "Input Required";
            MessageText.Text = request.Message ?? "";
            
            BackButton.Visibility = canGoBack ? Visibility.Visible : Visibility.Collapsed;
            
            ConfigureInputControls(context);
            SetupProgressIndicator();
            
            Loaded += (s, e) => 
            {
                // Best-effort activation. Windows may block focus-stealing, but this makes the dialog
                // visible when the app is in the background and provides a taskbar flash fallback.
                WindowActivationHelper.BringToFrontBestEffort(this);
                Activate();
                Focus();
                SetInitialFocus();
            };
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragMove();
        }

        private void ConfigureInputControls(Dictionary<string, string>? context)
        {
            RequiredIndicator.Visibility = _request.IsRequired ? Visibility.Visible : Visibility.Collapsed;

            string defaultValue = _request.DefaultValue ?? "";
            if (context?.TryGetValue(_request.Key, out var contextValue) == true)
                defaultValue = contextValue;

            // Hide all controls first
            InputTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Collapsed;
            SelectionComboBox.Visibility = Visibility.Collapsed;
            MultiSelectListBox.Visibility = Visibility.Collapsed;
            FilePickerGrid.Visibility = Visibility.Collapsed;

            if (_request.IsFilePicker)
            {
                FilePickerGrid.Visibility = Visibility.Visible;
                if (!string.IsNullOrEmpty(defaultValue))
                    FilePathTextBox.Text = defaultValue;
            }
            else if (_request.IsSelectionList && _request.SelectionItems != null)
            {
                if (_request.IsMultiSelect)
                {
                    MultiSelectListBox.Visibility = Visibility.Visible;
                    foreach (var item in _request.SelectionItems)
                    {
                        var lbi = new ListBoxItem { Content = item.Display, Tag = item.Value };
                        try { lbi.Style = (Style)FindResource("Carbon.ListBoxItem"); } catch { /* ignore */ }
                        MultiSelectListBox.Items.Add(lbi);
                    }

                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        var defaultValues = defaultValue.Split(',');
                        foreach (var value in defaultValues)
                        {
                            var item = MultiSelectListBox.Items.Cast<ListBoxItem>()
                                .FirstOrDefault(i => i.Tag?.ToString() == value.Trim());
                            if (item != null) item.IsSelected = true;
                        }
                    }
                }
                else
                {
                    SelectionComboBox.Visibility = Visibility.Visible;
                    foreach (var item in _request.SelectionItems)
                    {
                        var cbi = new ComboBoxItem { Content = item.Display, Tag = item.Value };
                        try { cbi.Style = (Style)FindResource("Carbon.ComboBoxItem"); } catch { /* ignore */ }
                        SelectionComboBox.Items.Add(cbi);
                    }

                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        var idx = _request.SelectionItems.FindIndex(i => i.Value == defaultValue);
                        if (idx >= 0) SelectionComboBox.SelectedIndex = idx;
                    }
                }
            }
            else if (_request.IsPassword)
            {
                PasswordBox.Visibility = Visibility.Visible;
            }
            else
            {
                InputTextBox.Visibility = Visibility.Visible;
                InputTextBox.Text = defaultValue;
                
                if (_request.IsMultiLine)
                {
                    InputTextBox.AcceptsReturn = true;
                    InputTextBox.TextWrapping = TextWrapping.Wrap;
                    InputTextBox.Height = 100;
                }

                InputTextBox.TextChanged += (s, e) => ValidateInput();
            }
        }

        private void SetupProgressIndicator()
        {
            if (TotalSteps > 1)
            {
                ProgressPanel.Visibility = Visibility.Visible;
                CurrentStepText.Text = (CurrentStep + 1).ToString();
                TotalStepsText.Text = TotalSteps.ToString();
                ProgressBar.Width = 60 * ((double)(CurrentStep + 1) / TotalSteps);
            }
        }

        private void ValidateInput()
        {
            string input = InputTextBox.Text;
            bool isValid = true;
            string errorMessage = "";

            if (_request.IsRequired && string.IsNullOrWhiteSpace(input))
            {
                isValid = false;
                errorMessage = "This field is required";
            }
            else if (!string.IsNullOrEmpty(_request.ValidationRegex) && !string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    if (!Regex.IsMatch(input, _request.ValidationRegex))
                    {
                        isValid = false;
                        errorMessage = "Input format is invalid";
                    }
                }
                catch { isValid = false; errorMessage = "Invalid validation pattern"; }
            }

            if (!isValid)
            {
                ValidationErrorText.Text = errorMessage;
                ValidationPatternText.Text = !string.IsNullOrEmpty(_request.ValidationRegex) 
                    ? $"Expected format: {_request.ValidationRegex}" : "";
                ValidationPatternText.Visibility = !string.IsNullOrEmpty(_request.ValidationRegex) 
                    ? Visibility.Visible : Visibility.Collapsed;
                ValidationBorder.Visibility = Visibility.Visible;
            }
            else
            {
                ValidationBorder.Visibility = Visibility.Collapsed;
            }

            OkButton.IsEnabled = isValid || !_request.IsRequired;
        }

        private void SetInitialFocus()
        {
            if (_request.IsFilePicker) BrowseButton.Focus();
            else if (_request.IsSelectionList)
            {
                if (_request.IsMultiSelect) MultiSelectListBox.Focus();
                else SelectionComboBox.Focus();
            }
            else if (_request.IsPassword) PasswordBox.Focus();
            else { InputTextBox.Focus(); InputTextBox.SelectAll(); }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select File",
                Filter = GetFileFilter()
            };

            if (dialog.ShowDialog() == true)
                FilePathTextBox.Text = dialog.FileName;
        }

        private static string GetFileFilter()
        {
            // UserInputRequest doesn't have FileExtensions property - use default filter
            return "All Files|*.*";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string input;
            
            if (_request.IsFilePicker)
            {
                input = FilePathTextBox.Text;
            }
            else if (_request.IsSelectionList)
            {
                if (_request.IsMultiSelect)
                {
                    SelectedValues = MultiSelectListBox.SelectedItems.Cast<ListBoxItem>()
                        .Select(item => item.Tag?.ToString() ?? "").ToList();
                    input = string.Join(",", SelectedValues);
                }
                else
                {
                    var selectedItem = SelectionComboBox.SelectedItem as ComboBoxItem;
                    input = selectedItem?.Tag?.ToString() ?? "";
                }
            }
            else if (_request.IsPassword)
            {
                input = PasswordBox.Password;
            }
            else
            {
                input = InputTextBox.Text?.Trim() ?? "";
            }

            if (_request.IsRequired && string.IsNullOrWhiteSpace(input))
            {
                ValidationErrorText.Text = "Input is required";
                ValidationBorder.Visibility = Visibility.Visible;
                SetInitialFocus();
                return;
            }

            if (!string.IsNullOrEmpty(_request.ValidationRegex) && !string.IsNullOrEmpty(input))
            {
                try
                {
                    if (!Regex.IsMatch(input, _request.ValidationRegex))
                    {
                        ValidationErrorText.Text = "Invalid input format";
                        ValidationPatternText.Text = $"Expected format: {_request.ValidationRegex}";
                        ValidationBorder.Visibility = Visibility.Visible;
                        SetInitialFocus();
                        return;
                    }
                }
                catch { /* ignore invalid regex */ }
            }

            UserInput = input;
            NavigationResult = new NavigationResult
            {
                Action = NavigationAction.Next,
                Value = input,
                SelectedValues = SelectedValues
            };
            DialogResult = true;
            Close();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationResult = new NavigationResult { Action = NavigationAction.Back };
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationResult = new NavigationResult { Action = NavigationAction.Cancel };
            DialogResult = false;
            Close();
        }

        public NavigationResult ShowNavigationDialog()
        {
            ShowDialog();
            return NavigationResult;
        }
    }
}

