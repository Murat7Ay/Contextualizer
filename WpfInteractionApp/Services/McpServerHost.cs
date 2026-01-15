using Contextualizer.Core;
using Contextualizer.Core.Services;
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

        // Management tools (gated by settings.mcp_settings.management_tools_enabled)
        private const string HandlersListToolName = "handlers_list";
        private const string HandlersGetToolName = "handlers_get";
        private const string HandlerAddToolName = "handler_add";
        private const string HandlerUpdateToolName = "handler_update";
        private const string HandlerDeleteToolName = "handler_delete";
        private const string HandlerReloadToolName = "handler_reload";
        private const string PluginsListToolName = "plugins_list";
        private const string ConfigGetKeysToolName = "config_get_keys";
        private const string ConfigGetSectionToolName = "config_get_section";
        private const string ConfigSetValueToolName = "config_set_value";
        private const string ConfigReloadToolName = "config_reload";
        private const string HandlerDocsToolName = "handler_docs";

        private readonly ConcurrentDictionary<string, SseSession> _sessions = new(StringComparer.Ordinal);
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private WebApplication? _app;
        
        /// <summary>
        /// Gets the appropriate user interaction service based on MCP settings.
        /// Returns NativeUserInteractionService when use_native_ui is true, otherwise WebViewUserInteractionService.
        /// </summary>
        private static IUserInteractionService GetUserInteractionService()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            var useNative = settings?.Settings.McpSettings?.UseNativeUi ?? true;

            if (useNative)
            {
                return ServiceLocator.SafeGet<NativeUserInteractionService>() 
                    ?? ServiceLocator.SafeGet<IUserInteractionService>()
                    ?? throw new InvalidOperationException("No user interaction service available");
            }

            return ServiceLocator.SafeGet<WebViewUserInteractionService>() 
                ?? ServiceLocator.SafeGet<IUserInteractionService>()
                ?? throw new InvalidOperationException("No user interaction service available");
        }

        private static bool IsManagementToolsEnabled()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            return settings?.Settings?.McpSettings?.ManagementToolsEnabled == true;
        }

        private static HandlerConfigStore? TryCreateHandlerConfigStore()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            if (settings == null) return null;
            return new HandlerConfigStore(settings);
        }

        /// <summary>
        /// Prompts user inputs with navigation using the specified UI service.
        /// This bypasses HandlerContextProcessor which always uses ServiceLocator.
        /// </summary>
        private static bool PromptUserInputsWithNavigation(IUserInteractionService ui, List<UserInputRequest> userInputs, Dictionary<string, string> context)
        {
            if (userInputs == null || userInputs.Count == 0)
                return true;

            int currentIndex = 0;

            while (currentIndex < userInputs.Count)
            {
                var input = userInputs[currentIndex];

                if (string.IsNullOrWhiteSpace(input?.Key))
                {
                    currentIndex++;
                    continue;
                }

                // Handle dependent selection items
                if (!string.IsNullOrEmpty(input.DependentKey) &&
                    context.TryGetValue(input.DependentKey, out var dependentValue) &&
                    input.DependentSelectionItemMap?.TryGetValue(dependentValue, out var dependentSelection) == true)
                {
                    input.SelectionItems = dependentSelection.SelectionItems;
                    input.DefaultValue = dependentSelection.DefaultValue;
                }

                var result = ui.GetUserInputWithNavigation(input, context, currentIndex > 0, currentIndex, userInputs.Count);

                switch (result.Action)
                {
                    case NavigationAction.Next:
                        if (!string.IsNullOrWhiteSpace(result.Value))
                        {
                            context[input.Key] = result.Value;
                        }
                        currentIndex++;
                        break;

                    case NavigationAction.Back:
                        if (currentIndex > 0)
                        {
                            currentIndex--;
                            context.Remove(input.Key);
                        }
                        break;

                    case NavigationAction.Cancel:
                        return false;
                }
            }

            return true;
        }

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
                Description = """
Prompt user for inputs. Each input in user_inputs array MUST include the appropriate type flag:
- Text input: just key, title, message
- Password: add "is_password": true  
- File picker: add "is_file_picker": true
- Multi-line: add "is_multi_line": true
- Dropdown: add "is_selection_list": true and "selection_items": [{"value":"v1","display":"Option 1"}]
- Multi-select: add "is_multi_select": true with is_selection_list

Example for file picker: {"key":"file","title":"Select","message":"Pick file","is_file_picker":true}
Returns { cancelled: boolean, values: object }.
""",
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

            if (handlerManager != null)
            {
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

                    var schema = cfg.McpInputSchema ?? DefaultSchemaForHandler(cfg);

                    tools.Add(new McpTool
                    {
                        Name = toolName,
                        Description = description,
                        InputSchema = schema
                    });
                }
            }

            if (IsManagementToolsEnabled())
            {
                tools.Add(new McpTool { Name = HandlersListToolName, Description = "List handlers from handlers.json (optionally include full configs).", InputSchema = HandlersListSchema() });
                tools.Add(new McpTool { Name = HandlersGetToolName, Description = "Get a single handler config by name.", InputSchema = HandlersGetSchema() });
                tools.Add(new McpTool { Name = HandlerAddToolName, Description = "Add a new handler to handlers.json and optionally reload handlers.", InputSchema = HandlerAddSchema() });
                tools.Add(new McpTool { Name = HandlerUpdateToolName, Description = "Update an existing handler by name (partial update) and optionally reload handlers.", InputSchema = HandlerUpdateSchema() });
                tools.Add(new McpTool { Name = HandlerDeleteToolName, Description = "Delete an existing handler by name and optionally reload handlers.", InputSchema = HandlerDeleteSchema() });
                tools.Add(new McpTool { Name = HandlerReloadToolName, Description = "Reload handlers from handlers.json (optionally reload plugins).", InputSchema = HandlerReloadSchema() });
                tools.Add(new McpTool { Name = PluginsListToolName, Description = "List loaded plugin names (actions/validators/context_providers) and registered handler types.", InputSchema = EmptyObjectSchema() });
                tools.Add(new McpTool { Name = ConfigGetKeysToolName, Description = "List config keys (section.key).", InputSchema = EmptyObjectSchema() });
                tools.Add(new McpTool { Name = ConfigGetSectionToolName, Description = "Get a config section as key-value pairs (values may be masked).", InputSchema = ConfigGetSectionSchema() });
                tools.Add(new McpTool { Name = ConfigSetValueToolName, Description = "Set a config value in config.ini or secrets.ini.", InputSchema = ConfigSetValueSchema() });
                tools.Add(new McpTool { Name = ConfigReloadToolName, Description = "Reload config files from disk.", InputSchema = EmptyObjectSchema() });
                tools.Add(new McpTool { Name = HandlerDocsToolName, Description = "Handler authoring guide and examples (templating, conditions, seeder, MCP).", InputSchema = HandlerDocsSchema() });
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

            // Management tools (gated)
            if (IsManagementToolsEnabled())
            {
                var mgmt = await TryHandleManagementToolAsync(request, callParams, handlerManager);
                if (mgmt != null)
                    return mgmt;
            }

            // Find matching handler by mcp_tool_name or slug(name)
            if (handlerManager == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32001, Message = "HandlerManager is not available" }
                };
            }

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
            string[]? filesArg = null;
            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("files") && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var s = item.GetString();
                                if (!string.IsNullOrWhiteSpace(s))
                                    list.Add(s);
                            }
                            else
                            {
                                var s = item.ToString();
                                if (!string.IsNullOrWhiteSpace(s))
                                    list.Add(s);
                            }
                        }
                        filesArg = list.Count > 0 ? list.ToArray() : null;
                        // Also keep a stringified form in argsDict for completeness/debugging
                        argsDict[prop.Name] = prop.Value.ToString();
                        continue;
                    }

                    argsDict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.ToString();
                }
            }

            // Build input text
            var inputText = BuildInputText(matchedConfig, argsDict);

            ClipboardContent clipboard;
            if (filesArg != null && filesArg.Length > 0)
            {
                clipboard = new ClipboardContent
                {
                    Success = true,
                    IsFile = true,
                    Files = filesArg,
                    IsText = false,
                    Text = string.Empty
                };
            }
            else
            {
                clipboard = new ClipboardContent
                {
                    Success = true,
                    IsText = true,
                    Text = inputText
                };
            }

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

                if (!execResult.Processed)
                {
                    var message =
                        (execResult.Context != null && execResult.Context.TryGetValue(ContextKey._error, out var err) && !string.IsNullOrWhiteSpace(err))
                            ? err
                            : execResult.FormattedOutput ?? "Handler did not process input.";

                    var errorPayload = new Dictionary<string, object?>
                    {
                        ["error"] = message,
                        ["can_handle"] = execResult.CanHandle,
                        ["cancelled"] = execResult.Cancelled,
                        ["formatted_output"] = execResult.FormattedOutput ?? string.Empty
                    };

                    var errorText = JsonSerializer.Serialize(errorPayload, _jsonOptions);
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new McpToolsCallResult
                        {
                            Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = errorText } },
                            IsError = true
                        }
                    };
                }

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

        private async Task<JsonRpcResponse?> TryHandleManagementToolAsync(JsonRpcRequest request, McpToolsCallParams callParams, HandlerManager? handlerManager)
        {
            // If name doesn't match our management tool set, skip.
            var name = callParams.Name ?? string.Empty;
            if (!string.Equals(name, HandlersListToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, HandlersGetToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, HandlerAddToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, HandlerUpdateToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, HandlerDeleteToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, HandlerReloadToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, PluginsListToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, ConfigGetKeysToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, ConfigGetSectionToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, ConfigSetValueToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, ConfigReloadToolName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(name, HandlerDocsToolName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var args = callParams.Arguments.HasValue ? callParams.Arguments.Value : default;

            try
            {
                if (string.Equals(name, HandlersListToolName, StringComparison.OrdinalIgnoreCase))
                {
                    var store = TryCreateHandlerConfigStore();
                    if (store == null) return ToolError(request, "HandlerConfigStore not available");

                    bool includeConfigs = false;
                    if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("include_configs", out var ic) && ic.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        includeConfigs = ic.ValueKind == JsonValueKind.True;

                    var all = await store.ReadAllAsync();
                    if (includeConfigs)
                    {
                        return ToolOk(request, new { success = true, handlers = all });
                    }

                    return ToolOk(request, new
                    {
                        success = true,
                        handlers = all.Select(h => new
                        {
                            name = h.Name,
                            type = h.Type,
                            description = h.Description,
                            enabled = h.Enabled,
                            mcp_enabled = h.McpEnabled
                        }).ToList()
                    });
                }

                if (string.Equals(name, HandlersGetToolName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String)
                        return ToolError(request, "handlers_get requires arguments.name");

                    var store = TryCreateHandlerConfigStore();
                    if (store == null) return ToolError(request, "HandlerConfigStore not available");

                    var cfg = await store.GetByNameAsync(n.GetString() ?? string.Empty);
                    if (cfg == null) return ToolError(request, "Handler not found");
                    return ToolOk(request, new { success = true, handler = cfg });
                }

                if (string.Equals(name, HandlerAddToolName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_config", out var hc) || hc.ValueKind != JsonValueKind.Object)
                        return ToolError(request, "handler_add requires arguments.handler_config (object)");

                    bool reload = true;
                    if (args.TryGetProperty("reload_after_add", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        reload = ra.ValueKind == JsonValueKind.True;

                    var store = TryCreateHandlerConfigStore();
                    if (store == null) return ToolError(request, "HandlerConfigStore not available");

                    var cfg = hc.Deserialize<HandlerConfig>(_jsonOptions);
                    if (cfg == null) return ToolError(request, "Invalid handler_config JSON");

                    var res = await store.AddAsync(cfg);
                    if (!res.Success) return ToolError(request, $"{res.Code}: {res.Error}");

                    if (reload && handlerManager != null)
                        handlerManager.ReloadHandlers(reloadPlugins: false);

                    return ToolOk(request, new { success = true, name = cfg.Name, type = cfg.Type });
                }

                if (string.Equals(name, HandlerUpdateToolName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_name", out var hn) || hn.ValueKind != JsonValueKind.String)
                        return ToolError(request, "handler_update requires arguments.handler_name");
                    if (!args.TryGetProperty("updates", out var up) || up.ValueKind != JsonValueKind.Object)
                        return ToolError(request, "handler_update requires arguments.updates (object)");

                    bool reload = true;
                    if (args.TryGetProperty("reload_after_update", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        reload = ra.ValueKind == JsonValueKind.True;

                    var store = TryCreateHandlerConfigStore();
                    if (store == null) return ToolError(request, "HandlerConfigStore not available");

                    var res = await store.UpdatePartialAsync(hn.GetString() ?? string.Empty, up);
                    if (!res.Success) return ToolError(request, $"{res.Code}: {res.Error}");

                    if (reload && handlerManager != null)
                        handlerManager.ReloadHandlers(reloadPlugins: false);

                    return ToolOk(request, new { success = true, name = hn.GetString(), updated_fields = res.Payload?.UpdatedFields ?? new List<string>() });
                }

                if (string.Equals(name, HandlerDeleteToolName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_name", out var hn) || hn.ValueKind != JsonValueKind.String)
                        return ToolError(request, "handler_delete requires arguments.handler_name");

                    bool reload = true;
                    if (args.TryGetProperty("reload_after_delete", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        reload = ra.ValueKind == JsonValueKind.True;

                    var store = TryCreateHandlerConfigStore();
                    if (store == null) return ToolError(request, "HandlerConfigStore not available");

                    var res = await store.DeleteAsync(hn.GetString() ?? string.Empty);
                    if (!res.Success) return ToolError(request, $"{res.Code}: {res.Error}");

                    if (reload && handlerManager != null)
                        handlerManager.ReloadHandlers(reloadPlugins: false);

                    return ToolOk(request, new { success = true, name = hn.GetString() });
                }

                if (string.Equals(name, HandlerReloadToolName, StringComparison.OrdinalIgnoreCase))
                {
                    bool reloadPlugins = false;
                    if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("reload_plugins", out var rp) && rp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        reloadPlugins = rp.ValueKind == JsonValueKind.True;

                    if (handlerManager == null) return ToolError(request, "HandlerManager is not available");

                    var (handlersReloaded, newPluginsLoaded) = handlerManager.ReloadHandlers(reloadPlugins);
                    return ToolOk(request, new { success = true, handlers_reloaded = handlersReloaded, new_plugins_loaded = newPluginsLoaded });
                }

                if (string.Equals(name, HandlerDocsToolName, StringComparison.OrdinalIgnoreCase))
                {
                    bool showUi = false;
                    string title = "Handler Authoring Guide";
                    bool autoFocus = false;
                    bool bringToFront = false;

                    if (args.ValueKind == JsonValueKind.Object)
                    {
                        if (args.TryGetProperty("show_ui", out var s) && s.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            showUi = s.ValueKind == JsonValueKind.True;
                        if (args.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                            title = t.GetString() ?? title;
                        if (args.TryGetProperty("auto_focus", out var af) && af.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            autoFocus = af.ValueKind == JsonValueKind.True;
                        if (args.TryGetProperty("bring_to_front", out var bf) && bf.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            bringToFront = bf.ValueKind == JsonValueKind.True;
                    }

                    var markdown = BuildHandlerDocsMarkdown();
                    if (showUi)
                    {
                        ShowMarkdownTab(title, markdown, autoFocus, bringToFront);
                    }

                    return ToolOk(request, new { success = true, markdown });
                }

                if (string.Equals(name, PluginsListToolName, StringComparison.OrdinalIgnoreCase))
                {
                    var actionService = ServiceLocator.SafeGet<IActionService>();
                    var concrete = actionService as ActionService;

                    var payload = new
                    {
                        success = true,
                        handler_types = HandlerFactory.GetRegisteredTypeNames(),
                        actions = concrete?.GetActionNames() ?? Array.Empty<string>(),
                        validators = concrete?.GetValidatorNames() ?? Array.Empty<string>(),
                        context_providers = concrete?.GetContextProviderNames() ?? Array.Empty<string>(),
                    };

                    return ToolOk(request, payload);
                }

                if (string.Equals(name, ConfigGetKeysToolName, StringComparison.OrdinalIgnoreCase))
                {
                    var cfg = ServiceLocator.SafeGet<IConfigurationService>();
                    if (cfg == null) return ToolError(request, "IConfigurationService is not available");

                    var keys = cfg.GetAllKeys();
                    return ToolOk(request, new { success = true, keys });
                }

                if (string.Equals(name, ConfigGetSectionToolName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("section", out var s) || s.ValueKind != JsonValueKind.String)
                        return ToolError(request, "config_get_section requires arguments.section");

                    var cfg = ServiceLocator.SafeGet<IConfigurationService>();
                    if (cfg == null) return ToolError(request, "IConfigurationService is not available");

                    var section = s.GetString() ?? string.Empty;
                    var values = cfg.GetSection(section);

                    // Mask common sensitive sections by default
                    var masked = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var shouldMaskAll = section.Equals("api_keys", StringComparison.OrdinalIgnoreCase) ||
                                        section.Equals("credentials", StringComparison.OrdinalIgnoreCase) ||
                                        section.Equals("connections", StringComparison.OrdinalIgnoreCase);
                    foreach (var kvp in values)
                    {
                        if (shouldMaskAll || kvp.Key.Contains("token", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("key", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("password", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                            masked[kvp.Key] = "***";
                        else
                            masked[kvp.Key] = kvp.Value;
                    }

                    return ToolOk(request, new { success = true, section, values = masked });
                }

                if (string.Equals(name, ConfigSetValueToolName, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.ValueKind != JsonValueKind.Object) return ToolError(request, "config_set_value requires arguments");
                    if (!args.TryGetProperty("file_type", out var ft) || ft.ValueKind != JsonValueKind.String) return ToolError(request, "config_set_value requires file_type");
                    if (!args.TryGetProperty("section", out var sec) || sec.ValueKind != JsonValueKind.String) return ToolError(request, "config_set_value requires section");
                    if (!args.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String) return ToolError(request, "config_set_value requires key");
                    if (!args.TryGetProperty("value", out var val)) return ToolError(request, "config_set_value requires value");

                    var cfg = ServiceLocator.SafeGet<IConfigurationService>();
                    if (cfg == null) return ToolError(request, "IConfigurationService is not available");

                    cfg.SetValue(ft.GetString() ?? "config", sec.GetString() ?? "", key.GetString() ?? "", val.ToString());
                    return ToolOk(request, new { success = true });
                }

                if (string.Equals(name, ConfigReloadToolName, StringComparison.OrdinalIgnoreCase))
                {
                    var cfg = ServiceLocator.SafeGet<IConfigurationService>();
                    if (cfg == null) return ToolError(request, "IConfigurationService is not available");
                    cfg.ReloadConfig();
                    return ToolOk(request, new { success = true });
                }

                return null;
            }
            catch (Exception ex)
            {
                return ToolError(request, ex.Message);
            }
        }

        private JsonRpcResponse ToolOk(JsonRpcRequest request, object payload)
        {
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

        private JsonRpcResponse ToolError(JsonRpcRequest request, string message)
        {
            var text = JsonSerializer.Serialize(new { success = false, error = message }, _jsonOptions);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = true
                }
            };
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
            IUserInteractionService ui;
            try
            {
                ui = GetUserInteractionService();
            }
            catch
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
            IUserInteractionService ui;
            try
            {
                ui = GetUserInteractionService();
            }
            catch
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

            // Use the UI service directly (native or webview based on settings)
            bool completed = PromptUserInputsWithNavigation(ui, args.UserInputs, context);

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
            IUserInteractionService ui;
            try
            {
                ui = GetUserInteractionService();
            }
            catch
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
            // ui_show_markdown always uses WebView service because it displays content in a React tab
            var ui = ServiceLocator.SafeGet<WebViewUserInteractionService>() 
                ?? ServiceLocator.SafeGet<IUserInteractionService>();
            
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

            // Headless MCP mode: avoid injecting arguments into clipboard text unless explicitly provided.
            if (config.McpHeadless)
                return string.Empty;

            // If no template and no text arg, fall back to JSON string of args (interactive mode / debugging convenience)
            return JsonSerializer.Serialize(argsDict);
        }

        private static JsonElement DefaultSchemaForHandler(HandlerConfig cfg)
        {
            if (string.Equals(cfg.Type, FileHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                return FilesSchema();
            }

            if (cfg.UserInputs != null && cfg.UserInputs.Count > 0)
            {
                return UserInputsSchema(cfg.UserInputs);
            }

            return DefaultTextSchema();
        }

        private static JsonElement FilesSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "files": {
                  "type": "array",
                  "items": { "type": "string" }
                }
              },
              "required": ["files"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement UserInputsSchema(List<UserInputRequest> userInputs)
        {
            var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in userInputs)
            {
                if (input == null || string.IsNullOrWhiteSpace(input.Key))
                    continue;

                var prop = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                };

                if (!string.IsNullOrWhiteSpace(input.Title))
                    prop["title"] = input.Title;

                if (!string.IsNullOrWhiteSpace(input.Message))
                    prop["description"] = input.Message;

                if (!string.IsNullOrWhiteSpace(input.DefaultValue))
                    prop["default"] = input.DefaultValue;

                properties[input.Key] = prop;

                if (input.IsRequired)
                    required.Add(input.Key);
            }

            var schemaObj = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["additionalProperties"] = new Dictionary<string, object?> { ["type"] = "string" }
            };

            if (required.Count > 0)
                schemaObj["required"] = required.ToArray();

            var json = JsonSerializer.Serialize(schemaObj);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
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
                "context": { 
                  "type": "object", 
                  "additionalProperties": { "type": "string" },
                  "description": "Optional initial context values (key-value pairs)"
                },
                "user_inputs": {
                  "type": "array",
                  "description": "Array of input prompts to show to the user sequentially",
                  "items": {
                    "type": "object",
                    "properties": {
                      "key": { "type": "string", "description": "Unique key to store the user's input" },
                      "title": { "type": "string", "description": "Dialog title" },
                      "message": { "type": "string", "description": "Prompt message shown to user" },
                      "validation_regex": { "type": "string", "description": "Optional regex pattern for input validation" },
                      "is_required": { "type": "boolean", "description": "If true, user must provide a value (default: true)" },
                      "is_selection_list": { "type": "boolean", "description": "If true, shows a dropdown. Requires selection_items" },
                      "is_password": { "type": "boolean", "description": "If true, shows a password input (masked)" },
                      "is_multi_select": { "type": "boolean", "description": "If true, allows multiple selection. Requires is_selection_list" },
                      "is_file_picker": { "type": "boolean", "description": "If true, shows a file browser dialog" },
                      "is_multi_line": { "type": "boolean", "description": "If true, shows a multi-line text area" },
                      "default_value": { "type": "string", "description": "Default value pre-filled in the input" },
                      "selection_items": {
                        "type": "array",
                        "description": "Options for dropdown/list. Required when is_selection_list is true",
                        "items": {
                          "type": "object",
                          "properties": {
                            "value": { "type": "string", "description": "The value stored when selected" },
                            "display": { "type": "string", "description": "The text shown to the user" }
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

        private static JsonElement EmptyObjectSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {},
              "additionalProperties": true
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlersListSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "include_configs": { "type": "boolean", "default": false }
              }
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlersGetSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": { "name": { "type": "string" } },
              "required": ["name"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlerAddSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "handler_config": { "type": "object" },
                "reload_after_add": { "type": "boolean", "default": true }
              },
              "required": ["handler_config"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlerUpdateSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "handler_name": { "type": "string" },
                "updates": { "type": "object" },
                "reload_after_update": { "type": "boolean", "default": true }
              },
              "required": ["handler_name", "updates"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlerDeleteSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "handler_name": { "type": "string" },
                "reload_after_delete": { "type": "boolean", "default": true }
              },
              "required": ["handler_name"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlerReloadSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "reload_plugins": { "type": "boolean", "default": false }
              }
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement HandlerDocsSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "show_ui": { "type": "boolean", "default": false },
                "title": { "type": "string" },
                "auto_focus": { "type": "boolean", "default": false },
                "bring_to_front": { "type": "boolean", "default": false }
              }
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement ConfigGetSectionSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "section": { "type": "string" }
              },
              "required": ["section"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static JsonElement ConfigSetValueSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "file_type": { "type": "string", "description": "config or secrets" },
                "section": { "type": "string" },
                "key": { "type": "string" },
                "value": { "type": "string" }
              },
              "required": ["file_type", "section", "key", "value"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        private static string BuildHandlerDocsMarkdown()
        {
            return """
# Handler Authoring Guide (Contextualizer) — Single Source of Truth

This document fully describes **all supported fields** in `HandlerConfig` and how to compose a handler from scratch.
If you know nothing about the system, start here.

---

## 0) Minimal handler
Every handler requires:
```
name: string
type: string
```
Everything else is optional but may be required by the specific handler type.

---

## 1) Handler lifecycle (execution order)
1. **CanHandle** decides if a handler should run for clipboard content.
2. **CreateContext** builds a context map (regex groups, API JSON, DB rows, file metadata, etc.).
3. **ConstantSeeder** merges into context (no templating).
4. **Seeder** merges into context (templating enabled).
5. **Templating** resolves `$config:`, `$file:`, `$func:` then `$(key)` placeholders.
6. **Actions** execute in order (with optional conditions and action user_inputs).
7. **Output** is produced using `output_format` (if missing, handler may produce defaults).

---

## 2) All fields (HandlerConfig schema)

### Common identity + UI
```
name: string (required)
description?: string
type: string (required)
screen_id?: string         // tab id for show_window actions
title?: string             // tab title
enabled?: boolean          // default true
requires_confirmation?: boolean
auto_focus_tab?: boolean
bring_window_to_front?: boolean
```

### Output + context seeding
```
output_format?: string
seeder?: { [key:string]: string }           // templated values
constant_seeder?: { [key:string]: string }  // raw values
user_inputs?: UserInputRequest[]            // handler-level prompts
actions?: ConfigAction[]                    // handler-level actions
```

### Regex / Groups (used by Regex, optional in Api/Database)
```
regex?: string
groups?: string[]
```

### File handler
```
file_extensions?: string[]   // e.g. [".txt", ".json"]
```

### Lookup handler
```
path?: string                 // supports $config:, $file:
delimiter?: string            // e.g. "\\t" or ","
key_names?: string[]          // which columns are keys
value_names?: string[]        // all column names
```

### Database handler
```
connectionString?: string
query?: string
connector?: string            // "mssql" | "plsql"
command_timeout_seconds?: int?
connection_timeout_seconds?: int?
max_pool_size?: int?
min_pool_size?: int?
disable_pooling?: bool?
regex?: string                // optional
groups?: string[]             // optional
```

### API handler
```
url?: string
method?: string               // GET/POST/PUT/PATCH/DELETE
headers?: { [key:string]: string }
request_body?: object|array   // JSON
content_type?: string         // e.g. application/json
timeout_seconds?: int?
regex?: string                // optional
groups?: string[]             // optional
```

### Custom handler
```
validator?: string            // IContextValidator.Name
context_provider?: string     // IContextProvider.Name
```

### Synthetic handler
```
reference_handler?: string    // name of existing handler
actual_type?: string          // embed actual handler type
synthetic_input?: UserInputRequest
```

### Cron handler
```
cron_job_id?: string
cron_expression?: string
cron_timezone?: string
cron_enabled?: bool
actual_type?: string          // embedded handler type (required)
```

### MCP (Model Context Protocol)
```
mcp_enabled?: bool
mcp_tool_name?: string
mcp_description?: string
mcp_input_schema?: object     // JSON Schema
mcp_input_template?: string   // builds ClipboardContent.Text
mcp_return_keys?: string[]    // filter outputs
mcp_headless?: bool           // disable UI prompts
mcp_seed_overwrite?: bool
```

---

## 3) Placeholders & templating
Templating order: `$file:` → `$config:` → `$func:` → `$(key)`

### $(key)
```
"Order $(orderId) for $(customer)"
```

### $config:
```
$config:secrets.api_key
$config:database.default
```

### $file:
```
$file:C:\\path\\file.txt
```

### $func:
Functions run before `$(key)` substitution.
Examples:
```
$func:today().format("yyyy-MM-dd")
$func:guid()
$func:string.upper("hello")
$func:{{ $(id) | string.upper() }}
```

---

## 4) FunctionProcessor reference
Supported base functions (case-insensitive):
```
today, now, yesterday, tomorrow, guid, random,
base64encode, base64decode, env, username, computername
```

Supported namespaces (base):
```
hash.*, url.*, web.*, ip.*, json.*, string.*, math.*, array.*
```

Common examples:
```
$func:hash.sha256("text")
$func:url.encode("a b")
$func:web.get("https://example.com")
$func:ip.local()
$func:json.get("{\\"a\\":1}", "a")
$func:string.upper("hi")
$func:math.add(2, 3)
$func:array.join("[\\"a\\",\\"b\\"]", ",")
```

Pipeline format:
```
$func:{{ "abc" | string.upper() | string.substring(0,2) }}
```

---

## 5) Conditions (ConditionEvaluator)
Operators:
```
and, or, equals, not_equals, greater_than, less_than,
contains, starts_with, ends_with, matches_regex,
is_empty, is_not_empty
```

Leaf condition:
```
{ "operator": "equals", "field": "StatusCode", "value": "200" }
```

Group:
```
{ "operator": "and", "conditions": [ ... ] }
```

---

## 6) Seeder vs ConstantSeeder
- `constant_seeder` merges raw values first.
- `seeder` merges after and resolves templates.

Example:
```
constant_seeder: { "source": "ui" }
seeder: { "key": "$(group_1)", "ts": "$func:now().format(\\"o\\")" }
```

---

## 7) UserInputRequest (all options)
```
{
  "key": "username",
  "title": "User",
  "message": "Enter user name",
  "validation_regex": "^[a-z0-9_]+$",
  "is_required": true,
  "is_selection_list": false,
  "is_password": false,
  "selection_items": [ {"value":"a","display":"A"} ],
  "is_multi_select": false,
  "is_file_picker": false,
  "is_multi_line": false,
  "default_value": "",
  "dependent_key": "country",
  "dependent_selection_item_map": {
    "TR": { "selection_items": [ {"value":"34","display":"Istanbul"} ], "default_value": "34" }
  },
  "config_target": "secrets.section.key"
}
```

---

## 8) ConfigAction (all options)
```
{
  "name": "show_window",
  "key": "optional",
  "requires_confirmation": false,
  "conditions": { ...Condition... },
  "user_inputs": [ ...UserInputRequest... ],
  "seeder": { "k": "$(value)" },
  "constant_seeder": { "k": "v" },
  "inner_actions": [ ...ConfigAction... ]
}
```

---

## 9) Type-specific minimal examples

### Regex
```
{ "name":"R1", "type":"Regex", "regex":"^ABC(?<id>\\d+)$", "groups":["id"] }
```

### File
```
{ "name":"F1", "type":"File", "file_extensions":[".txt",".json"] }
```

### Lookup
```
{ "name":"L1", "type":"Lookup", "path":"$config:data.lookup_path", "delimiter":"\\t",
  "key_names":["sku"], "value_names":["sku","desc","price"] }
```

### Database
```
{ "name":"DB1", "type":"Database", "connector":"mssql",
  "connectionString":"$config:db.main",
  "query":"select * from Orders where Id=@orderId",
  "regex":"^ORD-(?<orderId>\\d+)$", "groups":["orderId"] }
```

### API
```
{ "name":"API1", "type":"Api",
  "url":"https://api/items/$(id)",
  "method":"GET",
  "headers": { "Authorization":"Bearer $config:secrets.api" } }
```

### Custom
```
{ "name":"C1", "type":"Custom", "validator":"MyValidator", "context_provider":"MyProvider" }
```

### Manual
```
{ "name":"M1", "type":"Manual" }
```

### Synthetic (reference)
```
{ "name":"S1", "type":"Synthetic", "reference_handler":"API1" }
```

### Synthetic (embedded)
```
{ "name":"S2", "type":"Synthetic", "actual_type":"Database",
  "connectionString":"$config:db.main", "connector":"mssql", "query":"select 1" }
```

### Cron (embedded)
```
{ "name":"CR1", "type":"Cron", "cron_expression":"0 */5 * * * ?",
  "cron_timezone":"Europe/Istanbul", "cron_enabled":true,
  "actual_type":"Api", "url":"https://api/ping", "method":"GET" }
```

---

## 10) MCP usage
Set `mcp_enabled` to expose the handler as a tool.

Optional MCP fields:
```
mcp_tool_name, mcp_description, mcp_input_schema,
mcp_input_template, mcp_return_keys, mcp_headless, mcp_seed_overwrite
```

If no `mcp_input_schema` is provided:
- File handlers expect `{ files: string[] }`
- If `user_inputs` exist, a schema is generated from them
- Otherwise `{ text: string }`

---

## 11) Complete example (API + actions + user_inputs)
```
{
  "name":"OrderLookup",
  "type":"Api",
  "url":"https://api/orders/$(orderId)",
  "method":"GET",
  "headers": { "Authorization":"Bearer $config:secrets.api_key" },
  "user_inputs":[
    { "key":"orderId", "title":"Order", "message":"Enter order id" }
  ],
  "seeder": { "requested_at":"$func:now().format(\\"o\\")" },
  "actions": [
    {
      "name":"show_notification",
      "conditions": { "operator":"equals", "field":"StatusCode", "value":"200" }
    }
  ],
  "output_format":"Order $(id) — $(status)"
}
```
""";
        }

        private static void ShowMarkdownTab(string title, string markdown, bool autoFocus, bool bringToFront)
        {
            var ui = ServiceLocator.SafeGet<WebViewUserInteractionService>()
                ?? ServiceLocator.SafeGet<IUserInteractionService>();

            if (ui == null) return;

            var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ContextKey._body] = markdown
            };

            ui.ShowWindow("markdown2", title, context, null, autoFocus, bringToFront);
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


