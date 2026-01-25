using System;
using System.Collections.Generic;
using System.Text;
using Contextualizer.Core.Handlers.Api.Models;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpAuthHandler
    {
        public static void ApplyAuthToQueryAndHeaders(
            NormalizedHttpConfig config,
            Dictionary<string, string> context,
            Dictionary<string, string> query,
            out Dictionary<string, string> headers)
        {
            headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (config.AuthType == null) return;

            var type = config.AuthType.Trim().ToLowerInvariant();
            var token = config.AuthTokenTemplate == null ? null : HandlerContextProcessor.ReplaceDynamicValues(config.AuthTokenTemplate, context);
            var username = config.AuthUsernameTemplate == null ? null : HandlerContextProcessor.ReplaceDynamicValues(config.AuthUsernameTemplate, context);
            var password = config.AuthPasswordTemplate == null ? null : HandlerContextProcessor.ReplaceDynamicValues(config.AuthPasswordTemplate, context);

            if (type == "basic")
            {
                var raw = $"{username ?? string.Empty}:{password ?? string.Empty}";
                var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                headers["Authorization"] = $"Basic {value}";
                return;
            }

            if (type == "bearer" || type == "oauth2")
            {
                var prefix = string.IsNullOrWhiteSpace(config.AuthTokenPrefix) ? "Bearer" : config.AuthTokenPrefix;
                headers["Authorization"] = $"{prefix} {token ?? string.Empty}".Trim();
                return;
            }

            if (type == "api_key" || type == "custom")
            {
                if (!string.IsNullOrWhiteSpace(config.AuthHeaderName))
                {
                    var prefix = config.AuthTokenPrefix;
                    headers[config.AuthHeaderName] = string.IsNullOrWhiteSpace(prefix) ? token ?? string.Empty : $"{prefix} {token}".Trim();
                }
                if (!string.IsNullOrWhiteSpace(config.AuthQueryName))
                {
                    query[config.AuthQueryName] = token ?? string.Empty;
                }
            }
        }
    }
}
