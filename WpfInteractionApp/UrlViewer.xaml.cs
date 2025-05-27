using Contextualizer.PluginContracts;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public partial class UrlViewer : UserControl, IDynamicScreen
    {
        private bool _isWebViewInitialized;

        public UrlViewer()
        {
            InitializeComponent();
            _isWebViewInitialized = false;
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await WebView.EnsureCoreWebView2Async();
                _isWebViewInitialized = true;

                // WebView2'ye JavaScript mesajlarını dinleme yeteneği ekle
                WebView.WebMessageReceived += WebView_WebMessageReceived;

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
                // URL'yi doğrula ve düzelt
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
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
            Context = context ?? throw new ArgumentNullException(nameof(context));
            if (!context.TryGetValue(ContextKey._body, out var url))
            {
                Url = string.Empty;
                return;
            }
            Url = url;
        }
    }
} 