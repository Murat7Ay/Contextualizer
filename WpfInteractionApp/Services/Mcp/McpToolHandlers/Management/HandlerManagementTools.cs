using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers.Management
{
    internal static class HandlerManagementTools
    {
        public const string HandlersListToolName = "handlers_list";
        public const string HandlersGetToolName = "handlers_get";
        public const string HandlerCreateApiToolName = "handler_create_api";
        public const string HandlerUpdateApiToolName = "handler_update_api";
        public const string HandlerDeleteToolName = "handler_delete";
        public const string HandlerReloadToolName = "handler_reload";
        public const string HandlerDocsToolName = "handler_docs";

        public static async Task<JsonRpcResponse> HandleHandlersListAsync(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            var store = ManagementToolHelpers.TryCreateHandlerConfigStore();
            if (store == null) return ManagementToolHelpers.CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            bool includeConfigs = false;
            if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("include_configs", out var ic) && ic.ValueKind is JsonValueKind.True or JsonValueKind.False)
                includeConfigs = ic.ValueKind == JsonValueKind.True;

            var all = await store.ReadAllAsync();
            if (includeConfigs)
            {
                return ManagementToolHelpers.CreateToolOk(request, new { success = true, handlers = all }, jsonOptions);
            }

            return ManagementToolHelpers.CreateToolOk(request, new
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
            }, jsonOptions);
        }

        public static async Task<JsonRpcResponse> HandleHandlersGetAsync(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String)
                return ManagementToolHelpers.CreateToolError(request, "handlers_get requires arguments.name", jsonOptions);

            var store = ManagementToolHelpers.TryCreateHandlerConfigStore();
            if (store == null) return ManagementToolHelpers.CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var cfg = await store.GetByNameAsync(n.GetString() ?? string.Empty);
            if (cfg == null) return ManagementToolHelpers.CreateToolError(request, "Handler not found", jsonOptions);
            return ManagementToolHelpers.CreateToolOk(request, new { success = true, handler = cfg }, jsonOptions);
        }

        public static async Task<JsonRpcResponse> HandleHandlerCreateAsync(JsonRpcRequest request, string toolName, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_config", out var hc) || hc.ValueKind != JsonValueKind.Object)
                return ManagementToolHelpers.CreateToolError(request, "handler_create requires arguments.handler_config (object)", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_add", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = ManagementToolHelpers.TryCreateHandlerConfigStore();
            if (store == null) return ManagementToolHelpers.CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var cfg = hc.Deserialize<HandlerConfig>(jsonOptions);
            if (cfg == null) return ManagementToolHelpers.CreateToolError(request, "Invalid handler_config JSON", jsonOptions);

            var expectedType = "Api";
            if (string.IsNullOrWhiteSpace(cfg.Type))
                cfg.Type = expectedType;
            if (!string.Equals(cfg.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                return ManagementToolHelpers.CreateToolError(request, $"handler_config.type must be '{expectedType}'", jsonOptions);

            var res = await store.AddAsync(cfg);
            if (!res.Success) return ManagementToolHelpers.CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return ManagementToolHelpers.CreateToolOk(request, new { success = true, name = cfg.Name, type = cfg.Type }, jsonOptions);
        }

        public static async Task<JsonRpcResponse> HandleHandlerUpdateAsync(JsonRpcRequest request, string toolName, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_name", out var hn) || hn.ValueKind != JsonValueKind.String)
                return ManagementToolHelpers.CreateToolError(request, "handler_update requires arguments.handler_name", jsonOptions);
            if (!args.TryGetProperty("updates", out var up) || up.ValueKind != JsonValueKind.Object)
                return ManagementToolHelpers.CreateToolError(request, "handler_update requires arguments.updates (object)", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_update", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = ManagementToolHelpers.TryCreateHandlerConfigStore();
            if (store == null) return ManagementToolHelpers.CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var existing = await store.GetByNameAsync(hn.GetString() ?? string.Empty);
            if (existing == null) return ManagementToolHelpers.CreateToolError(request, "Handler not found", jsonOptions);
            var expectedType = string.Equals(toolName, DatabaseToolManagementTools.HandlerUpdateDatabaseToolName, StringComparison.OrdinalIgnoreCase) ? "Database" : "Api";
            if (!string.Equals(existing.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                return ManagementToolHelpers.CreateToolError(request, $"Handler '{existing.Name}' is not type '{expectedType}'", jsonOptions);

            var res = await store.UpdatePartialAsync(hn.GetString() ?? string.Empty, up);
            if (!res.Success) return ManagementToolHelpers.CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return ManagementToolHelpers.CreateToolOk(request, new { success = true, name = hn.GetString(), updated_fields = res.Payload?.UpdatedFields ?? new List<string>() }, jsonOptions);
        }

        public static async Task<JsonRpcResponse> HandleHandlerDeleteAsync(JsonRpcRequest request, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_name", out var hn) || hn.ValueKind != JsonValueKind.String)
                return ManagementToolHelpers.CreateToolError(request, "handler_delete requires arguments.handler_name", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_delete", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = ManagementToolHelpers.TryCreateHandlerConfigStore();
            if (store == null) return ManagementToolHelpers.CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var res = await store.DeleteAsync(hn.GetString() ?? string.Empty);
            if (!res.Success) return ManagementToolHelpers.CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return ManagementToolHelpers.CreateToolOk(request, new { success = true, name = hn.GetString() }, jsonOptions);
        }

        public static async Task<JsonRpcResponse> HandleHandlerReloadAsync(JsonRpcRequest request, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            bool reloadPlugins = false;
            if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("reload_plugins", out var rp) && rp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reloadPlugins = rp.ValueKind == JsonValueKind.True;

            if (handlerManager == null) return ManagementToolHelpers.CreateToolError(request, "HandlerManager is not available", jsonOptions);

            var (handlersReloaded, newPluginsLoaded) = handlerManager.ReloadHandlers(reloadPlugins);
            return ManagementToolHelpers.CreateToolOk(request, new { success = true, handlers_reloaded = handlersReloaded, new_plugins_loaded = newPluginsLoaded }, jsonOptions);
        }

        public static async Task<JsonRpcResponse> HandleHandlerDocsAsync(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
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

            var markdown = McpHelpers.McpHelper.BuildHandlerDocsMarkdown();
            if (showUi)
            {
                McpHelpers.McpHelper.ShowMarkdownTab(title, markdown, autoFocus, bringToFront);
            }

            return ManagementToolHelpers.CreateToolOk(request, new { success = true, markdown }, jsonOptions);
        }
    }
}
