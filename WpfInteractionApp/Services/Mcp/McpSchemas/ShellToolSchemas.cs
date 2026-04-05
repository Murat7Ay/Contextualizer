using System.Text.Json;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class ShellToolSchemas
    {
        public static JsonElement RunShellSchema()
        {
            const string schemaJson = """
            {
              "type": "object",
              "properties": {
                "command": {
                  "type": "string",
                  "description": "The full command string to execute in PowerShell. Use semicolons to chain multiple commands (e.g. 'cd C:\\project; git status'). Do NOT use && or || operators (PowerShell 5.1 does not support them)."
                },
                "working_directory": {
                  "type": "string",
                  "description": "Absolute path to set as the working directory before execution. Defaults to the Contextualizer app directory if not provided. Example: 'C:\\Users\\murat\\source\\repos\\MyProject'"
                },
                "timeout_seconds": {
                  "type": "integer",
                  "description": "Maximum execution time in seconds (1-300). The process is killed if it exceeds this limit. Default: 30. Use higher values for long-running operations like builds or installs."
                }
              },
              "required": ["command"]
            }
            """;
            using var doc = JsonDocument.Parse(schemaJson);
            return doc.RootElement.Clone();
        }
    }
}
