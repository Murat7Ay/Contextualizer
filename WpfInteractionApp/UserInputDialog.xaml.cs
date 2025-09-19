using Contextualizer.PluginContracts;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;

namespace WpfInteractionApp
{
    public partial class UserInputDialog : Window
    {
        private readonly UserInputRequest _request;
        public string UserInput { get; private set; }
        public List<string> SelectedValues { get; private set; }
        public NavigationResult NavigationResult { get; private set; }
        
        // Navigation properties
        public bool CanGoBack { get; set; } = false;
        public int CurrentStep { get; set; } = 0;
        public int TotalSteps { get; set; } = 0;

        public UserInputDialog(UserInputRequest request) : this(request, null, false, 0, 0)
        {
        }

        public UserInputDialog(UserInputRequest request, Dictionary<string, string> context, bool canGoBack = false, int currentStep = 0, int totalSteps = 0)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));
            SelectedValues = new List<string>();
            
            // Set navigation properties
            CanGoBack = canGoBack;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            
            // Set window title and message
            Title = request.Title;
            MessageText.Text = request.Message;
            
            // Progress indicator is handled by SetupProgressIndicator() method
            
            // Show back button if navigation is possible
            BackButton.Visibility = canGoBack ? Visibility.Visible : Visibility.Collapsed;

            // Configure input controls with context-based defaults
            ConfigureInputControls(context);

            // Set owner window
            if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Set initial focus to appropriate input control
            Loaded += (s, e) => SetInitialFocus();
        }

        private void ConfigureInputControls(Dictionary<string, string> context = null)
        {
            // Show required field indicator if needed
            RequiredIndicator.Visibility = _request.IsRequired ? Visibility.Visible : Visibility.Collapsed;

            // Show validation pattern if provided
            if (!string.IsNullOrEmpty(_request.ValidationRegex))
            {
                ValidationPatternText.Text = $"Format: {_request.ValidationRegex}";
                ValidationPatternText.Visibility = Visibility.Visible;
            }

            // Get default value - prioritize context over request default
            string defaultValue = _request.DefaultValue;
            if (context?.TryGetValue(_request.Key, out var contextValue) == true)
            {
                defaultValue = contextValue;
            }

            // Configure input control based on request type
            if (_request.IsFilePicker)
            {
                FilePickerGrid.Visibility = Visibility.Visible;
                InputTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Collapsed;
                SelectionComboBox.Visibility = Visibility.Collapsed;
                MultiSelectListBox.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    FilePathTextBox.Text = defaultValue;
                }
            }
            else if (_request.IsSelectionList && _request.SelectionItems != null)
            {
                if (_request.IsMultiSelect)
                {
                    MultiSelectListBox.Visibility = Visibility.Visible;
                    InputTextBox.Visibility = Visibility.Collapsed;
                    PasswordBox.Visibility = Visibility.Collapsed;
                    SelectionComboBox.Visibility = Visibility.Collapsed;
                    FilePickerGrid.Visibility = Visibility.Collapsed;

                    foreach (var item in _request.SelectionItems)
                    {
                        MultiSelectListBox.Items.Add(new ListBoxItem 
                        { 
                            Content = item.Display, 
                            Tag = item.Value 
                        });
                    }

                    // Set default values if provided
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        var defaultValues = defaultValue.Split(',');
                        foreach (var value in defaultValues)
                        {
                            var item = MultiSelectListBox.Items.Cast<ListBoxItem>()
                                .FirstOrDefault(i => i.Tag.ToString() == value.Trim());
                            if (item != null)
                            {
                                item.IsSelected = true;
                            }
                        }
                    }
                }
                else
                {
                    SelectionComboBox.Visibility = Visibility.Visible;
                    InputTextBox.Visibility = Visibility.Collapsed;
                    PasswordBox.Visibility = Visibility.Collapsed;
                    MultiSelectListBox.Visibility = Visibility.Collapsed;
                    FilePickerGrid.Visibility = Visibility.Collapsed;

                    foreach (var item in _request.SelectionItems)
                    {
                        SelectionComboBox.Items.Add(new ComboBoxItem 
                        { 
                            Content = item.Display, 
                            Tag = item.Value 
                        });
                    }

                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        var defaultItem = _request.SelectionItems.Find(i => i.Value == defaultValue);
                        if (defaultItem != null)
                        {
                            SelectionComboBox.SelectedIndex = _request.SelectionItems.IndexOf(defaultItem);
                        }
                    }
                }
            }
            else if (_request.IsPassword)
            {
                PasswordBox.Visibility = Visibility.Visible;
                InputTextBox.Visibility = Visibility.Collapsed;
                SelectionComboBox.Visibility = Visibility.Collapsed;
                MultiSelectListBox.Visibility = Visibility.Collapsed;
                FilePickerGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                InputTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                SelectionComboBox.Visibility = Visibility.Collapsed;
                MultiSelectListBox.Visibility = Visibility.Collapsed;
                FilePickerGrid.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    InputTextBox.Text = defaultValue;
                }

                // Configure multiline settings
                if (_request.IsMultiLine)
                {
                    InputTextBox.AcceptsReturn = true;
                    InputTextBox.TextWrapping = TextWrapping.Wrap;
                    InputTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    InputTextBox.Height = 100; // Set a default height for multiline
                }
                else
                {
                    InputTextBox.AcceptsReturn = false;
                    InputTextBox.TextWrapping = TextWrapping.NoWrap;
                    InputTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    InputTextBox.Height = double.NaN; // Reset to default height
                }
            }
            
            // ✨ Setup progress and validation
            SetupProgressIndicator();
            SetupValidation();
        }

        // ✨ Progress Indicator Setup
        private void SetupProgressIndicator()
        {
            if (TotalSteps > 1)
            {
                CurrentStepText.Text = (CurrentStep + 1).ToString();
                TotalStepsText.Text = TotalSteps.ToString();
                
                // Calculate progress bar width (60px total width)
                double progressPercentage = (double)(CurrentStep + 1) / TotalSteps;
                ProgressBar.Width = 60 * progressPercentage;
            }
            else
            {
                // Hide progress for single step
                CurrentStepText.Visibility = Visibility.Collapsed;
                TotalStepsText.Visibility = Visibility.Collapsed;
                ProgressBar.Parent.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            }
        }

        // ✨ Validation Setup
        private void SetupValidation()
        {
            if (InputTextBox.Visibility == Visibility.Visible)
            {
                InputTextBox.TextChanged += InputTextBox_TextChanged;
            }
        }

        // ✨ Real-time Validation
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateInput();
        }

        private void ValidateInput()
        {
            string input = InputTextBox.Text;
            bool isValid = true;
            string errorMessage = "";

            // Required field validation
            if (_request.IsRequired && string.IsNullOrWhiteSpace(input))
            {
                isValid = false;
                errorMessage = "This field is required";
            }
            // Pattern validation
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
                catch (Exception)
                {
                    // Invalid regex pattern
                    isValid = false;
                    errorMessage = "Invalid validation pattern";
                }
            }

            // Update UI based on validation result
            if (isValid && !string.IsNullOrWhiteSpace(input))
            {
                ShowSuccessFeedback();
            }
            else if (!isValid)
            {
                ShowValidationError(errorMessage);
            }
            else
            {
                HideAllFeedback();
            }

            // Enable/disable OK button
            OkButton.IsEnabled = isValid || !_request.IsRequired;
        }

        private void ShowValidationError(string message)
        {
            ValidationErrorText.Text = message;
            if (!string.IsNullOrEmpty(_request.ValidationRegex))
            {
                ValidationPatternText.Text = $"Expected format: {_request.ValidationRegex}";
                ValidationPatternText.Visibility = Visibility.Visible;
            }
            else
            {
                ValidationPatternText.Visibility = Visibility.Collapsed;
            }
            
            ValidationBorder.Visibility = Visibility.Visible;
            SuccessBorder.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccessFeedback()
        {
            SuccessBorder.Visibility = Visibility.Visible;
            ValidationBorder.Visibility = Visibility.Collapsed;
        }

        private void HideAllFeedback()
        {
            ValidationBorder.Visibility = Visibility.Collapsed;
            SuccessBorder.Visibility = Visibility.Collapsed;
        }

        private void SetInitialFocus()
        {
            if (_request.IsFilePicker)
                BrowseButton.Focus();
            else if (_request.IsSelectionList)
            {
                if (_request.IsMultiSelect)
                    MultiSelectListBox.Focus();
                else
                    SelectionComboBox.Focus();
            }
            else if (_request.IsPassword)
                PasswordBox.Focus();
            else
                InputTextBox.Focus();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select File",
                Filter = "All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
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
                    SelectedValues = MultiSelectListBox.SelectedItems
                        .Cast<ListBoxItem>()
                        .Select(item => item.Tag.ToString())
                        .ToList();
                    input = string.Join(",", SelectedValues);
                }
                else
                {
                    var selectedItem = SelectionComboBox.SelectedItem as ComboBoxItem;
                    input = selectedItem?.Tag?.ToString() ?? string.Empty;
                }
            }
            else if (_request.IsPassword)
            {
                input = PasswordBox.Password;
            }
            else
            {
                input = InputTextBox.Text?.Trim();
            }

            if (_request.IsRequired && string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Input is required. Please enter a value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetInitialFocus();
                return;
            }

            if (!string.IsNullOrEmpty(_request.ValidationRegex) && !string.IsNullOrEmpty(input))
            {
                var regex = new Regex(_request.ValidationRegex);
                if (!regex.IsMatch(input))
                {
                    MessageBox.Show("Invalid input format. Please follow the expected format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetInitialFocus();
                    return;
                }
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
            NavigationResult = new NavigationResult
            {
                Action = NavigationAction.Back
            };
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationResult = new NavigationResult
            {
                Action = NavigationAction.Cancel
            };
            DialogResult = false;
            Close();
        }

        public NavigationResult ShowNavigationDialog()
        {
            ShowDialog();
            return NavigationResult ?? new NavigationResult { Action = NavigationAction.Cancel };
        }
    }
}