using Contextualizer.PluginContracts;
using Markdig;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public partial class MarkdownViewer2 : UserControl, IDynamicScreen
    {
        private readonly MarkdownPipeline _pipeline;
        private bool _isWebViewInitialized;

        public MarkdownViewer2()
        {
            InitializeComponent();
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseTaskLists()
                .UseEmojiAndSmiley()
                .Build();
            _isWebViewInitialized = false;
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;
                
                if (!string.IsNullOrEmpty(Text))
                {
                    UpdateMarkdownContent(Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMarkdownContent(string markdown)
        {
            if (!_isWebViewInitialized)
            {
                return;
            }

            if (string.IsNullOrEmpty(markdown))
            {
                WebView.NavigateToString("<p>No content available</p>");
                return;
            }

            try
            {
                var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
                var styledHtml = $@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            :root {{
                                color-scheme: light;
                            }}
                            
                            body {{ 
                                font-family: 'IBM Plex Sans', 'Segoe UI', system-ui, sans-serif;
                                line-height: 1.5;
                                color: #161616;
                                margin: 2rem;
                                background-color: #f4f4f4;
                                max-width: 960px;
                                margin: 2rem auto;
                                padding: 0 2rem;
                            }}
                            
                            h1, h2, h3, h4, h5, h6 {{
                                font-weight: 400;
                                color: #161616;
                                margin-top: 2rem;
                                margin-bottom: 1rem;
                                line-height: 1.25;
                            }}
                            
                            h1 {{ font-size: 2.625rem; }}
                            h2 {{ font-size: 2rem; }}
                            h3 {{ font-size: 1.75rem; }}
                            h4 {{ font-size: 1.25rem; }}
                            h5 {{ font-size: 1rem; }}
                            h6 {{ font-size: 0.875rem; }}
                            
                            p {{
                                margin: 1rem 0;
                                line-height: 1.5;
                            }}
                            
                            pre {{ 
                                background-color: #ffffff;
                                padding: 1rem;
                                border-radius: 4px;
                                border: 1px solid #e0e0e0;
                                overflow-x: auto;
                                margin: 1rem 0;
                            }}
                            
                            code {{
                                font-family: 'IBM Plex Mono', 'Consolas', monospace;
                                font-size: 0.875rem;
                                padding: 0.2rem 0.4rem;
                                background-color: #f4f4f4;
                                border-radius: 2px;
                                color: #161616;
                            }}
                            
                            pre code {{
                                background-color: transparent;
                                padding: 0;
                                border-radius: 0;
                                color: inherit;
                            }}
                            
                            blockquote {{
                                border-left: 4px solid #0f62fe;
                                margin: 1.5rem 0;
                                padding: 0.5rem 1rem;
                                background-color: #ffffff;
                                border-radius: 0 4px 4px 0;
                                color: #525252;
                            }}
                            
                            ul, ol {{
                                margin: 1rem 0;
                                padding-left: 2rem;
                            }}
                            
                            li {{
                                margin: 0.5rem 0;
                            }}
                            
                            a {{
                                color: #0f62fe;
                                text-decoration: none;
                            }}
                            
                            a:hover {{
                                text-decoration: underline;
                            }}
                            
                            hr {{
                                border: none;
                                border-top: 1px solid #e0e0e0;
                                margin: 2rem 0;
                            }}
                            
                            table {{
                                border-collapse: collapse;
                                width: 100%;
                                margin: 1.5rem 0;
                                background-color: #ffffff;
                                border: 1px solid #e0e0e0;
                            }}
                            
                            th, td {{
                                border: 1px solid #e0e0e0;
                                padding: 0.875rem 1rem;
                                text-align: left;
                            }}
                            
                            th {{
                                background-color: #f4f4f4;
                                font-weight: 600;
                                color: #161616;
                            }}
                            
                            tr:nth-child(even) {{
                                background-color: #fafafa;
                            }}
                            
                            img {{
                                max-width: 100%;
                                height: auto;
                                border-radius: 4px;
                                margin: 1rem 0;
                            }}

                            /* Carbon Design System inspired components */
                            .task-list-item {{
                                list-style-type: none;
                                margin-left: -2rem;
                            }}

                            .task-list-item input[type='checkbox'] {{
                                margin-right: 0.5rem;
                            }}

                            code[class*='language-'],
                            pre[class*='language-'] {{
                                font-family: 'IBM Plex Mono', 'Consolas', monospace;
                                font-size: 0.875rem;
                                line-height: 1.5;
                                direction: ltr;
                                text-align: left;
                                white-space: pre;
                                word-spacing: normal;
                                word-break: normal;
                                tab-size: 4;
                                hyphens: none;
                                background: #ffffff;
                                color: #161616;
                            }}
                        </style>
                    </head>
                    <body>
                        {html}
                    </body>
                    </html>";
                WebView.NavigateToString(styledHtml);
            }
            catch (Exception ex)
            {
                WebView.NavigateToString($@"
                    <html>
                    <body style='font-family: IBM Plex Sans, Segoe UI, system-ui, sans-serif; color: #da1e28; padding: 2rem;'>
                        <p>Error processing markdown content: {ex.Message}</p>
                    </body>
                    </html>");
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string ScreenId => "markdown2";
        private Dictionary<string, string>? Context { get; set; }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(MarkdownViewer2),
                new PropertyMetadata(string.Empty, OnTextChanged));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MarkdownViewer2 viewer)
                return;

            viewer.UpdateMarkdownContent(e.NewValue as string ?? string.Empty);
        }

        public void SetScreenInformation(Dictionary<string, string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            if (!context.TryGetValue(ContextKey._body, out var body))
            {
                Text = "No content available";
                return;
            }
            Text = body;
        }
    }
}