using Contextualizer.Core.Services.DataTools;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class DataToolSchemas
    {
        public static JsonElement DataStatementsListSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "provider": { "type": "string", "description": "Optional provider filter (mssql, plsql, neo4j, redis, elasticsearch, etc.)" },
                "operation": { "type": "string", "description": "Optional operation filter (select, scalar, execute, procedure)" },
                "tag": { "type": "string", "description": "Optional tag filter" },
                "search": { "type": "string", "description": "Optional free-text search across id, name, description, and tags" }
              },
              "additionalProperties": false
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement DataStatementGetSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "id": { "type": "string", "description": "Statement/procedure definition id" }
              },
              "required": ["id"],
              "additionalProperties": false
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }

        public static JsonElement GenericStatementSchema(string idFieldName, string idDescription)
        {
            var schema = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object?>
                {
                    [idFieldName] = new Dictionary<string, object?>
                    {
                        ["type"] = "string",
                        ["description"] = idDescription
                    },
                    ["arguments"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["description"] = "Input parameters for the selected statement/procedure.",
                        ["additionalProperties"] = true
                    }
                },
                ["required"] = new[] { idFieldName },
                ["additionalProperties"] = true
            };

            return ToJsonElement(schema);
        }

        public static JsonElement RawSqlToolSchema(IEnumerable<string> allowedModes)
        {
            var normalizedModes = allowedModes
                .Where(mode => !string.IsNullOrWhiteSpace(mode))
                .Select(mode => mode.ToLowerInvariant())
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var properties = new Dictionary<string, object?>
            {
                ["sql"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["description"] = "Raw SQL text to execute against this tool's fixed configured connection."
                },
                ["parameters"] = new Dictionary<string, object?>
                {
                    ["type"] = "object",
                    ["description"] = "Optional parameter object. Flat top-level parameters also work.",
                    ["additionalProperties"] = true
                },
                ["max_rows"] = new Dictionary<string, object?>
                {
                    ["type"] = "integer",
                    ["description"] = "Maximum returned rows for select mode. Defaults to 200."
                },
                ["command_timeout_seconds"] = new Dictionary<string, object?>
                {
                    ["type"] = "integer",
                    ["description"] = "Optional command timeout override in seconds."
                }
            };

            if (normalizedModes.Length > 1)
            {
                properties["mode"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["description"] = "Execution mode for this raw SQL tool.",
                    ["enum"] = normalizedModes
                };
            }

            var schema = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "sql" },
                ["additionalProperties"] = true
            };

            return ToJsonElement(schema);
        }

        public static JsonElement SchemaForDefinition(DataToolDefinition definition)
        {
            if (definition.InputSchema.HasValue && definition.InputSchema.Value.ValueKind == JsonValueKind.Object)
                return definition.InputSchema.Value.Clone();

            var properties = new Dictionary<string, object?>();
            var required = new List<string>();

            foreach (var parameter in definition.Parameters.Where(IsInputParameter))
            {
                var property = new Dictionary<string, object?>
                {
                    ["type"] = NormalizeJsonType(parameter.Type),
                    ["description"] = parameter.Description
                };

                if (parameter.Enum != null && parameter.Enum.Count > 0)
                    property["enum"] = parameter.Enum;

                if (parameter.DefaultValue.HasValue)
                    property["default"] = DataToolArgumentConverter.FromJsonValue(parameter.DefaultValue.Value);

                if (string.Equals(parameter.Type, "array", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(parameter.ArrayItemType))
                {
                    property["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = NormalizeJsonType(parameter.ArrayItemType)
                    };
                }

                properties[parameter.Name] = property;

                if (parameter.Required)
                    required.Add(parameter.Name);
            }

            var schema = new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["additionalProperties"] = false
            };

            if (required.Count > 0)
                schema["required"] = required;

            return ToJsonElement(schema);
        }

        private static bool IsInputParameter(DataToolParameterDefinition parameter)
        {
            return !string.Equals(parameter.Direction, DataToolParameterDirections.Output, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeJsonType(string? type)
        {
            return type?.ToLowerInvariant() switch
            {
                "bool" => "boolean",
                "boolean" => "boolean",
                "int" => "integer",
                "integer" => "integer",
                "int32" => "integer",
                "int64" => "integer",
                "long" => "integer",
                "float" => "number",
                "double" => "number",
                "decimal" => "number",
                "array" => "array",
                "object" => "object",
                _ => "string"
            };
        }

        private static JsonElement ToJsonElement(object value)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value));
            return doc.RootElement.Clone();
        }
    }
}