using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using Contextualizer.Core;

namespace WpfInteractionApp
{
    public partial class JsonFormatterView : UserControl, IDynamicScreen
    {
        private string _lastJson = null;
        private bool _showingFormatted = false;

        public JsonFormatterView()
        {
            InitializeComponent();
        }
        public string ScreenId => "jsonformatter";

        public void SetScreenInformation(Dictionary<string, string> context)
        {
            JsonTree.Items.Clear();
            FormattedJsonBoxScroll.Visibility = Visibility.Collapsed;
            JsonTreeScroll.Visibility = Visibility.Visible;
            ToggleViewButton.Content = "Formatlı Göster";
            _showingFormatted = false;

            if (context == null || !context.TryGetValue(ContextKey._body, out var json) || string.IsNullOrWhiteSpace(json))
            {
                JsonTree.Items.Add(new TreeViewItem { Header = "No JSON content." });
                _lastJson = null;
                return;
            }

            _lastJson = json;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = CreateTreeViewItem(doc.RootElement, "root");
                JsonTree.Items.Add(root);
                root.IsExpanded = true;
            }
            catch (Exception ex)
            {
                JsonTree.Items.Add(new TreeViewItem { Header = $"Invalid JSON: {ex.Message}", Foreground = Brushes.Red });
            }
        }

        private TreeViewItem CreateTreeViewItem(JsonElement element, string name)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var objItem = new TreeViewItem { Header = name };
                    foreach (var prop in element.EnumerateObject())
                        objItem.Items.Add(CreateTreeViewItem(prop.Value, prop.Name));
                    return objItem;
                case JsonValueKind.Array:
                    var arrItem = new TreeViewItem { Header = $"{name} [ ]" };
                    int idx = 0;
                    foreach (var item in element.EnumerateArray())
                        arrItem.Items.Add(CreateTreeViewItem(item, $"[{idx++}]"));
                    return arrItem;
                case JsonValueKind.String:
                    return new TreeViewItem { Header = $"{name}: \"{element.GetString()}\"" };
                case JsonValueKind.Number:
                    return new TreeViewItem { Header = $"{name}: {element.GetRawText()}" };
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return new TreeViewItem { Header = $"{name}: {element.GetRawText()}" };
                case JsonValueKind.Null:
                    return new TreeViewItem { Header = $"{name}: null" };
                default:
                    return new TreeViewItem { Header = $"{name}: ?" };
            }
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showingFormatted)
            {
                FormattedJsonBoxScroll.Visibility = Visibility.Collapsed;
                JsonTreeScroll.Visibility = Visibility.Visible;
                ToggleViewButton.Content = "Formatlı Göster";
                _showingFormatted = false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_lastJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(_lastJson);
                        var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                        FormattedJsonBox.Text = formatted;
                    }
                    catch (Exception ex)
                    {
                        FormattedJsonBox.Text = $"Invalid JSON: {ex.Message}";
                    }
                }
                else
                {
                    FormattedJsonBox.Text = "No JSON content.";
                }
                FormattedJsonBoxScroll.Visibility = Visibility.Visible;
                JsonTreeScroll.Visibility = Visibility.Collapsed;
                ToggleViewButton.Content = "Ağaç Göster";
                _showingFormatted = true;
            }
        }
    }
}