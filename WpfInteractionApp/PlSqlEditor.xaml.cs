using Contextualizer.PluginContracts;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WpfInteractionApp.Services;

namespace WpfInteractionApp
{
    public partial class PlSqlEditor : UserControl, IDynamicScreen, IThemeAware, IDisposable
    {
        private bool _isWebViewInitialized;
        private string _currentTheme = "light";
        private bool _disposed = false;

        public PlSqlEditor()
        {
            InitializeComponent();
            _isWebViewInitialized = false;
            _currentTheme = ThemeManager.Instance.CurrentTheme.ToLower();
            InitializeWebView();
            
            // Subscribe to Unloaded event for cleanup
            this.Unloaded += PlSqlEditor_Unloaded;
        }

        private void PlSqlEditor_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Cleanup when control is unloaded
            Dispose();
        }

        public void OnThemeChanged(string theme)
        {
            _currentTheme = theme.ToLower();
            if (_isWebViewInitialized)
            {
                // JavaScript'e tema değişikliğini bildir
                var script = $"setTheme('{(_currentTheme == "dark" ? "monokai" : _currentTheme == "dim" ? "tomorrow_night" : "sqlserver")}')";
                WebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                
                // Get the Assets/ace folder path (next to the executable)
                string aceFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Ace");
                
                // Verify the folder exists
                if (!System.IO.Directory.Exists(aceFolderPath))
                {
                    // Try lowercase 'ace' folder as fallback
                    aceFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ace");
                    
                    if (!System.IO.Directory.Exists(aceFolderPath))
                    {
                        throw new System.IO.DirectoryNotFoundException($"Assets folder not found at: {aceFolderPath}");
                    }
                }
                
                WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "local", // virtual hostname
                    aceFolderPath,
                    CoreWebView2HostResourceAccessKind.Allow);
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
                MessageBox.Show($"WebView2 initialization failed: {ex.Message}\n\nPlease ensure Assets folder exists next to the executable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        <script src='https://local/ace.js'></script>
                        <script src='https://local/mode-sql.js'></script>
                        <script src='https://local/theme-sqlserver.js'></script>
                        <script src='https://local/theme-monokai.js'></script>
                        <script src='https://local/theme-tomorrow_night.js'></script>
                        <style>
                            body {{
                                margin: 0;
                                padding: 0;
                                font-family: 'IBM Plex Sans', 'Segoe UI', system-ui, sans-serif;
                                background-color: {(_currentTheme == "dark" ? "#161616" : _currentTheme == "dim" ? "#474747" : "#f4f4f4")};
                                color: {(_currentTheme == "dark" ? "#ffffff" : _currentTheme == "dim" ? "#ffffff" : "#161616")};
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
                            editor.setTheme('ace/theme/{(_currentTheme == "dark" ? "monokai" : _currentTheme == "dim" ? "tomorrow_night" : "sqlserver")}');
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        if (_isWebViewInitialized && WebView != null)
                        {
                            if (WebView.CoreWebView2 != null)
                            {
                                WebView.WebMessageReceived -= WebView_WebMessageReceived;
                                WebView.CoreWebView2.Stop();
                            }
                            
                            // Close the WebView2 control to ensure browser processes are terminated
                            try
                            {
                                WebView.Dispose();
                            }
                            catch { /* Ignore disposal errors */ }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing PlSqlEditor: {ex.Message}");
                    }
                }
                _disposed = true;
            }
        }
    }
} 