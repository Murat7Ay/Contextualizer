using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Contextualizer.Core.Handlers.Api.Models;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpClientFactory
    {
        public static HttpClient CreateHttpClient(NormalizedHttpConfig config)
        {
            var handler = new SocketsHttpHandler()
            {
                MaxConnectionsPerServer = 10,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            };

            if (config.ConnectTimeoutSeconds.HasValue)
            {
                handler.ConnectTimeout = TimeSpan.FromSeconds(config.ConnectTimeoutSeconds.Value);
            }

            ApplyProxySettings(handler, config);
            ApplyTlsSettings(handler, config);

            var httpClient = new HttpClient(handler);

            var overallSeconds = config.OverallTimeoutSeconds ?? config.LegacyTimeoutSeconds ?? 30;
            httpClient.Timeout = TimeSpan.FromSeconds(overallSeconds);

            httpClient.DefaultRequestHeaders.ConnectionClose = false;

            try
            {
                httpClient.DefaultRequestHeaders.Add("Keep-Alive", "timeout=300, max=1000");
            }
            catch
            {
                // Keep-Alive header might not be supported in all scenarios
            }

            return httpClient;
        }

        private static void ApplyProxySettings(SocketsHttpHandler handler, NormalizedHttpConfig config)
        {
            if (config.UseSystemProxy)
            {
                handler.UseProxy = true;
                handler.Proxy = WebRequest.DefaultWebProxy;
                return;
            }

            if (string.IsNullOrWhiteSpace(config.ProxyUrl))
                return;

            var proxy = new WebProxy(config.ProxyUrl);
            if (config.ProxyBypassList.Count > 0)
                proxy.BypassList = config.ProxyBypassList.ToArray();

            if (config.ProxyUseDefaultCredentials)
            {
                proxy.UseDefaultCredentials = true;
            }
            else if (!string.IsNullOrWhiteSpace(config.ProxyUsername))
            {
                proxy.Credentials = new NetworkCredential(config.ProxyUsername, config.ProxyPassword);
            }

            handler.Proxy = proxy;
            handler.UseProxy = true;
        }

        private static void ApplyTlsSettings(SocketsHttpHandler handler, NormalizedHttpConfig config)
        {
            if (config.AllowInvalidCerts)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            }

            if (!string.IsNullOrWhiteSpace(config.MinTls))
            {
                if (config.MinTls == "1.2") handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12;
                else if (config.MinTls == "1.3") handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls13;
            }

            if (!string.IsNullOrWhiteSpace(config.ClientCertPath))
            {
                try
                {
                    var cert = string.IsNullOrWhiteSpace(config.ClientCertPassword)
                        ? new X509Certificate2(config.ClientCertPath)
                        : new X509Certificate2(config.ClientCertPath, config.ClientCertPassword);
                    handler.SslOptions.ClientCertificates ??= new X509CertificateCollection();
                    handler.SslOptions.ClientCertificates.Add(cert);
                }
                catch (Exception ex)
                {
                    UserFeedback.ShowWarning($"Failed to load client certificate: {ex.Message}");
                }
            }
        }
    }
}
