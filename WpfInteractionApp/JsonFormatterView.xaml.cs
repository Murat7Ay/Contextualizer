using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace WpfInteractionApp
{
    public partial class JsonFormatterView : UserControl, IDynamicScreen
    {
        public JsonFormatterView()
        {
            InitializeComponent();
        }
        public string ScreenId => "jsonformatter";

        public void SetScreenInformation(Dictionary<string, string> context)
        {
            JsonTree.Items.Clear();
            if (context == null || !context.TryGetValue("body", out var json) || string.IsNullOrWhiteSpace(json))
            {
                JsonTree.Items.Add(new TreeViewItem { Header = "No JSON content." });
                return;
            }

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
    }
}