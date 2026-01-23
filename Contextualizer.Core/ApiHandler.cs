using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class ApiHandler : Dispatch, IHandler, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Regex? _optionalRegex;
        private readonly NormalizedHttpConfig _config;
        private bool _disposed = false;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null
        };

        public static string TypeName => "Api";
        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        public ApiHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            // Initialize optional regex if configured
            if (!string.IsNullOrWhiteSpace(handlerConfig.Regex))
            {
                try
                {
                    _optionalRegex = new Regex(
                        handlerConfig.Regex,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(5) // ReDoS protection
                    );
                }
                catch (ArgumentException ex)
                {
                    UserFeedback.ShowError($"ApiHandler '{handlerConfig.Name}': Invalid regex pattern - {ex.Message}");
                    throw new InvalidOperationException($"Invalid regex pattern in ApiHandler '{handlerConfig.Name}': {handlerConfig.Regex}", ex);
                }
                catch (RegexMatchTimeoutException ex)
                {
                    UserFeedback.ShowError($"ApiHandler '{handlerConfig.Name}': Regex compilation timeout - {ex.Message}");
                    throw new InvalidOperationException($"Regex compilation timeout in ApiHandler '{handlerConfig.Name}': {handlerConfig.Regex}", ex);
                }
            }
            
            _config = NormalizeConfig(handlerConfig);
            
            // Create HttpClient with optimized settings for long-running applications
            _httpClient = CreateHttpClient(_config);
        }

        private HttpClient CreateHttpClient(NormalizedHttpConfig config)
        {
            // Create SocketsHttpHandler with optimized settings for long-running applications
            var handler = new SocketsHttpHandler()
            {
                // Connection pooling settings
                MaxConnectionsPerServer = 10, // Limit concurrent connections per server
                PooledConnectionLifetime = TimeSpan.FromMinutes(15), // Refresh connections every 15 minutes
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5), // Close idle connections after 5 minutes
            };

            if (config.ConnectTimeoutSeconds.HasValue)
            {
                handler.ConnectTimeout = TimeSpan.FromSeconds(config.ConnectTimeoutSeconds.Value);
            }

            ApplyProxySettings(handler, config);
            ApplyTlsSettings(handler, config);

            var httpClient = new HttpClient(handler);
            
            // Set timeout if configured, otherwise use a reasonable default
            var overallSeconds = config.OverallTimeoutSeconds ?? config.LegacyTimeoutSeconds ?? 30;
            httpClient.Timeout = TimeSpan.FromSeconds(overallSeconds);

            // Optimize for long-running applications
            httpClient.DefaultRequestHeaders.ConnectionClose = false; // Keep connections alive
            
            // Add Keep-Alive header safely
            try
            {
                httpClient.DefaultRequestHeaders.Add("Keep-Alive", "timeout=300, max=1000"); // 5 min timeout, max 1000 requests
            }
            catch
            {
                // Keep-Alive header might not be supported in all scenarios
            }

            return httpClient;
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
                return false;

            // Use compiled regex if configured
            if (_optionalRegex != null)
            {
                try
                {
                    return _optionalRegex.IsMatch(clipboardContent.Text);
                }
                catch (RegexMatchTimeoutException ex)
                {
                    UserFeedback.ShowWarning($"ApiHandler '{HandlerConfig.Name}': Regex match timeout for input length {clipboardContent.Text.Length}");
                    System.Diagnostics.Debug.WriteLine($"ApiHandler: Regex match timeout - {ex.Message}");
                    return false;
                }
                catch (ArgumentException ex)
                {
                    UserFeedback.ShowError($"ApiHandler '{HandlerConfig.Name}': Invalid input for regex matching - {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = clipboardContent.Text;

            // If called programmatically (e.g., MCP), merge seed arguments into context BEFORE any template replacement.
            // This allows URL/body placeholders like $(oid) to work without relying on clipboard/regex/groups.
            var seed = clipboardContent.SeedContext;
            if (seed != null && seed.Count > 0)
            {
                var isMcpCall =
                    seed.TryGetValue(ContextKey._trigger, out var t) &&
                    string.Equals(t, "mcp", StringComparison.OrdinalIgnoreCase);

                foreach (var kvp in seed)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                        continue;

                    if (kvp.Key.Equals(ContextKey._trigger, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (isMcpCall && HandlerConfig.McpSeedOverwrite)
                    {
                        context[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                    else if (!context.ContainsKey(kvp.Key))
                    {
                        context[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }
            }

            // Process regex groups if configured
            if (_optionalRegex != null)
            {
                try
                {
                    var match = _optionalRegex.Match(clipboardContent.Text);

                    if (match.Success)
                    {
                        // Add full match
                        context[ContextKey._match] = match.Value;
                        
                        // Process configured groups
                        if (HandlerConfig.Groups != null && HandlerConfig.Groups.Count > 0)
                        {
                            for (int i = 0; i < HandlerConfig.Groups.Count; i++)
                            {
                                var groupName = HandlerConfig.Groups[i];
                                string groupValue;

                                // Try to get named group first, then fall back to indexed group
                                var namedGroup = match.Groups[groupName];
                                if (namedGroup.Success)
                                {
                                    groupValue = namedGroup.Value;
                                }
                                else
                                {
                                    // Groups[0] is always the full match, actual capturing groups start from index 1
                                    var groupIndex = i + 1;
                                    groupValue = match.Groups.Count > groupIndex ? match.Groups[groupIndex].Value : string.Empty;
                                }

                                context[groupName] = groupValue;
                            }
                        }
                        else
                        {
                            // If no groups configured, add all captured groups with numeric keys
                            for (int i = 1; i < match.Groups.Count; i++)
                            {
                                context[$"group_{i}"] = match.Groups[i].Value;
                            }
                        }
                    }
                    else
                    {
                        UserFeedback.ShowWarning($"ApiHandler '{HandlerConfig.Name}': Regex pattern did not match input");
                    }
                }
                catch (RegexMatchTimeoutException ex)
                {
                    context[ContextKey._error] = $"Regex match timeout: {ex.Message}";
                    UserFeedback.ShowError($"ApiHandler '{HandlerConfig.Name}': Regex match timeout");
                    System.Diagnostics.Debug.WriteLine($"ApiHandler: Regex match timeout - {ex.Message}");
                }
                catch (ArgumentException ex)
                {
                    context[ContextKey._error] = $"Invalid regex operation: {ex.Message}";
                    UserFeedback.ShowError($"ApiHandler '{HandlerConfig.Name}': Invalid regex operation - {ex.Message}");
                }
            }

            try
            {
                await ExecuteHttpFlowAsync(context);
            }
            catch (Exception ex)
            {
                context["Error"] = ex.Message;
                context[ContextKey._error] = ex.Message;
                UserFeedback.ShowError($"API request failed: {ex.Message}");
            }

            return context;
        }

        private async Task ExecuteHttpFlowAsync(Dictionary<string, string> context)
        {
            var paginationType = _config.PaginationType;
            var maxPages = paginationType == null ? 1 : Math.Max(1, _config.PaginationMaxPages ?? 1);
            var rawResponses = new List<string>();
            JsonDocument? lastJson = null;
            int pageIndex = 0;
            string? cursor = null;
            int? offset = _config.PaginationStartOffset;
            int? page = _config.PaginationStartPage;

            while (pageIndex < maxPages)
            {
                var extraQuery = BuildPaginationQuery(paginationType, cursor, offset, page);
                using var request = BuildRequestMessage(context, extraQuery);
                using var response = await SendWithRetryAsync(() => _httpClient.SendAsync(request));

                var responseInfo = await ReadResponseAsync(response, context, pageIndex, paginationType != null);
                rawResponses.Add(responseInfo.RawBody ?? string.Empty);
                lastJson?.Dispose();
                lastJson = responseInfo.JsonDocument;

                pageIndex++;

                if (paginationType == null || !response.IsSuccessStatusCode)
                {
                    break;
                }

                if (paginationType.Equals("cursor", StringComparison.OrdinalIgnoreCase))
                {
                    cursor = TryExtractCursor(responseInfo.JsonDocument, _config.PaginationCursorPath);
                    if (string.IsNullOrWhiteSpace(cursor))
                        break;
                }
                else if (paginationType.Equals("offset", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_config.PaginationPageSize.HasValue)
                        break;
                    offset = (offset ?? 0) + _config.PaginationPageSize.Value;
                }
                else if (paginationType.Equals("page", StringComparison.OrdinalIgnoreCase))
                {
                    page = (page ?? 1) + 1;
                }
                else
                {
                    break;
                }
            }

            if (rawResponses.Count > 1)
            {
                var json = JsonSerializer.Serialize(rawResponses, _jsonOptions);
                context["RawResponses"] = json;
                context["PageCount"] = rawResponses.Count.ToString();
            }

            if (lastJson != null)
            {
                ApplyOutputMappings(lastJson.RootElement, context);
            }
        }

        private Dictionary<string, string>? BuildPaginationQuery(string? paginationType, string? cursor, int? offset, int? page)
        {
            if (paginationType == null)
                return null;

            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (paginationType.Equals("cursor", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(_config.PaginationNextParam) && !string.IsNullOrWhiteSpace(cursor))
                    query[_config.PaginationNextParam] = cursor;
                if (!string.IsNullOrWhiteSpace(_config.PaginationLimitParam) && _config.PaginationPageSize.HasValue)
                    query[_config.PaginationLimitParam] = _config.PaginationPageSize.Value.ToString();
            }
            else if (paginationType.Equals("offset", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(_config.PaginationOffsetParam) && offset.HasValue)
                    query[_config.PaginationOffsetParam] = offset.Value.ToString();
                if (!string.IsNullOrWhiteSpace(_config.PaginationLimitParam) && _config.PaginationPageSize.HasValue)
                    query[_config.PaginationLimitParam] = _config.PaginationPageSize.Value.ToString();
            }
            else if (paginationType.Equals("page", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(_config.PaginationPageParam) && page.HasValue)
                    query[_config.PaginationPageParam] = page.Value.ToString();
                if (!string.IsNullOrWhiteSpace(_config.PaginationLimitParam) && _config.PaginationPageSize.HasValue)
                    query[_config.PaginationLimitParam] = _config.PaginationPageSize.Value.ToString();
            }

            return query.Count > 0 ? query : null;
        }

        private HttpRequestMessage BuildRequestMessage(Dictionary<string, string> context, Dictionary<string, string>? extraQuery)
        {
            var urlTemplate = _config.UrlTemplate ?? string.Empty;
            var resolvedUrl = HandlerContextProcessor.ReplaceDynamicValues(urlTemplate, context);
            if (string.IsNullOrWhiteSpace(resolvedUrl))
                throw new InvalidOperationException("URL cannot be empty after placeholder replacement");

            var query = ResolveDictionary(_config.QueryTemplates, context);
            if (extraQuery != null)
            {
                foreach (var kvp in extraQuery)
                    query[kvp.Key] = kvp.Value;
            }

            ApplyAuthToQueryAndHeaders(context, query, out var authHeaders);

            var finalUrl = BuildUrlWithQuery(resolvedUrl, query);
            var method = new HttpMethod(_config.Method ?? "GET");
            var request = new HttpRequestMessage(method, finalUrl);

            var body = ResolveBodyTemplate(context);
            if (!string.IsNullOrWhiteSpace(body) && ShouldSendBody(method.Method))
            {
                var contentType = _config.ContentType ?? "application/json";
                var charset = _config.Charset ?? "utf-8";
                request.Content = new StringContent(body, Encoding.GetEncoding(charset), contentType);
            }

            var headers = ResolveDictionary(_config.HeaderTemplates, context);
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

        private bool ShouldSendBody(string method)
        {
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                return _config.AllowBodyForGet;
            if (string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
                return _config.AllowBodyForDelete;
            return true;
        }

        private string? ResolveBodyTemplate(Dictionary<string, string> context)
        {
            if (!string.IsNullOrWhiteSpace(_config.BodyTextTemplate))
            {
                return HandlerContextProcessor.ReplaceDynamicValues(_config.BodyTextTemplate, context);
            }

            if (!string.IsNullOrWhiteSpace(_config.BodyJsonTemplate))
            {
                return HandlerContextProcessor.ReplaceDynamicValues(_config.BodyJsonTemplate, context);
            }

            return null;
        }

        private Dictionary<string, string> ResolveDictionary(Dictionary<string, string>? source, Dictionary<string, string> context)
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

        private void ApplyAuthToQueryAndHeaders(Dictionary<string, string> context, Dictionary<string, string> query, out Dictionary<string, string> headers)
        {
            headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_config.AuthType == null) return;

            var type = _config.AuthType.Trim().ToLowerInvariant();
            var token = _config.AuthTokenTemplate == null ? null : HandlerContextProcessor.ReplaceDynamicValues(_config.AuthTokenTemplate, context);
            var username = _config.AuthUsernameTemplate == null ? null : HandlerContextProcessor.ReplaceDynamicValues(_config.AuthUsernameTemplate, context);
            var password = _config.AuthPasswordTemplate == null ? null : HandlerContextProcessor.ReplaceDynamicValues(_config.AuthPasswordTemplate, context);

            if (type == "basic")
            {
                var raw = $"{username ?? string.Empty}:{password ?? string.Empty}";
                var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                headers["Authorization"] = $"Basic {value}";
                return;
            }

            if (type == "bearer" || type == "oauth2")
            {
                var prefix = string.IsNullOrWhiteSpace(_config.AuthTokenPrefix) ? "Bearer" : _config.AuthTokenPrefix;
                headers["Authorization"] = $"{prefix} {token ?? string.Empty}".Trim();
                return;
            }

            if (type == "api_key" || type == "custom")
            {
                if (!string.IsNullOrWhiteSpace(_config.AuthHeaderName))
                {
                    var prefix = _config.AuthTokenPrefix;
                    headers[_config.AuthHeaderName] = string.IsNullOrWhiteSpace(prefix) ? token ?? string.Empty : $"{prefix} {token}".Trim();
                }
                if (!string.IsNullOrWhiteSpace(_config.AuthQueryName))
                {
                    query[_config.AuthQueryName] = token ?? string.Empty;
                }
            }
        }

        private string BuildUrlWithQuery(string url, Dictionary<string, string> query)
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

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send)
        {
            var retry = _config.RetryEnabled;
            var maxAttempts = Math.Max(1, _config.RetryMaxAttempts);
            var attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var response = await send();
                    if (!retry || attempt >= maxAttempts)
                        return response;

                    var status = (int)response.StatusCode;
                    if (!_config.RetryOnStatus.Contains(status))
                        return response;

                    await DelayForRetryAsync(attempt);
                    continue;
                }
                catch (Exception ex) when (retry && attempt < maxAttempts && ShouldRetryException(ex))
                {
                    await DelayForRetryAsync(attempt);
                }
            }
        }

        private async Task DelayForRetryAsync(int attempt)
        {
            var baseDelay = Math.Max(50, _config.RetryBaseDelayMs);
            var maxDelay = Math.Max(baseDelay, _config.RetryMaxDelayMs);
            var exp = Math.Min(maxDelay, baseDelay * (int)Math.Pow(2, attempt - 1));
            var delay = _config.RetryJitter ? exp + Random.Shared.Next(0, baseDelay) : exp;
            await Task.Delay(delay);
        }

        private bool ShouldRetryException(Exception ex)
        {
            if (_config.RetryOnExceptions.Count == 0) return true;
            var typeName = ex.GetType().Name;
            return _config.RetryOnExceptions.Any(t => string.Equals(t, typeName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<ResponseInfo> ReadResponseAsync(HttpResponseMessage response, Dictionary<string, string> context, int pageIndex, bool isPaginated)
        {
                context["StatusCode"] = ((int)response.StatusCode).ToString();
                context["IsSuccessful"] = response.IsSuccessStatusCode.ToString();
            context["ReasonPhrase"] = response.ReasonPhrase ?? string.Empty;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            context["ResponseContentType"] = contentType;

            if (_config.IncludeHeaders)
            {
                var prefix = _config.HeaderPrefix ?? "Header.";
                foreach (var header in response.Headers)
                {
                    context[$"{prefix}{header.Key}"] = string.Join(", ", header.Value);
                }
                foreach (var header in response.Content.Headers)
                {
                    context[$"{prefix}{header.Key}"] = string.Join(", ", header.Value);
                }
            }

            var maxBytes = _config.ResponseMaxBytes;
            var bytes = await ReadContentWithLimitAsync(response.Content, maxBytes);
            context["ResponseBytes"] = bytes.Length.ToString();

            var outputKey = _config.RawBodyKey ?? "RawResponse";

            var expect = _config.ResponseExpect;
            if (string.Equals(expect, "binary", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = Convert.ToBase64String(bytes);
                context["RawResponseBase64"] = base64;
                if (_config.IncludeRawBody)
                {
                    context[outputKey] = base64;
                }
                return new ResponseInfo(base64, null);
            }

            var encoding = GetResponseEncoding(response) ?? Encoding.UTF8;
            var rawBody = encoding.GetString(bytes);
            if (_config.IncludeRawBody)
            {
                context[outputKey] = rawBody;
            }

            var treatAsJson = string.Equals(expect, "json", StringComparison.OrdinalIgnoreCase) ||
                              (string.IsNullOrWhiteSpace(expect) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase));

            if (treatAsJson)
            {
                try
                {
                    var doc = JsonDocument.Parse(rawBody);
                    if (_config.FlattenJson)
                    {
                        var prefix = _config.FlattenPrefix;
                        if (isPaginated && !string.IsNullOrWhiteSpace(prefix))
                            FlattenJsonToContext(doc.RootElement, $"{prefix}{pageIndex}.", context);
                        else if (isPaginated)
                            FlattenJsonToContext(doc.RootElement, $"page_{pageIndex}.", context);
                        else
                            FlattenJsonToContext(doc.RootElement, "", context);
                    }
                    return new ResponseInfo(rawBody, doc);
                    }
                    catch (JsonException ex)
                    {
                        context["JsonParseError"] = ex.Message;
                        UserFeedback.ShowError($"Failed to parse JSON response: {ex.Message}");
                    }
                }

            return new ResponseInfo(rawBody, null);
        }

        private async Task<byte[]> ReadContentWithLimitAsync(HttpContent content, int maxBytes)
        {
            var limit = maxBytes <= 0 ? 5 * 1024 * 1024 : maxBytes;
            using var stream = await content.ReadAsStreamAsync();
            using var ms = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            int total = 0;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                total += read;
                if (total > limit)
                    throw new InvalidOperationException($"Response exceeds max_bytes limit ({limit} bytes)");
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        private static Encoding? GetResponseEncoding(HttpResponseMessage response)
        {
            var charset = response.Content.Headers.ContentType?.CharSet;
            if (string.IsNullOrWhiteSpace(charset))
                return null;
            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch
            {
                return null;
            }
        }

        private void ApplyOutputMappings(JsonElement root, Dictionary<string, string> context)
        {
            if (_config.OutputMappings.Count > 0)
            {
                foreach (var kvp in _config.OutputMappings)
                {
                    if (TryGetJsonPathValue(root, kvp.Value, out var value))
                        context[kvp.Key] = value;
                }
            }

            if (_config.OutputHeaderMappings.Count > 0)
            {
                foreach (var kvp in _config.OutputHeaderMappings)
                {
                    if (context.TryGetValue($"{_config.HeaderPrefix ?? "Header."}{kvp.Value}", out var headerValue))
                        context[kvp.Key] = headerValue;
                }
            }
        }

        private string? TryExtractCursor(JsonDocument? doc, string? cursorPath)
        {
            if (doc == null || string.IsNullOrWhiteSpace(cursorPath))
                return null;

            if (TryGetJsonPathValue(doc.RootElement, cursorPath, out var value))
                return value;

            return null;
        }

        private bool TryGetJsonPathValue(JsonElement element, string path, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return false;

            var current = element;
            foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var segment = part;
                int? index = null;
                var idxStart = part.IndexOf('[');
                if (idxStart >= 0 && part.EndsWith("]"))
                {
                    segment = part[..idxStart];
                    var idxText = part[(idxStart + 1)..^1];
                    if (int.TryParse(idxText, out var idx))
                        index = idx;
                }

                if (!string.IsNullOrWhiteSpace(segment))
                {
                    if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                        return false;
                }

                if (index.HasValue)
                {
                    if (current.ValueKind != JsonValueKind.Array || current.GetArrayLength() <= index.Value)
                        return false;
                    current = current[index.Value];
                }
            }

            value = current.ToString();
            return true;
        }

        private void ApplyProxySettings(SocketsHttpHandler handler, NormalizedHttpConfig config)
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

        private void ApplyTlsSettings(SocketsHttpHandler handler, NormalizedHttpConfig config)
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

        private NormalizedHttpConfig NormalizeConfig(HandlerConfig config)
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

        private void FlattenJsonToContext(JsonElement element, string prefix, Dictionary<string, string> context)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        FlattenJsonToContext(property.Value, newPrefix, context);
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? index.ToString() : $"{prefix}[{index}]";
                        FlattenJsonToContext(item, newPrefix, context);
                        index++;
                    }
                    break;

                default:
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        context[prefix] = element.ToString();
                    }
                    break;
            }
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
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
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }

        ~ApiHandler()
        {
            Dispose(false);
        }

        private sealed class ResponseInfo
        {
            public ResponseInfo(string? rawBody, JsonDocument? jsonDocument)
            {
                RawBody = rawBody;
                JsonDocument = jsonDocument;
            }

            public string? RawBody { get; }
            public JsonDocument? JsonDocument { get; }
        }

        private sealed class NormalizedHttpConfig
        {
            public string? UrlTemplate { get; set; }
            public string? Method { get; set; }
            public Dictionary<string, string>? HeaderTemplates { get; set; }
            public Dictionary<string, string>? QueryTemplates { get; set; }
            public string? BodyJsonTemplate { get; set; }
            public string? BodyTextTemplate { get; set; }
            public string? ContentType { get; set; }
            public string? Charset { get; set; }
            public bool AllowBodyForGet { get; set; }
            public bool AllowBodyForDelete { get; set; }

            public string? AuthType { get; set; }
            public string? AuthTokenTemplate { get; set; }
            public string? AuthUsernameTemplate { get; set; }
            public string? AuthPasswordTemplate { get; set; }
            public string? AuthHeaderName { get; set; }
            public string? AuthQueryName { get; set; }
            public string? AuthTokenPrefix { get; set; }

            public string? ProxyUrl { get; set; }
            public string? ProxyUsername { get; set; }
            public string? ProxyPassword { get; set; }
            public List<string> ProxyBypassList { get; set; } = new();
            public bool UseSystemProxy { get; set; }
            public bool ProxyUseDefaultCredentials { get; set; }

            public bool AllowInvalidCerts { get; set; }
            public string? MinTls { get; set; }
            public string? ClientCertPath { get; set; }
            public string? ClientCertPassword { get; set; }

            public int? ConnectTimeoutSeconds { get; set; }
            public int? OverallTimeoutSeconds { get; set; }
            public int? LegacyTimeoutSeconds { get; set; }

            public bool RetryEnabled { get; set; }
            public int RetryMaxAttempts { get; set; } = 3;
            public int RetryBaseDelayMs { get; set; } = 250;
            public int RetryMaxDelayMs { get; set; } = 5000;
            public bool RetryJitter { get; set; } = true;
            public List<int> RetryOnStatus { get; set; } = new();
            public List<string> RetryOnExceptions { get; set; } = new();

            public string? PaginationType { get; set; }
            public string? PaginationCursorPath { get; set; }
            public string? PaginationNextParam { get; set; }
            public string? PaginationLimitParam { get; set; }
            public string? PaginationOffsetParam { get; set; }
            public string? PaginationPageParam { get; set; }
            public int? PaginationPageSize { get; set; }
            public int? PaginationMaxPages { get; set; }
            public int? PaginationStartPage { get; set; }
            public int? PaginationStartOffset { get; set; }

            public string? ResponseExpect { get; set; }
            public bool FlattenJson { get; set; } = true;
            public string? FlattenPrefix { get; set; }
            public int ResponseMaxBytes { get; set; } = 5 * 1024 * 1024;
            public bool IncludeHeaders { get; set; }
            public string? HeaderPrefix { get; set; } = "Header.";

            public Dictionary<string, string> OutputMappings { get; set; } = new();
            public Dictionary<string, string> OutputHeaderMappings { get; set; } = new();
            public bool IncludeRawBody { get; set; } = true;
            public string? RawBodyKey { get; set; } = "RawResponse";
        }
    }
}
