using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class SchemaBuilder
    {
        public static JsonElement UiConfirmSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "message": { "type": "string" },
                "details": {
                  "type": "object",
                  "properties": {
                    "format": { "type": "string", "description": "text | json" },
                    "text": { "type": "string" },
                    "json": { "type": "object" }
                  }
                }
              },
              "required": ["message"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement UiUserInputsSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "context": { 
                  "type": "object", 
                  "additionalProperties": { "type": "string" },
                  "description": "Optional initial context values (key-value pairs)"
                },
                "user_inputs": {
                  "type": "array",
                  "description": "Array of input prompts to show to the user sequentially",
                  "items": {
                    "type": "object",
                    "properties": {
                      "key": { "type": "string", "description": "Unique key to store the user's input" },
                      "title": { "type": "string", "description": "Dialog title" },
                      "message": { "type": "string", "description": "Prompt message shown to user" },
                      "validation_regex": { "type": "string", "description": "Optional regex pattern for input validation" },
                      "is_required": { "type": "boolean", "description": "If true, user must provide a value (default: true)" },
                      "is_selection_list": { "type": "boolean", "description": "If true, shows a dropdown. Requires selection_items" },
                      "is_password": { "type": "boolean", "description": "If true, shows a password input (masked)" },
                      "is_multi_select": { "type": "boolean", "description": "If true, allows multiple selection. Requires is_selection_list" },
                      "is_file_picker": { "type": "boolean", "description": "If true, shows a file browser dialog" },
                      "file_extensions": { "type": "array", "items": { "type": "string" }, "description": "Optional list of allowed extensions (e.g. .txt, .json)" },
                      "is_folder_picker": { "type": "boolean", "description": "If true, shows a folder picker dialog" },
                      "is_multi_line": { "type": "boolean", "description": "If true, shows a multi-line text area" },
                      "is_date": { "type": "boolean", "description": "If true, shows a date picker (yyyy-MM-dd)" },
                      "is_date_picker": { "type": "boolean", "description": "Alias for is_date" },
                      "is_time": { "type": "boolean", "description": "If true, shows a time picker (HH:mm)" },
                      "is_time_picker": { "type": "boolean", "description": "Alias for is_time" },
                      "is_date_time": { "type": "boolean", "description": "If true, shows a date-time picker (yyyy-MM-ddTHH:mm)" },
                      "is_datetime_picker": { "type": "boolean", "description": "Alias for is_date_time" },
                      "default_value": { "type": "string", "description": "Default value pre-filled in the input" },
                      "selection_items": {
                        "type": "array",
                        "description": "Options for dropdown/list. Required when is_selection_list is true",
                        "items": {
                          "type": "object",
                          "properties": {
                            "value": { "type": "string", "description": "The value stored when selected" },
                            "display": { "type": "string", "description": "The text shown to the user" }
                          },
                          "required": ["value", "display"]
                        }
                      }
                    },
                    "required": ["key", "title", "message"]
                  }
                }
              },
              "required": ["user_inputs"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement UiNotifySchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "message": { "type": "string" },
                "level": { "type": "string" },
                "durationSeconds": { "type": "integer" },
                "duration_seconds": { "type": "integer" }
              },
              "required": ["message"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement UiShowMarkdownSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "title": { "type": "string" },
                "markdown": { "type": "string" },
                "autoFocus": { "type": "boolean" },
                "bringToFront": { "type": "boolean" },
                "auto_focus": { "type": "boolean" },
                "bring_to_front": { "type": "boolean" }
              },
              "required": ["markdown"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement UserInputsSchema(List<UserInputRequest> userInputs)
        {
            var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var input in userInputs)
            {
                if (input == null || string.IsNullOrWhiteSpace(input.Key))
                    continue;

                var prop = new Dictionary<string, object?>
                {
                    ["type"] = "string"
                };

                if (!string.IsNullOrWhiteSpace(input.Title))
                    prop["title"] = input.Title;

                if (!string.IsNullOrWhiteSpace(input.Message))
                    prop["description"] = input.Message;

                if (!string.IsNullOrWhiteSpace(input.DefaultValue))
                    prop["default"] = input.DefaultValue;

                properties[input.Key] = prop;

                if (input.IsRequired)
                    required.Add(input.Key);
            }

            var schemaObj = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["additionalProperties"] = new Dictionary<string, object?> { ["type"] = "string" }
            };

            if (required.Count > 0)
                schemaObj["required"] = required.ToArray();

            var json = JsonSerializer.Serialize(schemaObj);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        public static JsonElement DefaultTextSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "text": { "type": "string" }
              },
              "required": ["text"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement FilesSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "files": {
                  "type": "array",
                  "items": { "type": "string" }
                }
              },
              "required": ["files"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement EmptyObjectSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {},
              "additionalProperties": true
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlersListSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "include_configs": { "type": "boolean", "default": false }
              }
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlersGetSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": { "name": { "type": "string" } },
              "required": ["name"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlerCreateSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "handler_config": { "type": "object" },
                "reload_after_add": { "type": "boolean", "default": true }
              },
              "required": ["handler_config"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlerUpdateSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "handler_name": { "type": "string" },
                "updates": { "type": "object" },
                "reload_after_update": { "type": "boolean", "default": true }
              },
              "required": ["handler_name", "updates"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlerDeleteSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "handler_name": { "type": "string" },
                "reload_after_delete": { "type": "boolean", "default": true }
              },
              "required": ["handler_name"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlerReloadSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "reload_plugins": { "type": "boolean", "default": false }
              }
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement HandlerDocsSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "show_ui": { "type": "boolean", "default": false },
                "title": { "type": "string" },
                "auto_focus": { "type": "boolean", "default": false },
                "bring_to_front": { "type": "boolean", "default": false }
              }
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement ConfigGetSectionSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "section": { "type": "string" }
              },
              "required": ["section"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement ConfigSetValueSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "file_type": { "type": "string", "description": "config or secrets" },
                "section": { "type": "string" },
                "key": { "type": "string" },
                "value": { "type": "string" }
              },
              "required": ["file_type", "section", "key", "value"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement DatabaseToolCreateSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "name": { 
                  "type": "string", 
                  "description": "Handler name (unique identifier)" 
                },
                "connection_string": { 
                  "type": "string", 
                  "description": "Database connection string or $config:section.key reference (e.g., $config:connections.main_db)" 
                },
                "connector": { 
                  "type": "string", 
                  "enum": ["mssql", "plsql"],
                  "description": "Database connector type" 
                },
                "query": { 
                  "type": "string", 
                  "description": "SQL SELECT query only. Use @param (mssql) or :param (plsql) for parameters. p_input is automatically available from clipboard/MCP input. All MCP tool arguments are automatically available as SQL parameters with the same names." 
                },
                "description": { 
                  "type": "string", 
                  "description": "Optional handler description" 
                },
                "regex": { 
                  "type": "string", 
                  "description": "Optional regex pattern for clipboard content matching" 
                },
                "groups": { 
                  "type": "array", 
                  "items": { "type": "string" },
                  "description": "Optional regex group names to extract as SQL parameters" 
                },
                "command_timeout_seconds": {
                  "type": "integer",
                  "description": "Query execution timeout in seconds (default: 30)"
                },
                "connection_timeout_seconds": {
                  "type": "integer",
                  "description": "Connection timeout in seconds"
                },
                "max_pool_size": {
                  "type": "integer",
                  "description": "Maximum connection pool size"
                },
                "min_pool_size": {
                  "type": "integer",
                  "description": "Minimum connection pool size"
                },
                "disable_pooling": {
                  "type": "boolean",
                  "description": "Disable connection pooling"
                },
                "mcp_tool_name": { 
                  "type": "string", 
                  "description": "Custom MCP tool name (default: slugified handler name)" 
                },
                "mcp_description": { 
                  "type": "string", 
                  "description": "Custom MCP tool description (default: description or 'Database tool: {name}')" 
                },
                "mcp_input_schema": {
                  "type": "object",
                  "description": "JSON Schema object defining MCP tool input parameters. If omitted, defaults to { text: string } or derived from user_inputs."
                },
                "mcp_input_template": {
                  "type": "string",
                  "description": "Template to build ClipboardContent.Text from MCP arguments. Supports $(key), $config:, $file:, $func: placeholders."
                },
                "mcp_return_keys": {
                  "type": "array",
                  "items": { "type": "string" },
                  "description": "List of context keys to return in MCP response (default: [_formatted_output])"
                },
                "mcp_headless": {
                  "type": "boolean",
                  "default": false,
                  "description": "Run in headless mode (no interactive dialogs)"
                },
                "mcp_seed_overwrite": {
                  "type": "boolean",
                  "default": false,
                  "description": "Allow MCP seed context to overwrite existing context keys"
                },
                "reload_after_add": { 
                  "type": "boolean", 
                  "default": true,
                  "description": "Reload handlers after creation" 
                }
              },
              "required": ["name", "connection_string", "connector", "query"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }
    }
}
