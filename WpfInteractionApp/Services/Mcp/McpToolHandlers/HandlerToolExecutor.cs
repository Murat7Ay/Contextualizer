using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpHelpers;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class HandlerToolExecutor
    {
        public static async Task<JsonRpcResponse> ExecuteHandlerAsync(JsonRpcRequest request, HandlerConfig matchedConfig, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
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
                        argsDict[prop.Name] = prop.Value.ToString();
                        continue;
                    }

                    argsDict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.ToString();
                }
            }

            var inputText = McpHelper.BuildInputText(matchedConfig, argsDict);

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
                    await handler.Execute(clipboard);
                    var fallbackJson = JsonSerializer.Serialize(new Dictionary<string, string> { [ContextKey._formatted_output] = string.Empty }, jsonOptions);
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

                    var errorText = JsonSerializer.Serialize(errorPayload, jsonOptions);
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

                var payload = McpHelper.BuildReturnPayload(matchedConfig, execResult);
                var text = JsonSerializer.Serialize(payload, jsonOptions);

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
    }
}
