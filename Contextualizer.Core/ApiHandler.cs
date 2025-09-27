using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Regex _urlParameterRegex;
        private readonly HandlerContextProcessor _contextProcessor;
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
            _urlParameterRegex = new Regex(@"\$\(([^)]+)\)", RegexOptions.Compiled);
            _contextProcessor = new HandlerContextProcessor();
            
            // Create HttpClient with optimized settings for long-running applications
            _httpClient = CreateOptimizedHttpClient(handlerConfig);
        }

        private HttpClient CreateOptimizedHttpClient(HandlerConfig handlerConfig)
        {
            // Create SocketsHttpHandler with optimized settings for long-running applications
            var handler = new SocketsHttpHandler()
            {
                // Connection pooling settings
                MaxConnectionsPerServer = 10, // Limit concurrent connections per server
                PooledConnectionLifetime = TimeSpan.FromMinutes(15), // Refresh connections every 15 minutes
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5), // Close idle connections after 5 minutes
            };

            var httpClient = new HttpClient(handler);
            
            // Set timeout if configured, otherwise use a reasonable default
            httpClient.Timeout = handlerConfig.TimeoutSeconds.HasValue 
                ? TimeSpan.FromSeconds(handlerConfig.TimeoutSeconds.Value)
                : TimeSpan.FromSeconds(30);

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

            // Add custom headers
            if (handlerConfig.Headers != null)
            {
                foreach (var header in handlerConfig.Headers)
                {
                    if (!string.IsNullOrEmpty(header.Key))
                    {
                        try
                        {
                            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                        catch (Exception ex)
                        {
                            UserFeedback.ShowWarning($"Failed to add header {header.Key}: {ex.Message}");
                        }
                    }
                }
            }

            return httpClient;
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
                return false;

            if (!string.IsNullOrWhiteSpace(HandlerConfig.Regex))
            {
                var regex = new Regex(HandlerConfig.Regex);
                return regex.IsMatch(clipboardContent.Text);
            }

            return true;
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = clipboardContent.Text;

            if (!string.IsNullOrWhiteSpace(HandlerConfig.Regex))
            {
                var regex = new Regex(HandlerConfig.Regex);
                var match = regex.Match(clipboardContent.Text);

                if (match.Success && HandlerConfig.Groups != null)
                {
                    for (int i = 0; i < HandlerConfig.Groups.Count; i++)
                    {
                        // Groups[0] is always the full match, actual capturing groups start from index 1
                        var groupValue = match.Groups.Count > i + 1 ? match.Groups[i + 1].Value : match.Groups[0].Value;
                        context[HandlerConfig.Groups[i]] = groupValue;
                    }
                }
            }

            try
            {
                string url = HandlerContextProcessor.ReplaceDynamicValues(HandlerConfig.Url, context);
                if (string.IsNullOrWhiteSpace(url))
                {
                    throw new InvalidOperationException("URL cannot be empty after placeholder replacement");
                }

                var request = new HttpRequestMessage(new HttpMethod(HandlerConfig.Method ?? "GET"), url);

                if (HandlerConfig.RequestBody.HasValue)
                {
                    // Get JSON as string and replace dynamic values
                    string jsonString = HandlerConfig.RequestBody.Value.GetRawText();
                    string body = HandlerContextProcessor.ReplaceDynamicValues(jsonString, context);
                    request.Content = new StringContent(body, Encoding.UTF8, HandlerConfig.ContentType ?? "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                context["StatusCode"] = ((int)response.StatusCode).ToString();
                context["IsSuccessful"] = response.IsSuccessStatusCode.ToString();

                string responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
                {
                    try
                    {
                        using var document = JsonDocument.Parse(responseContent);
                        // Store raw response directly without additional serialization
                        context["RawResponse"] = responseContent;
                        FlattenJsonToContext(document.RootElement, "", context);
                    }
                    catch (JsonException ex)
                    {
                        context["RawResponse"] = responseContent;
                        context["JsonParseError"] = ex.Message;
                        UserFeedback.ShowError($"Failed to parse JSON response: {ex.Message}");
                    }
                }
                else
                {
                    context["RawResponse"] = responseContent;
                }
            }
            catch (Exception ex)
            {
                context["Error"] = ex.Message;
                UserFeedback.ShowError($"API request failed: {ex.Message}");
            }

            return context;
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
    }
}
