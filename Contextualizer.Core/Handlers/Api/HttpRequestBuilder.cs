using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Contextualizer.Core.Handlers.Api.Models;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpRequestBuilder
    {
        public static HttpRequestMessage BuildRequestMessage(
            NormalizedHttpConfig config,
            Dictionary<string, string> context,
            Dictionary<string, string>? extraQuery = null)
        {
            var urlTemplate = config.UrlTemplate ?? string.Empty;
            var resolvedUrl = HandlerContextProcessor.ReplaceDynamicValues(urlTemplate, context);
            if (string.IsNullOrWhiteSpace(resolvedUrl))
                throw new InvalidOperationException("URL cannot be empty after placeholder replacement");

            var query = ResolveDictionary(config.QueryTemplates, context);
            if (extraQuery != null)
            {
                foreach (var kvp in extraQuery)
                    query[kvp.Key] = kvp.Value;
            }

            HttpAuthHandler.ApplyAuthToQueryAndHeaders(config, context, query, out var authHeaders);

            var finalUrl = BuildUrlWithQuery(resolvedUrl, query);
            var method = new HttpMethod(config.Method ?? "GET");
            var request = new HttpRequestMessage(method, finalUrl);

            var body = ResolveBodyTemplate(config, context);
            if (!string.IsNullOrWhiteSpace(body) && ShouldSendBody(method.Method, config))
            {
                var contentType = config.ContentType ?? "application/json";
                var charset = config.Charset ?? "utf-8";
                request.Content = new StringContent(body, Encoding.GetEncoding(charset), contentType);
            }

            var headers = ResolveDictionary(config.HeaderTemplates, context);
            foreach (var kvp in authHeaders)
                headers[kvp.Key] = kvp.Value;

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header.Key))
                    continue;

                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return request;
        }

        private static bool ShouldSendBody(string method, NormalizedHttpConfig config)
        {
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                return config.AllowBodyForGet;
            if (string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
                return config.AllowBodyForDelete;
            return true;
        }

        private static string? ResolveBodyTemplate(NormalizedHttpConfig config, Dictionary<string, string> context)
        {
            if (!string.IsNullOrWhiteSpace(config.BodyTextTemplate))
            {
                return HandlerContextProcessor.ReplaceDynamicValues(config.BodyTextTemplate, context);
            }

            if (!string.IsNullOrWhiteSpace(config.BodyJsonTemplate))
            {
                return HandlerContextProcessor.ReplaceDynamicValues(config.BodyJsonTemplate, context);
            }

            return null;
        }

        private static Dictionary<string, string> ResolveDictionary(Dictionary<string, string>? source, Dictionary<string, string> context)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return result;
            foreach (var kvp in source)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;
                result[kvp.Key] = HandlerContextProcessor.ReplaceDynamicValues(kvp.Value ?? string.Empty, context);
            }
            return result;
        }

        private static string BuildUrlWithQuery(string url, Dictionary<string, string> query)
        {
            if (query.Count == 0)
                return url;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                var existing = ParseQuery(string.Empty);
                foreach (var kvp in query)
                    existing[kvp.Key] = kvp.Value;
                return $"{url}?{BuildQueryString(existing)}";
            }

            var builder = new UriBuilder(absolute);
            var merged = ParseQuery(builder.Query);
            foreach (var kvp in query)
                merged[kvp.Key] = kvp.Value;

            builder.Query = BuildQueryString(merged);
            return builder.Uri.ToString();
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(query))
                return result;

            var q = query.TrimStart('?');
            foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf('=');
                if (idx < 0)
                {
                    result[Uri.UnescapeDataString(part)] = string.Empty;
                    continue;
                }

                var key = Uri.UnescapeDataString(part.Substring(0, idx));
                var value = Uri.UnescapeDataString(part[(idx + 1)..]);
                result[key] = value;
            }
            return result;
        }

        private static string BuildQueryString(Dictionary<string, string> query)
        {
            return string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value ?? string.Empty)}"));
        }
    }
}
