using System;
using System.Collections.Generic;
using System.Linq;
using Contextualizer.Core;
using Contextualizer.Core.Services.DataTools;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpHelpers;
using WpfInteractionApp.Services.Mcp.McpModels;
using WpfInteractionApp.Services.Mcp.McpSchemas;
using WpfInteractionApp.Services.Mcp.McpToolHandlers;

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
        private const string RunShellToolName = "run_shell";
        private const string DataStatementsListToolName = "db_statements_list";
        private const string DataStatementGetToolName = "db_statement_get";
        private const string DbSelectStatementToolName = "db_select_statement";
        private const string DbScalarToolName = "db_scalar";
        private const string DbExecuteToolName = "db_execute";
        private const string DbProcedureExecuteToolName = "db_procedure_execute";

        public static List<McpTool> GetAllTools(bool includeManagementTools)
        {
            var tools = new List<McpTool>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddTool(McpTool tool)
            {
                if (tool == null || string.IsNullOrWhiteSpace(tool.Name))
                    return;

                if (names.Add(tool.Name))
                    tools.Add(tool);
            }

            // Built-in UI tools
            AddTool(new McpTool
            {
                Name = UiConfirmToolName,
                Description = "Show a confirmation dialog to the user. Optional 'details' object: {format: 'text'|'json', text?: string, json?: object}. Use details.text when format='text', details.json when format='json'. Returns { confirmed: boolean }.",
                InputSchema = SchemaBuilder.UiConfirmSchema()
            });

            AddTool(new McpTool
            {
                Name = UiUserInputsToolName,
                Description = """
Prompt the user for inputs sequentially. "user_inputs" is an array of objects with required fields:
- key (string): Unique key to store the user's input
- title (string): Dialog title
- message (string): Prompt message shown to user

Optional fields:
- validation_regex (string): Optional regex pattern for input validation
- is_required (boolean): If true, user must provide a value (default: true)
- default_value (string): Default value pre-filled in the input
- is_password (boolean): If true, shows a password input (masked)
- is_multi_line (boolean): If true, shows a multi-line text area
- is_file_picker (boolean): If true, shows a file browser dialog
- is_folder_picker (boolean): If true, shows a folder picker dialog
- file_extensions (array of strings): Optional list of allowed extensions (e.g. [".txt", ".json"])
- is_selection_list (boolean): If true, shows a dropdown. REQUIRES selection_items array
- selection_items (array): Required when is_selection_list=true. Array of {value: string, display: string} objects
- is_multi_select (boolean): If true, allows multiple selection. REQUIRES is_selection_list=true
- is_date / is_date_picker (boolean): If true, shows a date picker. Returns value in yyyy-MM-dd format (e.g., "2026-01-30")
- is_time / is_time_picker (boolean): If true, shows a time picker. Returns value in HH:mm format (24-hour, e.g., "14:30" for 2:30 PM, "09:05" for 9:05 AM)
- is_date_time / is_datetime_picker (boolean): If true, shows a date-time picker. Returns value in yyyy-MM-ddTHH:mm format (e.g., "2026-01-30T14:30")
- dependent_key (string): Key of a previous input whose value determines selection_items
- dependent_selection_item_map (object): Map where keys are values from dependent_key, values are {selection_items: [{value: string, display: string}], default_value?: string}. Example: {"TR": {"selection_items": [{"value": "34", "display": "Istanbul"}], "default_value": "34"}}
- config_target (string): Format "secrets.section.key" or "config.section.key" to save the input value

Optional "context" object: Initial context values (key-value pairs) for dependent selections.

Returns { cancelled: boolean, values: object }.
""",
                InputSchema = SchemaBuilder.UiUserInputsSchema()
            });

            AddTool(new McpTool
            {
                Name = UiNotifyToolName,
                Description = "Show a non-blocking notification/toast. Required: message (string). Optional: title (string), level (string: info|success|warning|error|critical|debug, default: info), durationSeconds (integer, 1-600, default: 5) or duration_seconds (accepted alias). Both durationSeconds and duration_seconds are accepted (camelCase preferred). Returns { ok: boolean }.",
                InputSchema = SchemaBuilder.UiNotifySchema()
            });

            AddTool(new McpTool
            {
                Name = UiShowMarkdownToolName,
                Description = "Show a markdown tab in the app (screen_id=markdown2). Required: markdown (string). Optional: title (string, default: 'Markdown'), autoFocus/auto_focus (boolean, default: false), bringToFront/bring_to_front (boolean, default: false). Both camelCase (autoFocus, bringToFront) and snake_case (auto_focus, bring_to_front) are accepted (camelCase preferred). Returns { shown: boolean }.",
                InputSchema = SchemaBuilder.UiShowMarkdownSchema()
            });

            // Shell execution tool
            AddTool(new McpTool
            {
                Name = RunShellToolName,
                Description = """
Execute a shell command via PowerShell on the host machine and return stdout, stderr, exit code.

Use this tool when you need to run terminal commands but the IDE terminal is unavailable or restricted. This is your bridge to the operating system.

Behavior:
- Runs "powershell.exe -NoProfile -NonInteractive -Command <command>"
- Returns JSON: { exit_code: int, stdout: string, stderr: string, timed_out: bool, elapsed_ms: long }
- exit_code 0 = success, non-zero = failure, -1 = timed out or killed
- stdout/stderr are truncated at 50000 chars if too long

Parameters:
- command (string, required): The PowerShell command to execute. Use semicolons (;) to chain commands, NOT && or ||. Examples: "git status", "Get-ChildItem C:\project", "dotnet build; dotnet test"
- working_directory (string, optional): Absolute path to set as CWD before running. Must exist. Example: "C:\Users\murat\source\repos\MyProject"
- timeout_seconds (integer, optional): Max seconds to wait (1-300, default 30). Process is killed on timeout.

Tips:
- For git operations, always set working_directory to the repo root
- Use "cmd /c <command>" if you need cmd.exe behavior instead of PowerShell
- Long output is automatically truncated with a marker showing total length
- Avoid interactive commands (Read-Host, pause, etc.) — they will hang until timeout
""",
                InputSchema = SchemaBuilder.RunShellSchema()
            });

            // Built-in data tools (optional; off by default)
            if (DataToolMcpSettings.IsGenericDataToolsEnabled())
            {
                AddTool(new McpTool
                {
                    Name = DataStatementsListToolName,
                    Description = "List configured data-tool definitions from the data-tools registry. Useful for discovery, filtering, and debugging supported providers and operations.",
                    InputSchema = SchemaBuilder.DataStatementsListSchema()
                });

                AddTool(new McpTool
                {
                    Name = DataStatementGetToolName,
                    Description = "Get a single data-tool definition by id, including its input schema, provider, operation, and parameter metadata.",
                    InputSchema = SchemaBuilder.DataStatementGetSchema()
                });

                AddTool(new McpTool
                {
                    Name = DbSelectStatementToolName,
                    Description = "Execute a registry-backed read/query statement by statement_id. Best for parameterized relational read operations (MSSQL/Oracle today; provider model is extensible).",
                    InputSchema = SchemaBuilder.GenericStatementSchema("statement_id", "Registered data-tool definition id for a SELECT-style statement.")
                });

                AddTool(new McpTool
                {
                    Name = DbScalarToolName,
                    Description = "Execute a registry-backed scalar statement by statement_id and return a single value.",
                    InputSchema = SchemaBuilder.GenericStatementSchema("statement_id", "Registered data-tool definition id for a scalar statement.")
                });

                AddTool(new McpTool
                {
                    Name = DbExecuteToolName,
                    Description = "Execute a registry-backed DML statement by statement_id and return affected_rows. Intended for INSERT/UPDATE/DELETE/MERGE style operations.",
                    InputSchema = SchemaBuilder.GenericStatementSchema("statement_id", "Registered data-tool definition id for an execute statement.")
                });

                AddTool(new McpTool
                {
                    Name = DbProcedureExecuteToolName,
                    Description = "Execute a registry-backed stored procedure by procedure_id. Procedure behavior is controlled by the definition result.mode and parameter metadata.",
                    InputSchema = SchemaBuilder.GenericStatementSchema("procedure_id", "Registered data-tool definition id for a stored procedure.")
                });
            }

            foreach (var rawSqlTool in DataToolMcpSettings.GetRawSqlTools())
            {
                AddTool(new McpTool
                {
                    Name = rawSqlTool.ToolName,
                    Description = rawSqlTool.BuildToolDescription(),
                    InputSchema = SchemaBuilder.RawSqlToolSchema(rawSqlTool.AllowedModes)
                });
            }

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

                    AddTool(new McpTool
                    {
                        Name = toolName,
                        Description = description,
                        InputSchema = schema
                    });
                }
            }

            // Registry-backed direct data tools. These are added after handler tools so
            // tools/list matches tools/call precedence when names collide.
            var dataToolRegistry = ServiceLocator.SafeGet<DataToolRegistryService>();
            if (dataToolRegistry != null)
            {
                foreach (var definition in dataToolRegistry.GetSupportedExposedDefinitions())
                {
                    AddTool(new McpTool
                    {
                        Name = DataToolRegistryService.ResolveToolName(definition),
                        Description =
                            !string.IsNullOrWhiteSpace(definition.Description)
                                ? definition.Description
                                : $"{definition.Operation} data tool ({definition.Provider})",
                        InputSchema = DataToolSchemas.SchemaForDefinition(definition)
                    });
                }
            }

            // Management tools
            if (includeManagementTools)
            {
                AddTool(new McpTool { Name = HandlersListToolName, Description = "List handlers from handlers.json (optionally include full configs).", InputSchema = SchemaBuilder.HandlersListSchema() });
                AddTool(new McpTool { Name = HandlersGetToolName, Description = "Get a single handler config by name.", InputSchema = SchemaBuilder.HandlersGetSchema() });
                AddTool(new McpTool { Name = DatabaseToolCreateToolName, Description = "Create a Database handler optimized for MCP usage. Automatically enables MCP and provides database-specific parameters.", InputSchema = SchemaBuilder.DatabaseToolCreateSchema() });
                AddTool(new McpTool { Name = HandlerUpdateDatabaseToolName, Description = "Update a Database handler by name (partial update).", InputSchema = SchemaBuilder.HandlerUpdateSchema() });
                AddTool(new McpTool { Name = HandlerCreateApiToolName, Description = "Create an Api handler (type=Api).", InputSchema = SchemaBuilder.HandlerCreateSchema() });
                AddTool(new McpTool { Name = HandlerUpdateApiToolName, Description = "Update an Api handler by name (partial update).", InputSchema = SchemaBuilder.HandlerUpdateSchema() });
                AddTool(new McpTool { Name = HandlerDeleteToolName, Description = "Delete an existing handler by name and optionally reload handlers.", InputSchema = SchemaBuilder.HandlerDeleteSchema() });
                AddTool(new McpTool { Name = HandlerReloadToolName, Description = "Reload handlers from handlers.json (optionally reload plugins).", InputSchema = SchemaBuilder.HandlerReloadSchema() });
                AddTool(new McpTool { Name = PluginsListToolName, Description = "List loaded plugin names (actions/validators/context_providers) and registered handler types.", InputSchema = SchemaBuilder.EmptyObjectSchema() });
                AddTool(new McpTool { Name = ConfigGetKeysToolName, Description = "List config keys (section.key).", InputSchema = SchemaBuilder.EmptyObjectSchema() });
                AddTool(new McpTool { Name = ConfigGetSectionToolName, Description = "Get a config section as key-value pairs (values may be masked).", InputSchema = SchemaBuilder.ConfigGetSectionSchema() });
                AddTool(new McpTool { Name = ConfigSetValueToolName, Description = "Set a config value in config.ini or secrets.ini.", InputSchema = SchemaBuilder.ConfigSetValueSchema() });
                AddTool(new McpTool { Name = ConfigReloadToolName, Description = "Reload config files from disk.", InputSchema = SchemaBuilder.EmptyObjectSchema() });
                AddTool(new McpTool { Name = HandlerDocsToolName, Description = "Handler authoring guide and examples (templating, conditions, seeder, MCP).", InputSchema = SchemaBuilder.HandlerDocsSchema() });
            }

            return tools;
        }
    }
}
