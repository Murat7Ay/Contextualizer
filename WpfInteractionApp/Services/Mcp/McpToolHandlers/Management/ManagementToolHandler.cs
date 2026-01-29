using System;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using WpfInteractionApp.Services.Mcp.McpModels;
using WpfInteractionApp.Services.Mcp.McpToolHandlers.Management;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class ManagementToolHandler
    {
        public static bool IsManagementTool(string name)
        {
            return string.Equals(name, HandlerManagementTools.HandlersListToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerManagementTools.HandlersGetToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, DatabaseToolManagementTools.DatabaseToolCreateToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, DatabaseToolManagementTools.HandlerUpdateDatabaseToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerManagementTools.HandlerCreateApiToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerManagementTools.HandlerUpdateApiToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerManagementTools.HandlerDeleteToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerManagementTools.HandlerReloadToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, PluginManagementTools.PluginsListToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigManagementTools.ConfigGetKeysToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigManagementTools.ConfigGetSectionToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigManagementTools.ConfigSetValueToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigManagementTools.ConfigReloadToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerManagementTools.HandlerDocsToolName, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<JsonRpcResponse?> TryHandleAsync(JsonRpcRequest request, McpToolsCallParams callParams, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            var name = callParams.Name ?? string.Empty;
            if (!IsManagementTool(name))
                return null;

            var args = callParams.Arguments.HasValue ? callParams.Arguments.Value : default;

            try
            {
                if (string.Equals(name, HandlerManagementTools.HandlersListToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlersListAsync(request, args, jsonOptions);

                if (string.Equals(name, HandlerManagementTools.HandlersGetToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlersGetAsync(request, args, jsonOptions);

                if (string.Equals(name, DatabaseToolManagementTools.DatabaseToolCreateToolName, StringComparison.OrdinalIgnoreCase))
                    return await DatabaseToolManagementTools.HandleDatabaseToolCreateAsync(request, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerManagementTools.HandlerCreateApiToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlerCreateAsync(request, name, args, handlerManager, jsonOptions);

                if (string.Equals(name, DatabaseToolManagementTools.HandlerUpdateDatabaseToolName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, HandlerManagementTools.HandlerUpdateApiToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlerUpdateAsync(request, name, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerManagementTools.HandlerDeleteToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlerDeleteAsync(request, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerManagementTools.HandlerReloadToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlerReloadAsync(request, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerManagementTools.HandlerDocsToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandlerManagementTools.HandleHandlerDocsAsync(request, args, jsonOptions);

                if (string.Equals(name, PluginManagementTools.PluginsListToolName, StringComparison.OrdinalIgnoreCase))
                    return PluginManagementTools.HandlePluginsList(request, jsonOptions);

                if (string.Equals(name, ConfigManagementTools.ConfigGetKeysToolName, StringComparison.OrdinalIgnoreCase))
                    return ConfigManagementTools.HandleConfigGetKeys(request, jsonOptions);

                if (string.Equals(name, ConfigManagementTools.ConfigGetSectionToolName, StringComparison.OrdinalIgnoreCase))
                    return ConfigManagementTools.HandleConfigGetSection(request, args, jsonOptions);

                if (string.Equals(name, ConfigManagementTools.ConfigSetValueToolName, StringComparison.OrdinalIgnoreCase))
                    return ConfigManagementTools.HandleConfigSetValue(request, args, jsonOptions);

                if (string.Equals(name, ConfigManagementTools.ConfigReloadToolName, StringComparison.OrdinalIgnoreCase))
                    return ConfigManagementTools.HandleConfigReload(request, jsonOptions);

                return null;
            }
            catch (Exception ex)
            {
                return Management.ManagementToolHelpers.CreateToolError(request, ex.Message, jsonOptions);
            }
        }
    }
}
