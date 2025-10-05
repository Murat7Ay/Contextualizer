using Contextualizer.PluginContracts;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public partial class UrlViewer : UserControl, IDynamicScreen
    {
        private bool _isWebViewInitialized;
        private string _sharedProfilePath;

        public UrlViewer()
        {
            InitializeComponent();
            _isWebViewInitialized = false;
        }

        private async void InitializeWebView()
        {
            try
            {
                if (!string.IsNullOrEmpty(_sharedProfilePath))
                {
                    var env = await CoreWebView2Environment.CreateAsync(null, _sharedProfilePath);
                    await WebView.EnsureCoreWebView2Async(env);
                }
                else
                {
                    await WebView.EnsureCoreWebView2Async();
                }

                _isWebViewInitialized = true;
                WebView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

                if (!string.IsNullOrEmpty(Url))
                {
                    NavigateToUrl(Url);
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

        private void NavigateToUrl(string url)
        {
            if (!_isWebViewInitialized)
            {
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                WebView.NavigateToString("<p>No URL provided</p>");
                return;
            }

            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
                {
                    if (File.Exists(url))
                    {
                        url = "file:///" + url.Replace("\\", "/");
                    }
                    else
                    {
                        url = "https://" + url;
                    }
                }


                WebView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                WebView.NavigateToString($@"
                    <html>
                    <body style='font-family: IBM Plex Sans, Segoe UI, system-ui, sans-serif; color: #da1e28; padding: 2rem;'>
                        <p>Error loading URL: {ex.Message}</p>
                    </body>
                    </html>");
            }
        }

        public string Url
        {
            get => (string)GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public string ScreenId => "url_viewer";
        private Dictionary<string, string>? Context { get; set; }

        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register(
                nameof(Url),
                typeof(string),
                typeof(UrlViewer),
                new PropertyMetadata(string.Empty, OnUrlChanged));

        private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UrlViewer viewer)
                return;

            viewer.NavigateToUrl(e.NewValue as string ?? string.Empty);
        }

        public void SetScreenInformation(Dictionary<string, string> context)
        {
            if (context.TryGetValue("shared_webview_profile", out string profilePath))
            {
                _sharedProfilePath = profilePath;
            }

            if (context.TryGetValue("url", out string url))
            {
                Url = url;
            }

            if (!_isWebViewInitialized)
            {
                InitializeWebView();
            }
        }
    }
} 