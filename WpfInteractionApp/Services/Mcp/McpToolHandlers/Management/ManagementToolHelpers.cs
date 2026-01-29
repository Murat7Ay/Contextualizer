using System.Text.Json;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using WpfInteractionApp.Services;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers.Management
{
    internal static class ManagementToolHelpers
    {
        public static HandlerConfigStore? TryCreateHandlerConfigStore()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            if (settings == null) return null;
            return new HandlerConfigStore(settings);
        }

        public static JsonRpcResponse CreateToolOk(JsonRpcRequest request, object payload, JsonSerializerOptions jsonOptions)
        {
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

        public static JsonRpcResponse CreateToolError(JsonRpcRequest request, string message, JsonSerializerOptions jsonOptions)
        {
            var text = JsonSerializer.Serialize(new { success = false, error = message }, jsonOptions);
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
    }
}
