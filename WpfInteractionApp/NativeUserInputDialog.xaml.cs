using Contextualizer.PluginContracts;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
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
            DatePickerPanel.Visibility = Visibility.Collapsed;
            TimePickerPanel.Visibility = Visibility.Collapsed;
            DateTimePickerGrid.Visibility = Visibility.Collapsed;

            // Clear and populate time ComboBoxes
            TimeHourComboBox.Items.Clear();
            TimeMinuteComboBox.Items.Clear();
            DateTimeHourComboBox.Items.Clear();
            DateTimeMinuteComboBox.Items.Clear();

            // Populate hour ComboBoxes (0-23)
            Style? comboBoxItemStyle = null;
            try { comboBoxItemStyle = (Style)FindResource("Carbon.ComboBoxItem"); } catch { /* ignore */ }
            
            for (int h = 0; h < 24; h++)
            {
                var hourItem = new ComboBoxItem { Content = h.ToString("00"), Tag = h };
                if (comboBoxItemStyle != null) hourItem.Style = comboBoxItemStyle;
                TimeHourComboBox.Items.Add(hourItem);
                
                var dateTimeHourItem = new ComboBoxItem { Content = h.ToString("00"), Tag = h };
                if (comboBoxItemStyle != null) dateTimeHourItem.Style = comboBoxItemStyle;
                DateTimeHourComboBox.Items.Add(dateTimeHourItem);
            }

            // Populate minute ComboBoxes (0-59)
            for (int m = 0; m < 60; m++)
            {
                var minuteItem = new ComboBoxItem { Content = m.ToString("00"), Tag = m };
                if (comboBoxItemStyle != null) minuteItem.Style = comboBoxItemStyle;
                TimeMinuteComboBox.Items.Add(minuteItem);
                
                var dateTimeMinuteItem = new ComboBoxItem { Content = m.ToString("00"), Tag = m };
                if (comboBoxItemStyle != null) dateTimeMinuteItem.Style = comboBoxItemStyle;
                DateTimeMinuteComboBox.Items.Add(dateTimeMinuteItem);
            }

            var isDateTime = _request.IsDateTime || _request.IsDateTimePicker;
            var isDate = _request.IsDate || _request.IsDatePicker;
            var isTime = _request.IsTime || _request.IsTimePicker;

            if (_request.IsFolderPicker || _request.IsFilePicker)
            {
                FilePickerGrid.Visibility = Visibility.Visible;
                BrowseButton.Content = _request.IsFolderPicker ? "Browse Folder" : "Browse";
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
            else if (isDateTime)
            {
                DateTimePickerGrid.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(defaultValue) && DateTime.TryParse(defaultValue, out var dt))
                {
                    DateTimeDatePicker.SelectedDate = dt.Date;
                    // Set hour and minute ComboBoxes
                    var hourItem = DateTimeHourComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == dt.Hour);
                    if (hourItem != null) DateTimeHourComboBox.SelectedItem = hourItem;
                    var minuteItem = DateTimeMinuteComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == dt.Minute);
                    if (minuteItem != null) DateTimeMinuteComboBox.SelectedItem = minuteItem;
                }
                else
                {
                    // Default to current time if no default value
                    var now = DateTime.Now;
                    var hourItem = DateTimeHourComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == now.Hour);
                    if (hourItem != null) DateTimeHourComboBox.SelectedItem = hourItem;
                    var minuteItem = DateTimeMinuteComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == now.Minute);
                    if (minuteItem != null) DateTimeMinuteComboBox.SelectedItem = minuteItem;
                }
                DateTimeDatePicker.SelectedDateChanged += (s, e) => ValidateInput();
                DateTimeHourComboBox.SelectionChanged += (s, e) => ValidateInput();
                DateTimeMinuteComboBox.SelectionChanged += (s, e) => ValidateInput();
            }
            else if (isDate)
            {
                DatePickerPanel.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(defaultValue) && DateTime.TryParse(defaultValue, out var d))
                {
                    DatePicker.SelectedDate = d.Date;
                }
                DatePicker.SelectedDateChanged += (s, e) => ValidateInput();
            }
            else if (isTime)
            {
                TimePickerPanel.Visibility = Visibility.Visible;
                if (!string.IsNullOrWhiteSpace(defaultValue) && TryParseTime(defaultValue, out var time))
                {
                    // Set hour and minute ComboBoxes from parsed time
                    var hourItem = TimeHourComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == time.Hours);
                    if (hourItem != null) TimeHourComboBox.SelectedItem = hourItem;
                    var minuteItem = TimeMinuteComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == time.Minutes);
                    if (minuteItem != null) TimeMinuteComboBox.SelectedItem = minuteItem;
                }
                else
                {
                    // Default to current time if no valid default value
                    var now = DateTime.Now;
                    var hourItem = TimeHourComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == now.Hour);
                    if (hourItem != null) TimeHourComboBox.SelectedItem = hourItem;
                    var minuteItem = TimeMinuteComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (int)i.Tag == now.Minute);
                    if (minuteItem != null) TimeMinuteComboBox.SelectedItem = minuteItem;
                }
                TimeHourComboBox.SelectionChanged += (s, e) => ValidateInput();
                TimeMinuteComboBox.SelectionChanged += (s, e) => ValidateInput();
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

            var isDateTime = _request.IsDateTime || _request.IsDateTimePicker;
            var isDate = _request.IsDate || _request.IsDatePicker;
            var isTime = _request.IsTime || _request.IsTimePicker;

            if (isDateTime)
            {
                if (_request.IsRequired && DateTimeDatePicker.SelectedDate == null)
                {
                    isValid = false;
                    errorMessage = "Date is required";
                }
                else if (_request.IsRequired && (DateTimeHourComboBox.SelectedItem == null || DateTimeMinuteComboBox.SelectedItem == null))
                {
                    isValid = false;
                    errorMessage = "Time is required";
                }
            }
            else if (isDate)
            {
                if (_request.IsRequired && DatePicker.SelectedDate == null)
                {
                    isValid = false;
                    errorMessage = "Date is required";
                }
            }
            else if (isTime)
            {
                if (_request.IsRequired && (TimeHourComboBox.SelectedItem == null || TimeMinuteComboBox.SelectedItem == null))
                {
                    isValid = false;
                    errorMessage = "Time is required";
                }
            }
            else if (_request.IsSelectionList)
            {
                if (_request.IsMultiSelect)
                {
                    if (_request.IsRequired && MultiSelectListBox.SelectedItems.Count == 0)
                    {
                        isValid = false;
                        errorMessage = "Please select at least one item";
                    }
                }
                else
                {
                    if (_request.IsRequired && SelectionComboBox.SelectedItem == null)
                    {
                        isValid = false;
                        errorMessage = "Please select an item";
                    }
                }
            }
            else
            {
                if (_request.IsFilePicker || _request.IsFolderPicker)
                    input = FilePathTextBox.Text;
                else if (_request.IsPassword)
                    input = PasswordBox.Password;

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
            var isDateTime = _request.IsDateTime || _request.IsDateTimePicker;
            var isDate = _request.IsDate || _request.IsDatePicker;
            var isTime = _request.IsTime || _request.IsTimePicker;

            if (_request.IsFilePicker || _request.IsFolderPicker) BrowseButton.Focus();
            else if (_request.IsSelectionList)
            {
                if (_request.IsMultiSelect) MultiSelectListBox.Focus();
                else SelectionComboBox.Focus();
            }
            else if (isDateTime)
            {
                DateTimeDatePicker.Focus();
            }
            else if (isDate)
            {
                DatePicker.Focus();
            }
            else if (isTime)
            {
                TimeHourComboBox.Focus();
            }
            else if (_request.IsPassword) PasswordBox.Focus();
            else { InputTextBox.Focus(); InputTextBox.SelectAll(); }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_request.IsFolderPicker)
            {
                using var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Select Folder",
                    ShowNewFolderButton = true,
                    SelectedPath = FilePathTextBox.Text
                };

                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    FilePathTextBox.Text = folderDialog.SelectedPath;
                return;
            }

            var fileDialog = new OpenFileDialog
            {
                Title = "Select File",
                Filter = GetFileFilter()
            };

            if (fileDialog.ShowDialog() == true)
                FilePathTextBox.Text = fileDialog.FileName;
        }

        private string GetFileFilter()
        {
            var extensions = _request.FileExtensions;
            if (extensions == null || extensions.Count == 0)
                return "All Files|*.*";

            var patterns = new List<string>();
            foreach (var ext in extensions)
            {
                if (string.IsNullOrWhiteSpace(ext)) continue;
                var e = ext.Trim();
                if (e == "*" || e == "*.*") continue;
                if (e.StartsWith("*.")) { patterns.Add(e); continue; }
                if (e.StartsWith(".")) { patterns.Add($"*{e}"); continue; }
                if (e.Contains("*")) { patterns.Add(e); continue; }
                patterns.Add($"*.{e}");
            }

            if (patterns.Count == 0)
                return "All Files|*.*";

            var label = $"Allowed Files ({string.Join(", ", patterns.Select(p => p.Replace("*", "")))})";
            return $"{label}|{string.Join(";", patterns)}|All Files|*.*";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string input;
            
            var isDateTime = _request.IsDateTime || _request.IsDateTimePicker;
            var isDate = _request.IsDate || _request.IsDatePicker;
            var isTime = _request.IsTime || _request.IsTimePicker;

            if (_request.IsFolderPicker || _request.IsFilePicker)
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
            else if (isDateTime)
            {
                if (DateTimeDatePicker.SelectedDate == null)
                {
                    ValidationErrorText.Text = "Date is required";
                    ValidationBorder.Visibility = Visibility.Visible;
                    SetInitialFocus();
                    return;
                }

                if (DateTimeHourComboBox.SelectedItem == null || DateTimeMinuteComboBox.SelectedItem == null)
                {
                    ValidationErrorText.Text = "Time is required";
                    ValidationBorder.Visibility = Visibility.Visible;
                    SetInitialFocus();
                    return;
                }

                var hourItem = DateTimeHourComboBox.SelectedItem as ComboBoxItem;
                var minuteItem = DateTimeMinuteComboBox.SelectedItem as ComboBoxItem;
                var hour = hourItem?.Tag is int h ? h : 0;
                var minute = minuteItem?.Tag is int m ? m : 0;
                var date = DateTimeDatePicker.SelectedDate.Value.Date.AddHours(hour).AddMinutes(minute);
                input = date.ToString("yyyy-MM-ddTHH:mm");
            }
            else if (isDate)
            {
                if (DatePicker.SelectedDate == null)
                {
                    ValidationErrorText.Text = "Date is required";
                    ValidationBorder.Visibility = Visibility.Visible;
                    SetInitialFocus();
                    return;
                }

                input = DatePicker.SelectedDate.Value.ToString("yyyy-MM-dd");
            }
            else if (isTime)
            {
                if (TimeHourComboBox.SelectedItem == null || TimeMinuteComboBox.SelectedItem == null)
                {
                    ValidationErrorText.Text = "Time is required";
                    ValidationBorder.Visibility = Visibility.Visible;
                    SetInitialFocus();
                    return;
                }

                var hourItem = TimeHourComboBox.SelectedItem as ComboBoxItem;
                var minuteItem = TimeMinuteComboBox.SelectedItem as ComboBoxItem;
                var hour = hourItem?.Tag is int h ? h : 0;
                var minute = minuteItem?.Tag is int m ? m : 0;
                input = $"{hour:D2}:{minute:D2}";
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

        private static bool TryParseTime(string input, out TimeSpan time)
        {
            if (TimeSpan.TryParse(input, CultureInfo.CurrentCulture, out time))
                return true;
            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var dt))
            {
                time = dt.TimeOfDay;
                return true;
            }
            if (TimeSpan.TryParse(input, CultureInfo.InvariantCulture, out time))
                return true;
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.NoCurrentDateDefault, out dt))
            {
                time = dt.TimeOfDay;
                return true;
            }

            time = default;
            return false;
        }
    }
}

