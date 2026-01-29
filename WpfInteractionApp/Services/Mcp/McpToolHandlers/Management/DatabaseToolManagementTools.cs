using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpHelpers;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers.Management
{
    internal static class DatabaseToolManagementTools
    {
        public const string DatabaseToolCreateToolName = "database_tool_create";
        public const string HandlerUpdateDatabaseToolName = "handler_update_database";

        public static async Task<JsonRpcResponse> HandleDatabaseToolCreateAsync(JsonRpcRequest request, JsonElement args, HandlerManager? handlerManager, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object)
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create requires arguments object", jsonOptions);

            // Validate required fields
            if (!args.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameProp.GetString()))
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create requires arguments.name (string)", jsonOptions);

            if (!args.TryGetProperty("connection_string", out var connStrProp) || connStrProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(connStrProp.GetString()))
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create requires arguments.connection_string (string)", jsonOptions);

            if (!args.TryGetProperty("connector", out var connectorProp) || connectorProp.ValueKind != JsonValueKind.String)
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create requires arguments.connector (string)", jsonOptions);

            var connector = connectorProp.GetString() ?? string.Empty;
            if (!string.Equals(connector, "mssql", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(connector, "plsql", StringComparison.OrdinalIgnoreCase))
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create connector must be 'mssql' or 'plsql'", jsonOptions);

            if (!args.TryGetProperty("query", out var queryProp) || queryProp.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(queryProp.GetString()))
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create requires arguments.query (string)", jsonOptions);

            var query = queryProp.GetString() ?? string.Empty;
            if (!query.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                return ManagementToolHelpers.CreateToolError(request, "database_tool_create query must start with SELECT", jsonOptions);

            bool reload = true;
            if (args.TryGetProperty("reload_after_add", out var ra) && ra.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reload = ra.ValueKind == JsonValueKind.True;

            var store = ManagementToolHelpers.TryCreateHandlerConfigStore();
            if (store == null) return ManagementToolHelpers.CreateToolError(request, "HandlerConfigStore not available", jsonOptions);

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
            if (!res.Success) return ManagementToolHelpers.CreateToolError(request, $"{res.Code}: {res.Error}", jsonOptions);

            if (reload && handlerManager != null)
                handlerManager.ReloadHandlers(reloadPlugins: false);

            return ManagementToolHelpers.CreateToolOk(request, new { success = true, name = cfg.Name, type = "Database" }, jsonOptions);
        }
    }
}
