using System.Collections.Generic;
using Contextualizer.Core.Handlers.Api.Models;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpConfigNormalizer
    {
        public static NormalizedHttpConfig NormalizeConfig(HandlerConfig config)
        {
            var normalized = new NormalizedHttpConfig();
            var http = config.Http;

            if (http?.Request != null)
            {
                normalized.UrlTemplate = http.Request.Url ?? config.Url;
                normalized.Method = http.Request.Method ?? config.Method ?? "GET";
                normalized.HeaderTemplates = http.Request.Headers ?? config.Headers ?? new Dictionary<string, string>();
                normalized.QueryTemplates = http.Request.Query ?? new Dictionary<string, string>();
                normalized.BodyJsonTemplate = http.Request.Body.HasValue ? http.Request.Body.Value.GetRawText() : null;
                normalized.BodyTextTemplate = http.Request.BodyText;
                normalized.ContentType = http.Request.ContentType ?? config.ContentType ?? "application/json";
                normalized.Charset = http.Request.Charset;
                normalized.AllowBodyForGet = http.Request.AllowBodyForGet == true;
                normalized.AllowBodyForDelete = http.Request.AllowBodyForDelete == true;
            }
            else
            {
                normalized.UrlTemplate = config.Url;
                normalized.Method = config.Method ?? "GET";
                normalized.HeaderTemplates = config.Headers ?? new Dictionary<string, string>();
                normalized.QueryTemplates = new Dictionary<string, string>();
                normalized.BodyJsonTemplate = config.RequestBody.HasValue ? config.RequestBody.Value.GetRawText() : null;
                normalized.BodyTextTemplate = null;
                normalized.ContentType = config.ContentType ?? "application/json";
                normalized.AllowBodyForGet = false;
                normalized.AllowBodyForDelete = false;
            }

            normalized.LegacyTimeoutSeconds = config.TimeoutSeconds;
            if (http?.Timeouts != null)
            {
                normalized.ConnectTimeoutSeconds = http.Timeouts.ConnectSeconds;
                normalized.OverallTimeoutSeconds = http.Timeouts.OverallSeconds ?? http.Timeouts.ReadSeconds;
            }

            if (http?.Auth != null)
            {
                normalized.AuthType = http.Auth.Type;
                normalized.AuthTokenTemplate = http.Auth.Token;
                normalized.AuthUsernameTemplate = http.Auth.Username;
                normalized.AuthPasswordTemplate = http.Auth.Password;
                normalized.AuthHeaderName = http.Auth.HeaderName;
                normalized.AuthQueryName = http.Auth.QueryName;
                normalized.AuthTokenPrefix = http.Auth.TokenPrefix;
            }

            if (http?.Proxy != null)
            {
                normalized.ProxyUrl = http.Proxy.Url;
                normalized.ProxyUsername = http.Proxy.Username;
                normalized.ProxyPassword = http.Proxy.Password;
                normalized.ProxyBypassList = http.Proxy.Bypass ?? new List<string>();
                normalized.UseSystemProxy = http.Proxy.UseSystemProxy == true;
                normalized.ProxyUseDefaultCredentials = http.Proxy.UseDefaultCredentials == true;
            }

            if (http?.Tls != null)
            {
                normalized.AllowInvalidCerts = http.Tls.AllowInvalidCert == true;
                normalized.MinTls = http.Tls.MinTls;
                normalized.ClientCertPath = http.Tls.ClientCertPath;
                normalized.ClientCertPassword = http.Tls.ClientCertPassword;
            }

            if (http?.Retry != null)
            {
                normalized.RetryEnabled = http.Retry.Enabled == true;
                normalized.RetryMaxAttempts = http.Retry.MaxAttempts ?? 3;
                normalized.RetryBaseDelayMs = http.Retry.BaseDelayMs ?? 250;
                normalized.RetryMaxDelayMs = http.Retry.MaxDelayMs ?? 5000;
                normalized.RetryJitter = http.Retry.Jitter != false;
                normalized.RetryOnStatus = http.Retry.RetryOnStatus ?? new List<int> { 408, 429, 500, 502, 503, 504 };
                normalized.RetryOnExceptions = http.Retry.RetryOnExceptions ?? new List<string>();
            }

            if (http?.Pagination != null)
            {
                normalized.PaginationType = http.Pagination.Type;
                normalized.PaginationCursorPath = http.Pagination.CursorPath;
                normalized.PaginationNextParam = http.Pagination.NextParam;
                normalized.PaginationLimitParam = http.Pagination.LimitParam;
                normalized.PaginationOffsetParam = http.Pagination.OffsetParam;
                normalized.PaginationPageParam = http.Pagination.PageParam;
                normalized.PaginationPageSize = http.Pagination.PageSize;
                normalized.PaginationMaxPages = http.Pagination.MaxPages;
                normalized.PaginationStartPage = http.Pagination.StartPage;
                normalized.PaginationStartOffset = http.Pagination.StartOffset;
            }

            if (http?.Response != null)
            {
                normalized.ResponseExpect = http.Response.Expect;
                normalized.FlattenJson = http.Response.FlattenJson != false;
                normalized.FlattenPrefix = http.Response.FlattenPrefix;
                normalized.ResponseMaxBytes = http.Response.MaxBytes ?? (5 * 1024 * 1024);
                normalized.IncludeHeaders = http.Response.IncludeHeaders == true;
                normalized.HeaderPrefix = http.Response.HeaderPrefix ?? "Header.";
            }
            else
            {
                normalized.ResponseExpect = null;
                normalized.FlattenJson = true;
                normalized.ResponseMaxBytes = 5 * 1024 * 1024;
                normalized.HeaderPrefix = "Header.";
            }

            if (http?.Output != null)
            {
                normalized.OutputMappings = http.Output.Mappings ?? new Dictionary<string, string>();
                normalized.OutputHeaderMappings = http.Output.HeaderMappings ?? new Dictionary<string, string>();
                normalized.IncludeRawBody = http.Output.IncludeRawBody != false;
                normalized.RawBodyKey = http.Output.RawBodyKey;
            }
            else
            {
                normalized.OutputMappings = new Dictionary<string, string>();
                normalized.OutputHeaderMappings = new Dictionary<string, string>();
                normalized.IncludeRawBody = true;
                normalized.RawBodyKey = "RawResponse";
            }

            return normalized;
        }
    }
}
