using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Contextualizer.Core.Handlers.Api;
using Contextualizer.Core.Handlers.Api.Models;

namespace Contextualizer.Core
{
    public class ApiHandler : Dispatch, IHandler, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Regex? _optionalRegex;
        private readonly NormalizedHttpConfig _config;
        private bool _disposed = false;

        public static string TypeName => "Api";
        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        public ApiHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            if (!string.IsNullOrWhiteSpace(handlerConfig.Regex))
            {
                try
                {
                    _optionalRegex = new Regex(
                        handlerConfig.Regex,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(5)
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

            _config = HttpConfigNormalizer.NormalizeConfig(handlerConfig);
            _httpClient = HttpClientFactory.CreateHttpClient(_config);
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
                return false;

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

            if (_optionalRegex != null)
            {
                try
                {
                    var match = _optionalRegex.Match(clipboardContent.Text);

                    if (match.Success)
                    {
                        context[ContextKey._match] = match.Value;

                        if (HandlerConfig.Groups != null && HandlerConfig.Groups.Count > 0)
                        {
                            for (int i = 0; i < HandlerConfig.Groups.Count; i++)
                            {
                                var groupName = HandlerConfig.Groups[i];
                                string groupValue;

                                var namedGroup = match.Groups[groupName];
                                if (namedGroup.Success)
                                {
                                    groupValue = namedGroup.Value;
                                }
                                else
                                {
                                    var groupIndex = i + 1;
                                    groupValue = match.Groups.Count > groupIndex ? match.Groups[groupIndex].Value : string.Empty;
                                }

                                context[groupName] = groupValue;
                            }
                        }
                        else
                        {
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
                await HttpPaginationHandler.ExecuteHttpFlowAsync(
                    _httpClient,
                    _config,
                    context,
                    (ctx, extraQuery) => HttpRequestBuilder.BuildRequestMessage(_config, ctx, extraQuery),
                    async (request) => await HttpRetryHandler.SendWithRetryAsync(() => _httpClient.SendAsync(request), _config)
                );
            }
            catch (Exception ex)
            {
                context["Error"] = ex.Message;
                context[ContextKey._error] = ex.Message;
                UserFeedback.ShowError($"API request failed: {ex.Message}");
            }

            return context;
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
