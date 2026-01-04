using Contextualizer.Core;
using Contextualizer.PluginContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WpfInteractionApp.Services
{
    public sealed class McpServerHost : IAsyncDisposable
    {
        private const string UiConfirmToolName = "ui_confirm";
        private const string UiUserInputsToolName = "ui_user_inputs";
        private const string UiNotifyToolName = "ui_notify";
        private const string UiShowMarkdownToolName = "ui_show_markdown";

        private readonly ConcurrentDictionary<string, SseSession> _sessions = new(StringComparer.Ordinal);
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private WebApplication? _app;
        private Task? _runTask;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _app != null;

        public int Port { get; private set; }

        public async Task StartAsync(int port, CancellationToken cancellationToken = default)
        {
            if (_app != null)
                throw new InvalidOperationException("MCP server is already running.");

            Port = port;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(McpServerHost).Assembly.FullName,
            });

            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

            var app = builder.Build();

            // Health endpoint
            app.MapGet("/mcp/health", () => Results.Json(new { ok = true, service = "contextualizer-mcp" }));

            // SSE endpoint
            app.MapGet("/mcp/sse", async (HttpContext httpContext) =>
            {
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";
                httpContext.Response.ContentType = "text/event-stream";

                var sessionId = Guid.NewGuid().ToString("N");
                var session = new SseSession(sessionId);
                _sessions[sessionId] = session;

                // Inform the client where to POST JSON-RPC messages
                await WriteSseEventAsync(httpContext, "endpoint", $"/mcp/message?sessionId={sessionId}", httpContext.RequestAborted);

                // Keep-alive heartbeat (some proxies/clients close idle SSE connections)
                var heartbeat = Task.Run(async () =>
                {
                    while (!httpContext.RequestAborted.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15), httpContext.RequestAborted);
                        await WriteSseCommentAsync(httpContext, "ping", httpContext.RequestAborted);
                    }
                }, httpContext.RequestAborted);

                try
                {
                    await foreach (var message in session.Reader.ReadAllAsync(httpContext.RequestAborted))
                    {
                        await WriteSseEventAsync(httpContext, "message", message, httpContext.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Client disconnected
                }
                finally
                {
                    session.TryComplete();
                    _sessions.TryRemove(sessionId, out _);
                    try { await heartbeat; } catch { /* ignore */ }
                }
            });

            // Message endpoint (client POSTs JSON-RPC requests here; responses are pushed via SSE)
            app.MapPost("/mcp/message", async (HttpContext httpContext) =>
            {
                var sessionId = httpContext.Request.Query["sessionId"].ToString();
                if (string.IsNullOrWhiteSpace(sessionId) || !_sessions.TryGetValue(sessionId, out var session))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    await httpContext.Response.WriteAsync("Unknown sessionId", httpContext.RequestAborted);
                    return;
                }

                string body;
                using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync(httpContext.RequestAborted);
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Empty request body", httpContext.RequestAborted);
                    return;
                }

                // MCP clients typically POST a single JSON-RPC object. We support that minimum.
                JsonRpcRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<JsonRpcRequest>(body, _jsonOptions);
                }
                catch (JsonException)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Invalid JSON", httpContext.RequestAborted);
                    return;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Method))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Invalid JSON-RPC request", httpContext.RequestAborted);
                    return;
                }

                // Notifications (no id) don't need responses
                var response = await HandleJsonRpcAsync(request);
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response, _jsonOptions);
                    await session.Writer.WriteAsync(json, httpContext.RequestAborted);
                }

                // Spec behavior: acknowledge receipt; actual response travels over SSE.
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
            });

            _app = app;
            // Start listening (non-blocking) and then keep a background task waiting for shutdown.
            await app.StartAsync(_cts.Token);
            _runTask = app.WaitForShutdownAsync(_cts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_app == null)
                return;

            try
            {
                _cts?.Cancel();
            }
            catch { /* ignore */ }

            foreach (var session in _sessions.Values)
            {
                session.TryComplete();
            }
            _sessions.Clear();

            try
            {
                await _app.StopAsync(cancellationToken);
            }
            catch { /* ignore */ }

            try
            {
                await _app.DisposeAsync();
            }
            catch { /* ignore */ }

            _app = null;
            _cts?.Dispose();
            _cts = null;

            if (_runTask != null)
            {
                try { await _runTask; } catch { /* ignore */ }
                _runTask = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        private async Task<JsonRpcResponse?> HandleJsonRpcAsync(JsonRpcRequest request)
        {
            // Notifications
            if (request.Id == null)
            {
                // Common client notification after initialize
                if (string.Equals(request.Method, "notifications/initialized", StringComparison.OrdinalIgnoreCase))
                    return null;

                return null;
            }

            try
            {
                return request.Method switch
                {
                    "initialize" => CreateInitializeResponse(request),
                    "tools/list" => await CreateToolsListResponseAsync(request),
                    "tools/call" => await CreateToolsCallResponseAsync(request),
                    _ => CreateMethodNotFound(request)
                };
            }
            catch (Exception ex)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32000,
                        Message = ex.Message
                    }
                };
            }
        }

        private JsonRpcResponse CreateInitializeResponse(JsonRpcRequest request)
        {
            // Minimal initialize payload for MCP clients
            var result = new
            {
                protocolVersion = "2024-11-05",
                serverInfo = new
                {
                    name = "Contextualizer",
                    version = "1.0.0"
                },
                capabilities = new
                {
                    tools = new { }
                }
            };

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }

        private async Task<JsonRpcResponse> CreateToolsListResponseAsync(JsonRpcRequest request)
        {
            var tools = new List<McpTool>();

            // Built-in UI tools (available by default)
            tools.Add(new McpTool
            {
                Name = UiConfirmToolName,
                Description = "Show a confirmation dialog to the user and return { confirmed: boolean }.",
                InputSchema = UiConfirmSchema()
            });

            tools.Add(new McpTool
            {
                Name = UiUserInputsToolName,
                Description = "Prompt the user for one or more inputs (wizard). Returns { cancelled: boolean, values: object }.",
                InputSchema = UiUserInputsSchema()
            });

            tools.Add(new McpTool
            {
                Name = UiNotifyToolName,
                Description = "Show a non-blocking notification/toast to the user (does not wait for input). Returns { ok: boolean }.",
                InputSchema = UiNotifySchema()
            });

            tools.Add(new McpTool
            {
                Name = UiShowMarkdownToolName,
                Description = "Show a markdown tab in the app (screen_id=markdown2). Returns { shown: boolean }.",
                InputSchema = UiShowMarkdownSchema()
            });

            var handlerManager = ServiceLocator.SafeGet<HandlerManager>();
            if (handlerManager == null)
            {
                // Still return built-in tools even if HandlerManager isn't available
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new McpToolsListResult { Tools = tools }
                };
            }

            var configs = handlerManager.GetAllHandlerConfigs()
                .Where(c => c.McpEnabled)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var cfg in configs)
            {
                var toolName = !string.IsNullOrWhiteSpace(cfg.McpToolName) ? cfg.McpToolName : Slugify(cfg.Name);
                var description =
                    !string.IsNullOrWhiteSpace(cfg.McpDescription) ? cfg.McpDescription :
                    !string.IsNullOrWhiteSpace(cfg.Description) ? cfg.Description :
                    !string.IsNullOrWhiteSpace(cfg.Title) ? cfg.Title :
                    $"{cfg.Type} handler";

                var schema = cfg.McpInputSchema ?? DefaultTextSchema();

                tools.Add(new McpTool
                {
                    Name = toolName,
                    Description = description,
                    InputSchema = schema
                });
            }

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsListResult { Tools = tools }
            };
        }

        private async Task<JsonRpcResponse> CreateToolsCallResponseAsync(JsonRpcRequest request)
        {
            var handlerManager = ServiceLocator.SafeGet<HandlerManager>();

            var callParams = request.Params.HasValue
                ? request.Params.Value.Deserialize<McpToolsCallParams>(_jsonOptions)
                : null;

            if (callParams == null || string.IsNullOrWhiteSpace(callParams.Name))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "Invalid params for tools/call" }
                };
            }

            // Built-in UI tools
            if (string.Equals(callParams.Name, UiConfirmToolName, StringComparison.OrdinalIgnoreCase))
            {
                return await HandleUiConfirmAsync(request, callParams);
            }

            if (string.Equals(callParams.Name, UiUserInputsToolName, StringComparison.OrdinalIgnoreCase))
            {
                return await HandleUiUserInputsAsync(request, callParams);
            }

            if (string.Equals(callParams.Name, UiNotifyToolName, StringComparison.OrdinalIgnoreCase))
            {
                return await HandleUiNotifyAsync(request, callParams);
            }

            if (string.Equals(callParams.Name, UiShowMarkdownToolName, StringComparison.OrdinalIgnoreCase))
            {
                return await HandleUiShowMarkdownAsync(request, callParams);
            }

            if (handlerManager == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32001, Message = "HandlerManager is not available" }
                };
            }

            // Find matching handler by mcp_tool_name or slug(name)
            var configs = handlerManager.GetAllHandlerConfigs();
            var matchedConfig = configs.FirstOrDefault(c =>
                c.McpEnabled &&
                (string.Equals(c.McpToolName, callParams.Name, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Slugify(c.Name), callParams.Name, StringComparison.OrdinalIgnoreCase)));

            if (matchedConfig == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32004, Message = $"Tool not found: {callParams.Name}" }
                };
            }

            // Convert arguments into string dictionary for template expansion + optional seed context
            var argsDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    argsDict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.ToString();
                }
            }

            // Build input text
            var inputText = BuildInputText(matchedConfig, argsDict);

            var clipboard = new ClipboardContent
            {
                Success = true,
                IsText = true,
                Text = inputText
            };

            // Create a fresh handler instance per call to avoid cross-trigger concurrency issues
            var handler = Contextualizer.Core.HandlerFactory.Create(matchedConfig);
            if (handler == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32005, Message = $"Failed to create handler for type: {matchedConfig.Type}" }
                };
            }

            try
            {
                if (handler is not Dispatch dispatch)
                {
                    // Fallback: execute without result
                    await handler.Execute(clipboard);
                    var fallbackJson = JsonSerializer.Serialize(new Dictionary<string, string> { [ContextKey._formatted_output] = string.Empty }, _jsonOptions);
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new McpToolsCallResult
                        {
                            Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = fallbackJson } },
                            IsError = false
                        }
                    };
                }

                // Seed MCP trigger source for conditional actions (e.g., skip show_window when called via MCP).
                var seedContext = new Dictionary<string, string>(argsDict, StringComparer.OrdinalIgnoreCase)
                {
                    [ContextKey._trigger] = "mcp"
                };

                var execResult = await dispatch.ExecuteWithResultAsync(clipboard, seedContext: seedContext);

                var payload = BuildReturnPayload(matchedConfig, execResult);
                var text = JsonSerializer.Serialize(payload, _jsonOptions);

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new McpToolsCallResult
                    {
                        Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                        IsError = false
                    }
                };
            }
            finally
            {
                if (handler is IDisposable disposable)
                {
                    try { disposable.Dispose(); } catch { /* ignore */ }
                }
            }
        }

        private static Dictionary<string, string> BuildReturnPayload(HandlerConfig config, DispatchExecutionResult execResult)
        {
            // Default: only formatted output
            if (config.McpReturnKeys == null || config.McpReturnKeys.Count == 0)
            {
                return new Dictionary<string, string>
                {
                    [ContextKey._formatted_output] = execResult.FormattedOutput ?? string.Empty
                };
            }

            var context = execResult.Context ?? new Dictionary<string, string>();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in config.McpReturnKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (context.TryGetValue(key, out var value))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        private async Task<JsonRpcResponse> HandleUiConfirmAsync(JsonRpcRequest request, McpToolsCallParams callParams)
        {
            var ui = ServiceLocator.SafeGet<IUserInteractionService>();
            if (ui == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            string title = "Confirmation";
            string message = string.Empty;

            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("title"))
                        title = prop.Value.GetString() ?? title;
                    else if (prop.NameEquals("message"))
                        message = prop.Value.GetString() ?? string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_confirm requires arguments.message" }
                };
            }

            bool confirmed = await ui.ShowConfirmationAsync(title, message);
            var payload = new Dictionary<string, object> { ["confirmed"] = confirmed };
            var text = JsonSerializer.Serialize(payload, _jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        private async Task<JsonRpcResponse> HandleUiUserInputsAsync(JsonRpcRequest request, McpToolsCallParams callParams)
        {
            var ui = ServiceLocator.SafeGet<IUserInteractionService>();
            if (ui == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            UiUserInputsArgs? args = null;
            try
            {
                if (callParams.Arguments.HasValue)
                    args = callParams.Arguments.Value.Deserialize<UiUserInputsArgs>(_jsonOptions);
            }
            catch (JsonException)
            {
                // ignore, handled below
            }

            if (args == null || args.UserInputs == null || args.UserInputs.Count == 0)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_user_inputs requires arguments.user_inputs (non-empty array)" }
                };
            }

            var context = args.Context ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Use the same navigation prompt logic as handlers
            var processor = new HandlerContextProcessor();
            bool completed = processor.PromptUserInputsWithNavigation(args.UserInputs, context);

            var payload = new Dictionary<string, object>
            {
                ["cancelled"] = !completed,
                ["values"] = context
            };

            var text = JsonSerializer.Serialize(payload, _jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        private async Task<JsonRpcResponse> HandleUiNotifyAsync(JsonRpcRequest request, McpToolsCallParams callParams)
        {
            var ui = ServiceLocator.SafeGet<IUserInteractionService>();
            if (ui == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            string title = string.Empty;
            string message = string.Empty;
            string level = "info";
            int durationSeconds = 5;

            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("title"))
                        title = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("message"))
                        message = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("level"))
                        level = prop.Value.GetString() ?? level;
                    else if (prop.NameEquals("durationSeconds") || prop.NameEquals("duration_seconds"))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var v))
                            durationSeconds = v;
                        else if (int.TryParse(prop.Value.ToString(), out var parsed))
                            durationSeconds = parsed;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_notify requires arguments.message" }
                };
            }

            if (durationSeconds < 1) durationSeconds = 1;
            if (durationSeconds > 600) durationSeconds = 600;

            var logType = level.Trim().ToLowerInvariant() switch
            {
                "success" => LogType.Success,
                "warn" => LogType.Warning,
                "warning" => LogType.Warning,
                "error" => LogType.Error,
                "critical" => LogType.Critical,
                "debug" => LogType.Debug,
                _ => LogType.Info
            };

            // Non-blocking: shows toast and returns immediately (no user input required).
            ui.ShowNotification(message, logType, title, durationSeconds, null);

            var payload = new Dictionary<string, object> { ["ok"] = true };
            var text = JsonSerializer.Serialize(payload, _jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        private async Task<JsonRpcResponse> HandleUiShowMarkdownAsync(JsonRpcRequest request, McpToolsCallParams callParams)
        {
            var ui = ServiceLocator.SafeGet<IUserInteractionService>();
            if (ui == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            string title = "Markdown";
            string markdown = string.Empty;
            bool autoFocus = false;
            bool bringToFront = false;

            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("title"))
                        title = prop.Value.GetString() ?? title;
                    else if (prop.NameEquals("markdown"))
                        markdown = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("autoFocus") || prop.NameEquals("auto_focus"))
                        autoFocus = prop.Value.ValueKind == JsonValueKind.True || (prop.Value.ValueKind == JsonValueKind.String && bool.TryParse(prop.Value.GetString(), out var b1) && b1);
                    else if (prop.NameEquals("bringToFront") || prop.NameEquals("bring_to_front"))
                        bringToFront = prop.Value.ValueKind == JsonValueKind.True || (prop.Value.ValueKind == JsonValueKind.String && bool.TryParse(prop.Value.GetString(), out var b2) && b2);
                }
            }

            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_show_markdown requires arguments.markdown" }
                };
            }

            var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ContextKey._body] = markdown
            };

            ui.ShowWindow("markdown2", title, context, null, autoFocus, bringToFront);

            var payload = new Dictionary<string, object> { ["shown"] = true };
            var text = JsonSerializer.Serialize(payload, _jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        private static string BuildInputText(HandlerConfig config, Dictionary<string, string> argsDict)
        {
            if (!string.IsNullOrWhiteSpace(config.McpInputTemplate))
            {
                return HandlerContextProcessor.ReplaceDynamicValues(config.McpInputTemplate, argsDict);
            }

            if (argsDict.TryGetValue("text", out var text))
                return text;

            // If no template and no text arg, fall back to JSON string of args
            return JsonSerializer.Serialize(argsDict);
        }

        private static JsonElement DefaultTextSchema()
        {
            var schemaJson = """
            {
              "type": "object",
              "properties": {
                "text": { "type": "string" }
              },
              "required": ["text"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement UiConfirmSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "message": { "type": "string" }
              },
              "required": ["message"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement UiUserInputsSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "context": { "type": "object", "additionalProperties": { "type": "string" } },
                "user_inputs": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "key": { "type": "string" },
                      "title": { "type": "string" },
                      "message": { "type": "string" },
                      "validation_regex": { "type": "string" },
                      "is_required": { "type": "boolean" },
                      "is_selection_list": { "type": "boolean" },
                      "is_password": { "type": "boolean" },
                      "is_multi_select": { "type": "boolean" },
                      "is_file_picker": { "type": "boolean" },
                      "is_multi_line": { "type": "boolean" },
                      "default_value": { "type": "string" },
                      "selection_items": {
                        "type": "array",
                        "items": {
                          "type": "object",
                          "properties": {
                            "value": { "type": "string" },
                            "display": { "type": "string" }
                          },
                          "required": ["value", "display"]
                        }
                      }
                    },
                    "required": ["key", "title", "message"]
                  }
                }
              },
              "required": ["user_inputs"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement UiNotifySchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "message": { "type": "string" },
                "level": { "type": "string" },
                "durationSeconds": { "type": "integer" }
              },
              "required": ["message"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement UiShowMarkdownSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "markdown": { "type": "string" },
                "autoFocus": { "type": "boolean" },
                "bringToFront": { "type": "boolean" }
              },
              "required": ["markdown"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "tool";

            var sb = new StringBuilder(name.Length);
            foreach (var ch in name.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    continue;
                }

                if (ch == ' ' || ch == '-' || ch == '_' || ch == '.')
                {
                    if (sb.Length == 0 || sb[^1] == '_') continue;
                    sb.Append('_');
                }
            }

            var result = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "tool" : result;
        }

        private static JsonRpcResponse CreateMethodNotFound(JsonRpcRequest request)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = -32601,
                    Message = $"Method not found: {request.Method}"
                }
            };
        }

        private static async Task WriteSseEventAsync(HttpContext context, string eventName, string data, CancellationToken cancellationToken)
        {
            await context.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {data}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        private static async Task WriteSseCommentAsync(HttpContext context, string comment, CancellationToken cancellationToken)
        {
            await context.Response.WriteAsync($": {comment}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        private sealed class SseSession
        {
            private readonly System.Threading.Channels.Channel<string> _channel;
            public string SessionId { get; }

            public SseSession(string sessionId)
            {
                SessionId = sessionId;
                _channel = System.Threading.Channels.Channel.CreateUnbounded<string>(
                    new System.Threading.Channels.UnboundedChannelOptions
                    {
                        AllowSynchronousContinuations = false,
                        SingleReader = true,
                        SingleWriter = false
                    });
            }

            public System.Threading.Channels.ChannelReader<string> Reader => _channel.Reader;
            public System.Threading.Channels.ChannelWriter<string> Writer => _channel.Writer;

            public void TryComplete()
            {
                try { _channel.Writer.TryComplete(); } catch { /* ignore */ }
            }
        }

        private sealed class JsonRpcRequest
        {
            [JsonPropertyName("jsonrpc")]
            public string? JsonRpc { get; set; }

            [JsonPropertyName("id")]
            public JsonElement? Id { get; set; }

            [JsonPropertyName("method")]
            public string Method { get; set; } = string.Empty;

            [JsonPropertyName("params")]
            public JsonElement? Params { get; set; }
        }

        private sealed class JsonRpcResponse
        {
            [JsonPropertyName("jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";

            [JsonPropertyName("id")]
            public JsonElement? Id { get; set; }

            [JsonPropertyName("result")]
            public object? Result { get; set; }

            [JsonPropertyName("error")]
            public JsonRpcError? Error { get; set; }
        }

        private sealed class JsonRpcError
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;
        }

        private sealed class McpTool
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("inputSchema")]
            public JsonElement InputSchema { get; set; }
        }

        private sealed class McpToolsListResult
        {
            [JsonPropertyName("tools")]
            public List<McpTool> Tools { get; set; } = new();
        }

        private sealed class McpToolsCallParams
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("arguments")]
            public JsonElement? Arguments { get; set; }
        }

        private sealed class UiUserInputsArgs
        {
            [JsonPropertyName("context")]
            public Dictionary<string, string>? Context { get; set; }

            [JsonPropertyName("user_inputs")]
            public List<UserInputRequest> UserInputs { get; set; } = new();
        }

        private sealed class McpToolsCallResult
        {
            [JsonPropertyName("content")]
            public List<McpContentItem> Content { get; set; } = new();

            [JsonPropertyName("isError")]
            public bool IsError { get; set; } = false;
        }

        private sealed class McpContentItem
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "text";

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}


