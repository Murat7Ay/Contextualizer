using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using WpfInteractionApp.Services.Mcp.McpHelpers;
using WpfInteractionApp.Services.Mcp.McpModels;
using WpfInteractionApp.Services.Mcp.McpToolHandlers;

namespace WpfInteractionApp.Services.Mcp
{
    internal static class McpJsonRpcHandler
    {
        public static async Task<JsonRpcResponse?> HandleAsync(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            // Notifications
            if (request.Id == null)
            {
                if (string.Equals(request.Method, "notifications/initialized", StringComparison.OrdinalIgnoreCase))
                    return null;
                return null;
            }

            try
            {
                return request.Method switch
                {
                    "initialize" => CreateInitializeResponse(request),
                    "tools/list" => await CreateToolsListResponseAsync(request, jsonOptions),
                    "tools/call" => await CreateToolsCallResponseAsync(request, jsonOptions),
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

        private static JsonRpcResponse CreateInitializeResponse(JsonRpcRequest request)
        {
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

        private static async Task<JsonRpcResponse> CreateToolsListResponseAsync(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            var includeManagementTools = IsManagementToolsEnabled();
            var tools = McpToolRegistry.GetAllTools(includeManagementTools);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsListResult { Tools = tools }
            };
        }

        private static async Task<JsonRpcResponse> CreateToolsCallResponseAsync(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            var handlerManager = ServiceLocator.SafeGet<HandlerManager>();

            var callParams = request.Params.HasValue
                ? request.Params.Value.Deserialize<McpToolsCallParams>(jsonOptions)
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
            if (string.Equals(callParams.Name, "ui_confirm", StringComparison.OrdinalIgnoreCase))
                return await UiToolHandler.HandleUiConfirmAsync(request, callParams, jsonOptions);

            if (string.Equals(callParams.Name, "ui_user_inputs", StringComparison.OrdinalIgnoreCase))
                return await UiToolHandler.HandleUiUserInputsAsync(request, callParams, jsonOptions);

            if (string.Equals(callParams.Name, "ui_notify", StringComparison.OrdinalIgnoreCase))
                return await UiToolHandler.HandleUiNotifyAsync(request, callParams, jsonOptions);

            if (string.Equals(callParams.Name, "ui_show_markdown", StringComparison.OrdinalIgnoreCase))
                return await UiToolHandler.HandleUiShowMarkdownAsync(request, callParams, jsonOptions);

            // Management tools (gated)
            if (IsManagementToolsEnabled())
            {
                var mgmt = await ManagementToolHandler.TryHandleAsync(request, callParams, handlerManager, jsonOptions);
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
                 string.Equals(McpHelper.Slugify(c.Name), callParams.Name, StringComparison.OrdinalIgnoreCase)));

            if (matchedConfig == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32004, Message = $"Tool not found: {callParams.Name}" }
                };
            }

            return await HandlerToolExecutor.ExecuteHandlerAsync(request, matchedConfig, callParams, jsonOptions);
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

        private static bool IsManagementToolsEnabled()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            return settings?.Settings?.McpSettings?.ManagementToolsEnabled == true;
        }
    }
}
