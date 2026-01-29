using System;
using System.Linq;
using System.Text.Json;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers.Management
{
    internal static class PluginManagementTools
    {
        public const string PluginsListToolName = "plugins_list";

        public static JsonRpcResponse HandlePluginsList(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
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

            return ManagementToolHelpers.CreateToolOk(request, payload, jsonOptions);
        }
    }
}
