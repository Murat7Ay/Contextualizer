using System;
using System.Collections.Generic;
using System.Linq;
using Contextualizer.Core;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpHelpers;
using WpfInteractionApp.Services.Mcp.McpModels;
using WpfInteractionApp.Services.Mcp.McpSchemas;

namespace WpfInteractionApp.Services.Mcp
{
    internal static class McpToolRegistry
    {
        private const string UiConfirmToolName = "ui_confirm";
        private const string UiUserInputsToolName = "ui_user_inputs";
        private const string UiNotifyToolName = "ui_notify";
        private const string UiShowMarkdownToolName = "ui_show_markdown";

        private const string HandlersListToolName = "handlers_list";
        private const string HandlersGetToolName = "handlers_get";
        private const string HandlerCreateDatabaseToolName = "handler_create_database";
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

        public static List<McpTool> GetAllTools(bool includeManagementTools)
        {
            var tools = new List<McpTool>();

            // Built-in UI tools
            tools.Add(new McpTool
            {
                Name = UiConfirmToolName,
                Description = "Show a confirmation dialog to the user. Supports details.format = text|json. Returns { confirmed: boolean }.",
                InputSchema = SchemaBuilder.UiConfirmSchema()
            });

            tools.Add(new McpTool
            {
                Name = UiUserInputsToolName,
                Description = """
Prompt the user for inputs sequentially. "user_inputs" is an array of objects with required fields:
- key, title, message

Optional fields include:
- validation_regex, is_required, default_value
- is_password, is_multi_line
- is_file_picker, is_folder_picker, file_extensions
- is_selection_list (+ selection_items), is_multi_select
- is_date / is_date_picker, is_time / is_time_picker, is_date_time / is_datetime_picker
- dependent_key, dependent_selection_item_map
- config_target (secrets.section.key or config.section.key)

Returns { cancelled: boolean, values: object }.
""",
                InputSchema = SchemaBuilder.UiUserInputsSchema()
            });

            tools.Add(new McpTool
            {
                Name = UiNotifyToolName,
                Description = "Show a non-blocking notification/toast. level=info|success|warning|error|critical|debug. durationSeconds (or duration_seconds) in 1..600. Returns { ok: boolean }.",
                InputSchema = SchemaBuilder.UiNotifySchema()
            });

            tools.Add(new McpTool
            {
                Name = UiShowMarkdownToolName,
                Description = "Show a markdown tab in the app (screen_id=markdown2). Accepts autoFocus/bringToFront or auto_focus/bring_to_front. Returns { shown: boolean }.",
                InputSchema = SchemaBuilder.UiShowMarkdownSchema()
            });

            // Handler-based tools
            var handlerManager = ServiceLocator.SafeGet<HandlerManager>();
            if (handlerManager != null)
            {
                var configs = handlerManager.GetAllHandlerConfigs()
                    .Where(c => c.McpEnabled)
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var cfg in configs)
                {
                    var toolName = !string.IsNullOrWhiteSpace(cfg.McpToolName) ? cfg.McpToolName : McpHelper.Slugify(cfg.Name);
                    var description =
                        !string.IsNullOrWhiteSpace(cfg.McpDescription) ? cfg.McpDescription :
                        !string.IsNullOrWhiteSpace(cfg.Description) ? cfg.Description :
                        !string.IsNullOrWhiteSpace(cfg.Title) ? cfg.Title :
                        $"{cfg.Type} handler";

                    var schema = cfg.McpInputSchema ?? SchemaDefinitions.DefaultSchemaForHandler(cfg);

                    tools.Add(new McpTool
                    {
                        Name = toolName,
                        Description = description,
                        InputSchema = schema
                    });
                }
            }

            // Management tools
            if (includeManagementTools)
            {
                tools.Add(new McpTool { Name = HandlersListToolName, Description = "List handlers from handlers.json (optionally include full configs).", InputSchema = SchemaBuilder.HandlersListSchema() });
                tools.Add(new McpTool { Name = HandlersGetToolName, Description = "Get a single handler config by name.", InputSchema = SchemaBuilder.HandlersGetSchema() });
                tools.Add(new McpTool { Name = HandlerCreateDatabaseToolName, Description = "Create a Database handler (type=Database).", InputSchema = SchemaBuilder.HandlerCreateSchema() });
                tools.Add(new McpTool { Name = HandlerUpdateDatabaseToolName, Description = "Update a Database handler by name (partial update).", InputSchema = SchemaBuilder.HandlerUpdateSchema() });
                tools.Add(new McpTool { Name = HandlerCreateApiToolName, Description = "Create an Api handler (type=Api).", InputSchema = SchemaBuilder.HandlerCreateSchema() });
                tools.Add(new McpTool { Name = HandlerUpdateApiToolName, Description = "Update an Api handler by name (partial update).", InputSchema = SchemaBuilder.HandlerUpdateSchema() });
                tools.Add(new McpTool { Name = HandlerDeleteToolName, Description = "Delete an existing handler by name and optionally reload handlers.", InputSchema = SchemaBuilder.HandlerDeleteSchema() });
                tools.Add(new McpTool { Name = HandlerReloadToolName, Description = "Reload handlers from handlers.json (optionally reload plugins).", InputSchema = SchemaBuilder.HandlerReloadSchema() });
                tools.Add(new McpTool { Name = PluginsListToolName, Description = "List loaded plugin names (actions/validators/context_providers) and registered handler types.", InputSchema = SchemaBuilder.EmptyObjectSchema() });
                tools.Add(new McpTool { Name = ConfigGetKeysToolName, Description = "List config keys (section.key).", InputSchema = SchemaBuilder.EmptyObjectSchema() });
                tools.Add(new McpTool { Name = ConfigGetSectionToolName, Description = "Get a config section as key-value pairs (values may be masked).", InputSchema = SchemaBuilder.ConfigGetSectionSchema() });
                tools.Add(new McpTool { Name = ConfigSetValueToolName, Description = "Set a config value in config.ini or secrets.ini.", InputSchema = SchemaBuilder.ConfigSetValueSchema() });
                tools.Add(new McpTool { Name = ConfigReloadToolName, Description = "Reload config files from disk.", InputSchema = SchemaBuilder.EmptyObjectSchema() });
                tools.Add(new McpTool { Name = HandlerDocsToolName, Description = "Handler authoring guide and examples (templating, conditions, seeder, MCP).", InputSchema = SchemaBuilder.HandlerDocsSchema() });
            }

            return tools;
        }
    }
}
