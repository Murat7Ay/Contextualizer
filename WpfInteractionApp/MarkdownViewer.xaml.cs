using MdXaml;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WpfInteractionApp
{
    public partial class MarkdownViewer : UserControl
    {
        private readonly Markdown _markdownEngine;

        public MarkdownViewer()
        {
            InitializeComponent();
            _markdownEngine = new Markdown();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(MarkdownViewer),
                new PropertyMetadata(string.Empty, OnTextChanged));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var viewer = (MarkdownViewer)d;
            var markdown = e.NewValue as string;

            if (string.IsNullOrEmpty(markdown))
            {
                viewer.Viewer.Document = new FlowDocument(new Paragraph(new Run("Empty Markdown content")));
                return;
            }

            try
            {
                var flowDocument = viewer._markdownEngine.Transform(markdown);
                viewer.Viewer.Document = flowDocument;
            }
            catch (Exception ex)
            {
                viewer.Viewer.Document = new FlowDocument(new Paragraph(new Run($"Markdown process exception: {ex.Message}")));
            }
        }
    }
}