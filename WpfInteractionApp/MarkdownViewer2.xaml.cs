using Contextualizer.Core;
using Markdig;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public partial class MarkdownViewer2 : UserControl, IDynamicScreen
    {
        public string ScreenId => "markdown2";
        private Dictionary<string, string> Context { get; set; }

        public MarkdownViewer2()
        {
            InitializeComponent();
            Loaded += MarkdownViewer2_Loaded;
        }

        private async void MarkdownViewer2_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (WebView.CoreWebView2 == null)
            {
                await WebView.EnsureCoreWebView2Async();
            }
        }

        public async void SetScreenInformation(Dictionary<string, string> context)
        {
            Context = context;
            string markdown = context.TryGetValue(ContextKey._body, out var md) ? md : string.Empty;
            string html = ConvertMarkdownToHtml(markdown);
            await SetHtmlAsync(html);
        }

        private string ConvertMarkdownToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdig.Markdown.ToHtml(markdown ?? string.Empty, pipeline);
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            font-family: 'Segoe UI', 'Calibri', 'Arial', sans-serif;
            background: #FAFAFA;
            color: #222;
            margin: 0;
            padding: 24px;
            font-size: 15px;
            line-height: 1.7;
        }}
        h1, h2, h3, h4, h5, h6 {{ font-weight: 600; margin-top: 1.5em; }}
        pre, code {{ background: #f4f4f4; border-radius: 4px; padding: 2px 6px; font-family: 'Consolas', monospace; }}
        pre {{ padding: 12px; overflow-x: auto; }}
        blockquote {{ border-left: 3px solid #e0e0e0; margin: 1em 0; padding: 0.5em 1em; color: #666; background: #f9f9f9; }}
        table {{ border-collapse: collapse; }}
        th, td {{ border: 1px solid #e0e0e0; padding: 6px 12px; }}
        a {{ color: #0066cc; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
{html}
</body>
</html>";
        }

        private async Task SetHtmlAsync(string html)
        {
            if (WebView.CoreWebView2 == null)
                await WebView.EnsureCoreWebView2Async();
            WebView.NavigateToString(html);
        }
    }
}