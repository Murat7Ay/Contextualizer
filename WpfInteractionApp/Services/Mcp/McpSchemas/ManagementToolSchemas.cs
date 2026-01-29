using System.Text.Json;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class ManagementToolSchemas
    {
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
    }
}
