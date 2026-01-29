using System.Text.Json;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class CommonSchemas
    {
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
    }
}
