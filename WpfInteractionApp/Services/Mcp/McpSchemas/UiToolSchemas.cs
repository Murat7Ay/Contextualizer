using System;
using System.Collections.Generic;
using System.Text.Json;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class UiToolSchemas
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
    }
}
