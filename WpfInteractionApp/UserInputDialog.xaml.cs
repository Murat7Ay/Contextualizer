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

        public UserInputDialog(UserInputRequest request)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));
            SelectedValues = new List<string>();
            
            // Set window title and message
            Title = request.Title;
            MessageText.Text = request.Message;

            // Configure input controls based on request type
            ConfigureInputControls();

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

        private void ConfigureInputControls()
        {
            // Show required field indicator if needed
            RequiredIndicator.Visibility = _request.IsRequired ? Visibility.Visible : Visibility.Collapsed;

            // Show validation pattern if provided
            if (!string.IsNullOrEmpty(_request.ValidationRegex))
            {
                ValidationPatternText.Text = $"Format: {_request.ValidationRegex}";
                ValidationPatternText.Visibility = Visibility.Visible;
            }

            // Configure input control based on request type
            if (_request.IsFilePicker)
            {
                FilePickerGrid.Visibility = Visibility.Visible;
                InputTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Collapsed;
                SelectionComboBox.Visibility = Visibility.Collapsed;
                MultiSelectListBox.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(_request.DefaultValue))
                {
                    FilePathTextBox.Text = _request.DefaultValue;
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
                    if (!string.IsNullOrEmpty(_request.DefaultValue))
                    {
                        var defaultValues = _request.DefaultValue.Split(',');
                        foreach (var defaultValue in defaultValues)
                        {
                            var item = MultiSelectListBox.Items.Cast<ListBoxItem>()
                                .FirstOrDefault(i => i.Tag.ToString() == defaultValue.Trim());
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

                    if (!string.IsNullOrEmpty(_request.DefaultValue))
                    {
                        var defaultItem = _request.SelectionItems.Find(i => i.Value == _request.DefaultValue);
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

                if (!string.IsNullOrEmpty(_request.DefaultValue))
                {
                    InputTextBox.Text = _request.DefaultValue;
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
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}