using Contextualizer.PluginContracts;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class PlSqlEditor : UserControl, IDynamicScreen
    {
        private bool _isWebViewInitialized;
        private string _currentTheme = "light";

        public PlSqlEditor()
        {
            InitializeComponent();
            _isWebViewInitialized = false;
            InitializeWebView();

            _currentTheme = ThemeManager.Instance.CurrentTheme.ToLower();

            // Tema değişikliğini dinle
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            
            // Unloaded event'ini dinle
            this.Unloaded += PlSqlEditor_Unloaded;
        }

        private void PlSqlEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, string theme)
        {
            _currentTheme = theme.ToLower();
            if (_isWebViewInitialized)
            {
                // JavaScript'e tema değişikliğini bildir
                var script = $"setTheme('{(_currentTheme == "dark" ? "monokai" : "sqlserver")}')";
                WebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;

                // WebView2'ye JavaScript mesajlarını dinleme yeteneği ekle
                WebView.WebMessageReceived += WebView_WebMessageReceived;

                if (!string.IsNullOrEmpty(Text))
                {
                    UpdateEditorContent(Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            // JavaScript'ten gelen mesajları işle
            var message = e.WebMessageAsJson;
            // Burada gerekirse mesajları işleyebilirsiniz
        }

        private void UpdateEditorContent(string sql)
        {
            if (!_isWebViewInitialized)
            {
                return;
            }

            if (string.IsNullOrEmpty(sql))
            {
                sql = "-- PL/SQL sorgunuzu buraya yazın";
            }

            try
            {
                var html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>PL/SQL Editor</title>
                        <script src='https://cdnjs.cloudflare.com/ajax/libs/ace/1.23.0/ace.js'></script>
                        <script src='https://cdnjs.cloudflare.com/ajax/libs/ace/1.23.0/mode-sql.js'></script>
                        <script src='https://cdnjs.cloudflare.com/ajax/libs/ace/1.23.0/theme-sqlserver.js'></script>
                        <script src='https://cdnjs.cloudflare.com/ajax/libs/ace/1.23.0/theme-monokai.js'></script>
                        <style>
                            body {{
                                margin: 0;
                                padding: 0;
                                font-family: 'IBM Plex Sans', 'Segoe UI', system-ui, sans-serif;
                                background-color: {(_currentTheme == "dark" ? "#161616" : "#f4f4f4")};
                                color: {(_currentTheme == "dark" ? "#ffffff" : "#161616")};
                            }}
                            #editor {{
                                position: absolute;
                                top: 0;
                                right: 0;
                                bottom: 0;
                                left: 0;
                            }}
                        </style>
                    </head>
                    <body>
                        <div id='editor'></div>
                        <script>
                            var editor = ace.edit('editor');
                            editor.setTheme('ace/theme/{(_currentTheme == "dark" ? "monokai" : "sqlserver")}');
                            editor.session.setMode('ace/mode/sql');
                            editor.setOptions({{
                                fontSize: '14px',
                                showPrintMargin: false,
                                showGutter: true,
                                highlightActiveLine: true,
                                enableBasicAutocompletion: true,
                                enableSnippets: true,
                                enableLiveAutocompletion: true,
                                tabSize: 4,
                                useSoftTabs: true,
                                wrap: true
                            }});

                            // PL/SQL için özel ayarlar
                            editor.session.setMode('ace/mode/sql');
                            editor.session.setValue(`{sql}`);

                            // Tema değiştirme fonksiyonu
                            function setTheme(theme) {{
                                editor.setTheme('ace/theme/' + theme);
                            }}

                            // Değişiklikleri ana uygulamaya bildir
                            editor.session.on('change', function() {{
                                window.chrome.webview.postMessage(JSON.stringify({{
                                    type: 'contentChange',
                                    content: editor.session.getValue()
                                }}));
                            }});
                        </script>
                    </body>
                    </html>";

                WebView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                WebView.NavigateToString($@"
                    <html>
                    <body style='font-family: IBM Plex Sans, Segoe UI, system-ui, sans-serif; color: #da1e28; padding: 2rem;'>
                        <p>Error initializing editor: {ex.Message}</p>
                    </body>
                    </html>");
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string ScreenId => "plsql_editor";
        private Dictionary<string, string>? Context { get; set; }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(PlSqlEditor),
                new PropertyMetadata(string.Empty, OnTextChanged));

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PlSqlEditor editor)
                return;

            editor.UpdateEditorContent(e.NewValue as string ?? string.Empty);
        }

        public void SetScreenInformation(Dictionary<string, string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            if (!context.TryGetValue(ContextKey._body, out var body))
            {
                Text = "-- PL/SQL sorgunuzu buraya yazın";
                return;
            }
            Text = body;
        }
    }
} 