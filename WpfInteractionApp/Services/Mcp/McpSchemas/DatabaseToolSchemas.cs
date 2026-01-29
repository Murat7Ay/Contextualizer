using System.Text.Json;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class DatabaseToolSchemas
    {
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
