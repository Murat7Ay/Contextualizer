using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp
{
    public partial class XmlFormatterView : UserControl, IDynamicScreen
    {
        private string _lastXml = null;
        private bool _showingFormatted = false;

        public XmlFormatterView()
        {
            InitializeComponent();
        }
        public string ScreenId => "xmlformatter";

        public void SetScreenInformation(Dictionary<string, string> context)
        {
            XmlTree.Items.Clear();
            FormattedXmlBoxScroll.Visibility = Visibility.Collapsed;
            XmlTreeScroll.Visibility = Visibility.Visible;
            ToggleViewButton.Content = "Formatlı Göster";
            _showingFormatted = false;

            if (context == null || !context.TryGetValue(ContextKey._input, out var xml) || string.IsNullOrWhiteSpace(xml))
            {
                XmlTree.Items.Add(new TreeViewItem { Header = "No XML content." });
                _lastXml = null;
                return;
            }

            _lastXml = xml;

            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var root = CreateTreeViewItem(doc.DocumentElement, "root");
                XmlTree.Items.Add(root);
                root.IsExpanded = true;
            }
            catch (Exception ex)
            {
                XmlTree.Items.Add(new TreeViewItem { Header = $"Invalid XML: {ex.Message}", Foreground = Brushes.Red });
            }
        }

        private TreeViewItem CreateTreeViewItem(XmlNode node, string name)
        {
            if (node == null)
                return new TreeViewItem { Header = $"{name}: null" };

            var item = new TreeViewItem { Header = node.Name };

            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    item.Items.Add(new TreeViewItem { Header = $"@{attr.Name}: \"{attr.Value}\"" });
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    item.Items.Add(CreateTreeViewItem(child, child.Name));
                }
                else if (child.NodeType == XmlNodeType.Text)
                {
                    item.Items.Add(new TreeViewItem { Header = $"#text: \"{child.Value}\"" });
                }
                else if (child.NodeType == XmlNodeType.CDATA)
                {
                    item.Items.Add(new TreeViewItem { Header = $"#cdata: \"{child.Value}\"" });
                }
            }

            return item;
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showingFormatted)
            {
                FormattedXmlBoxScroll.Visibility = Visibility.Collapsed;
                XmlTreeScroll.Visibility = Visibility.Visible;
                ToggleViewButton.Content = "Formatlı Göster";
                _showingFormatted = false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_lastXml))
                {
                    try
                    {
                        var doc = new XmlDocument();
                        doc.LoadXml(_lastXml);
                        var stringWriter = new System.IO.StringWriter();
                        var xmlTextWriter = new XmlTextWriter(stringWriter) { Formatting = Formatting.Indented };
                        doc.WriteTo(xmlTextWriter);
                        FormattedXmlBox.Text = stringWriter.ToString();
                    }
                    catch (Exception ex)
                    {
                        FormattedXmlBox.Text = $"Invalid XML: {ex.Message}";
                    }
                }
                else
                {
                    FormattedXmlBox.Text = "No XML content.";
                }
                FormattedXmlBoxScroll.Visibility = Visibility.Visible;
                XmlTreeScroll.Visibility = Visibility.Collapsed;
                ToggleViewButton.Content = "Ağaç Göster";
                _showingFormatted = true;
            }
        }
    }
}