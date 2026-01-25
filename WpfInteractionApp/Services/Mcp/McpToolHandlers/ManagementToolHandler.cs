using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpHelpers;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class ManagementToolHandler
    {
        private const string HandlersListToolName = "handlers_list";
        private const string HandlersGetToolName = "handlers_get";
        private const string DatabaseToolCreateToolName = "database_tool_create";
        private const string HandlerUpdateDatabaseToolName = "handler_update_database";
        private const string HandlerCreateApiToolName = "handler_create_api";
        private const string HandlerUpdateApiToolName = "handler_update_api";
        private const string HandlerDeleteToolName = "handler_delete";
        private const string HandlerReloadToolName = "handler_reload";
        private const string PluginsListToolName = "plugins_list";
        private const string ConfigGetKeysToolName = "config_get_keys";
        private const string ConfigGetSectionToolName = "config_get_section";
        private const string ConfigSetValueToolName = "config_set_value";
        private const string ConfigReloadToolName = "config_reload";
        private const string HandlerDocsToolName = "handler_docs";

        public static bool IsManagementTool(string name)
        {
            return string.Equals(name, HandlersListToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlersGetToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, DatabaseToolCreateToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerUpdateDatabaseToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerCreateApiToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerUpdateApiToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerDeleteToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerReloadToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, PluginsListToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigGetKeysToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigGetSectionToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigSetValueToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, ConfigReloadToolName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, HandlerDocsToolName, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<JsonRpcResponse?> TryHandleAsync(JsonRpcRequest request, McpToolsCallParams callParams, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            var name = callParams.Name ?? string.Empty;
            if (!IsManagementTool(name))
                return null;

            var args = callParams.Arguments.HasValue ? callParams.Arguments.Value : default;

            try
            {
                if (string.Equals(name, HandlersListToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlersListAsync(request, args, jsonOptions);

                if (string.Equals(name, HandlersGetToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlersGetAsync(request, args, jsonOptions);

                if (string.Equals(name, DatabaseToolCreateToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleDatabaseToolCreateAsync(request, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerCreateApiToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlerCreateAsync(request, name, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerUpdateDatabaseToolName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, HandlerUpdateApiToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlerUpdateAsync(request, name, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerDeleteToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlerDeleteAsync(request, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerReloadToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlerReloadAsync(request, args, handlerManager, jsonOptions);

                if (string.Equals(name, HandlerDocsToolName, StringComparison.OrdinalIgnoreCase))
                    return await HandleHandlerDocsAsync(request, args, jsonOptions);

                if (string.Equals(name, PluginsListToolName, StringComparison.OrdinalIgnoreCase))
                    return HandlePluginsList(request, jsonOptions);

                if (string.Equals(name, ConfigGetKeysToolName, StringComparison.OrdinalIgnoreCase))
                    return HandleConfigGetKeys(request, jsonOptions);

                if (string.Equals(name, ConfigGetSectionToolName, StringComparison.OrdinalIgnoreCase))
                    return HandleConfigGetSection(request, args, jsonOptions);

                if (string.Equals(name, ConfigSetValueToolName, StringComparison.OrdinalIgnoreCase))
                    return HandleConfigSetValue(request, args, jsonOptions);

                if (string.Equals(name, ConfigReloadToolName, StringComparison.OrdinalIgnoreCase))
                    return HandleConfigReload(request, jsonOptions);

                return null;
            }
            catch (Exception ex)
            {
                return CreateToolError(request, ex.Message, jsonOptions);
            }
        }

        private static async Task<JsonRpcResponse> HandleHandlersListAsync(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            var store = TryCreateHandlerConfigStore();
            if (store == null) return CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            bool includeConfigs = false;
            if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("include_configs", out var ic) && ic.ValueKind is JsonValueKind.True or JsonValueKind.False)
                includeConfigs = ic.ValueKind == JsonValueKind.True;

            var all = await store.ReadAllAsync();
            if (includeConfigs)
            {
                return CreateToolOk(request, new { success = true, handlers = all }, jsonOptions);
            }

            return CreateToolOk(request, new
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

        private static async Task<JsonRpcResponse> HandleHandlersGetAsync(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("name", out var n) || n.ValueKind != JsonValueKind.String)
                return CreateToolError(request, "handlers_get requires arguments.name", jsonOptions);

            var store = TryCreateHandlerConfigStore();
            if (store == null) return CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var cfg = await store.GetByNameAsync(n.GetString() ?? string.Empty);
            if (cfg == null) return CreateToolError(request, "Handler not found", jsonOptions);
            return CreateToolOk(request, new { success = true, handler = cfg }, jsonOptions);
        }

        private static async Task<JsonRpcResponse> HandleDatabaseToolCreateAsync(JsonRpcRequest request, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object)
                return CreateToolError(request, "database_tool_create requires arguments object", jsonOptions);

            // Validate required fields
            if (!args.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameProp.GetString()))
                return CreateToolError(request, "database_tool_create requires arguments.name (string)", jsonOptions);

            if (!args.TryGetProperty("connection_string", out var connStrProp) || connStrProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(connStrProp.GetString()))
                return CreateToolError(request, "database_tool_create requires arguments.connection_string (string)", jsonOptions);

            if (!args.TryGetProperty("connector", out var connectorProp) || connectorProp.ValueKind != JsonValueKind.String)
                return CreateToolError(request, "database_tool_create requires arguments.connector (string)", jsonOptions);

            var connector = connectorProp.GetString() ?? string.Empty;
            if (!string.Equals(connector, "mssql", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(connector, "plsql", StringComparison.OrdinalIgnoreCase))
                return CreateToolError(request, "database_tool_create connector must be 'mssql' or 'plsql'", jsonOptions);

            if (!args.TryGetProperty("query", out var queryProp) || queryProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(queryProp.GetString()))
                return CreateToolError(request, "database_tool_create requires arguments.query (string)", jsonOptions);

            var query = queryProp.GetString() ?? string.Empty;
            if (!query.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                return CreateToolError(request, "database_tool_create query must start with SELECT", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_add", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = TryCreateHandlerConfigStore();
            if (store == null) return CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            // Create HandlerConfig
            var cfg = new HandlerConfig
            {
                Name = nameProp.GetString() ?? string.Empty,
                ConnectionString = connStrProp.GetString() ?? string.Empty,
                Connector = connector,
                Query = query,
                Type = "Database",
                McpEnabled = true,
                Enabled = true
            };

            // Optional fields
            if (args.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                cfg.Description = descProp.GetString();

            if (args.TryGetProperty("regex", out var regexProp) && regexProp.ValueKind == JsonValueKind.String)
                cfg.Regex = regexProp.GetString();

            if (args.TryGetProperty("groups", out var groupsProp) && groupsProp.ValueKind == JsonValueKind.Array)
            {
                var groupsList = new List<string>();
                foreach (var groupItem in groupsProp.EnumerateArray())
                {
                    if (groupItem.ValueKind == JsonValueKind.String)
                        groupsList.Add(groupItem.GetString() ?? string.Empty);
                }
                if (groupsList.Count > 0)
                    cfg.Groups = groupsList;
            }

            if (args.TryGetProperty("mcp_tool_name", out var mcpToolNameProp) && mcpToolNameProp.ValueKind == JsonValueKind.String)
                cfg.McpToolName = mcpToolNameProp.GetString();
            else
                cfg.McpToolName = McpHelper.Slugify(cfg.Name);

            if (args.TryGetProperty("mcp_description", out var mcpDescProp) && mcpDescProp.ValueKind == JsonValueKind.String)
                cfg.McpDescription = mcpDescProp.GetString();
            else
                cfg.McpDescription = !string.IsNullOrWhiteSpace(cfg.Description) ? cfg.Description : $"Database tool: {cfg.Name}";

            // Database-specific timeout and pooling settings
            if (args.TryGetProperty("command_timeout_seconds", out var cmdTimeoutProp) && cmdTimeoutProp.ValueKind == JsonValueKind.Number)
                cfg.CommandTimeoutSeconds = cmdTimeoutProp.GetInt32();

            if (args.TryGetProperty("connection_timeout_seconds", out var connTimeoutProp) && connTimeoutProp.ValueKind == JsonValueKind.Number)
                cfg.ConnectionTimeoutSeconds = connTimeoutProp.GetInt32();

            if (args.TryGetProperty("max_pool_size", out var maxPoolProp) && maxPoolProp.ValueKind == JsonValueKind.Number)
                cfg.MaxPoolSize = maxPoolProp.GetInt32();

            if (args.TryGetProperty("min_pool_size", out var minPoolProp) && minPoolProp.ValueKind == JsonValueKind.Number)
                cfg.MinPoolSize = minPoolProp.GetInt32();

            if (args.TryGetProperty("disable_pooling", out var disablePoolProp) && disablePoolProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                cfg.DisablePooling = disablePoolProp.ValueKind == JsonValueKind.True;

            // MCP-specific settings
            if (args.TryGetProperty("mcp_input_schema", out var mcpInputSchemaProp) && mcpInputSchemaProp.ValueKind == JsonValueKind.Object)
                cfg.McpInputSchema = mcpInputSchemaProp.Clone();

            if (args.TryGetProperty("mcp_input_template", out var mcpInputTemplateProp) && mcpInputTemplateProp.ValueKind == JsonValueKind.String)
                cfg.McpInputTemplate = mcpInputTemplateProp.GetString();

            if (args.TryGetProperty("mcp_return_keys", out var mcpReturnKeysProp) && mcpReturnKeysProp.ValueKind == JsonValueKind.Array)
            {
                var returnKeysList = new List<string>();
                foreach (var keyItem in mcpReturnKeysProp.EnumerateArray())
                {
                    if (keyItem.ValueKind == JsonValueKind.String)
                        returnKeysList.Add(keyItem.GetString() ?? string.Empty);
                }
                if (returnKeysList.Count > 0)
                    cfg.McpReturnKeys = returnKeysList;
            }

            if (args.TryGetProperty("mcp_headless", out var mcpHeadlessProp) && mcpHeadlessProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                cfg.McpHeadless = mcpHeadlessProp.ValueKind == JsonValueKind.True;

            if (args.TryGetProperty("mcp_seed_overwrite", out var mcpSeedOverwriteProp) && mcpSeedOverwriteProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                cfg.McpSeedOverwrite = mcpSeedOverwriteProp.ValueKind == JsonValueKind.True;

            var res = await store.AddAsync(cfg);
            if (!res.Success) return CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return CreateToolOk(request, new { success = true, name = cfg.Name, type = "Database" }, jsonOptions);
        }

        private static async Task<JsonRpcResponse> HandleHandlerCreateAsync(JsonRpcRequest request, string toolName, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_config", out var hc) || hc.ValueKind != JsonValueKind.Object)
                return CreateToolError(request, "handler_create requires arguments.handler_config (object)", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_add", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = TryCreateHandlerConfigStore();
            if (store == null) return CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var cfg = hc.Deserialize<HandlerConfig>(jsonOptions);
            if (cfg == null) return CreateToolError(request, "Invalid handler_config JSON", jsonOptions);

            var expectedType = "Api";
            if (string.IsNullOrWhiteSpace(cfg.Type))
                cfg.Type = expectedType;
            if (!string.Equals(cfg.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                return CreateToolError(request, $"handler_config.type must be '{expectedType}'", jsonOptions);

            var res = await store.AddAsync(cfg);
            if (!res.Success) return CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return CreateToolOk(request, new { success = true, name = cfg.Name, type = cfg.Type }, jsonOptions);
        }

        private static async Task<JsonRpcResponse> HandleHandlerUpdateAsync(JsonRpcRequest request, string toolName, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_name", out var hn) || hn.ValueKind != JsonValueKind.String)
                return CreateToolError(request, "handler_update requires arguments.handler_name", jsonOptions);
            if (!args.TryGetProperty("updates", out var up) || up.ValueKind != JsonValueKind.Object)
                return CreateToolError(request, "handler_update requires arguments.updates (object)", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_update", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = TryCreateHandlerConfigStore();
            if (store == null) return CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var existing = await store.GetByNameAsync(hn.GetString() ?? string.Empty);
            if (existing == null) return CreateToolError(request, "Handler not found", jsonOptions);
            var expectedType = string.Equals(toolName, HandlerUpdateDatabaseToolName, StringComparison.OrdinalIgnoreCase) ? "Database" : "Api";
            if (!string.Equals(existing.Type, expectedType, StringComparison.OrdinalIgnoreCase))
                return CreateToolError(request, $"Handler '{existing.Name}' is not type '{expectedType}'", jsonOptions);

            var res = await store.UpdatePartialAsync(hn.GetString() ?? string.Empty, up);
            if (!res.Success) return CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return CreateToolOk(request, new { success = true, name = hn.GetString(), updated_fields = res.Payload?.UpdatedFields ?? new List<string>() }, jsonOptions);
        }

        private static async Task<JsonRpcResponse> HandleHandlerDeleteAsync(JsonRpcRequest request, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("handler_name", out var hn) || hn.ValueKind != JsonValueKind.String)
                return CreateToolError(request, "handler_delete requires arguments.handler_name", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_delete", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = TryCreateHandlerConfigStore();
            if (store == null) return CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

            var res = await store.DeleteAsync(hn.GetString() ?? string.Empty);
            if (!res.Success) return CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return CreateToolOk(request, new { success = true, name = hn.GetString() }, jsonOptions);
        }

        private static async Task<JsonRpcResponse> HandleHandlerReloadAsync(JsonRpcRequest request, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            bool reloadPlugins = false;
            if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("reload_plugins", out var rp) && rp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reloadPlugins = rp.ValueKind == JsonValueKind.True;

            if (handlerManager == null) return CreateToolError(request, "HandlerManager is not available", jsonOptions);

            var (handlersReloaded, newPluginsLoaded) = handlerManager.ReloadHandlers(reloadPlugins);
            return CreateToolOk(request, new { success = true, handlers_reloaded = handlersReloaded, new_plugins_loaded = newPluginsLoaded }, jsonOptions);
        }

        private static async Task<JsonRpcResponse> HandleHandlerDocsAsync(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
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

            var markdown = McpHelper.BuildHandlerDocsMarkdown();
            if (showUi)
            {
                McpHelper.ShowMarkdownTab(title, markdown, autoFocus, bringToFront);
            }

            return CreateToolOk(request, new { success = true, markdown }, jsonOptions);
        }

        private static JsonRpcResponse HandlePluginsList(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
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

            return CreateToolOk(request, payload, jsonOptions);
        }

        private static JsonRpcResponse HandleConfigGetKeys(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return CreateToolError(request, "IConfigurationService is not available", jsonOptions);

            var keys = cfg.GetAllKeys();
            return CreateToolOk(request, new { success = true, keys }, jsonOptions);
        }

        private static JsonRpcResponse HandleConfigGetSection(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("section", out var s) || s.ValueKind != JsonValueKind.String)
                return CreateToolError(request, "config_get_section requires arguments.section", jsonOptions);

            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return CreateToolError(request, "IConfigurationService is not available", jsonOptions);

            var section = s.GetString() ?? string.Empty;
            var values = cfg.GetSection(section);

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

            return CreateToolOk(request, new { success = true, section, values = masked }, jsonOptions);
        }

        private static JsonRpcResponse HandleConfigSetValue(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object) return CreateToolError(request, "config_set_value requires arguments", jsonOptions);
            if (!args.TryGetProperty("file_type", out var ft) || ft.ValueKind != JsonValueKind.String) return CreateToolError(request, "config_set_value requires file_type", jsonOptions);
            if (!args.TryGetProperty("section", out var sec) || sec.ValueKind != JsonValueKind.String) return CreateToolError(request, "config_set_value requires section", jsonOptions);
            if (!args.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String) return CreateToolError(request, "config_set_value requires key", jsonOptions);
            if (!args.TryGetProperty("value", out var val)) return CreateToolError(request, "config_set_value requires value", jsonOptions);

            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return CreateToolError(request, "IConfigurationService is not available", jsonOptions);

            cfg.SetValue(ft.GetString() ?? "config", sec.GetString() ?? "", key.GetString() ?? "", val.ToString());
            return CreateToolOk(request, new { success = true }, jsonOptions);
        }

        private static JsonRpcResponse HandleConfigReload(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return CreateToolError(request, "IConfigurationService is not available", jsonOptions);
            cfg.ReloadConfig();
            return CreateToolOk(request, new { success = true }, jsonOptions);
        }

        private static HandlerConfigStore? TryCreateHandlerConfigStore()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            if (settings == null) return null;
            return new HandlerConfigStore(settings);
        }

        private static JsonRpcResponse CreateToolOk(JsonRpcRequest request, object payload, JsonSerializerOptions jsonOptions)
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

        private static JsonRpcResponse CreateToolError(JsonRpcRequest request, string message, JsonSerializerOptions jsonOptions)
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
