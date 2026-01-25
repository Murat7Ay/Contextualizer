using System;
using System.Text.Json;
using Contextualizer.Core;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class SchemaDefinitions
    {
        public static JsonElement DefaultSchemaForHandler(HandlerConfig cfg)
        {
            if (string.Equals(cfg.Type, FileHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                return SchemaBuilder.FilesSchema();
            }

            if (cfg.UserInputs != null && cfg.UserInputs.Count > 0)
            {
                return SchemaBuilder.UserInputsSchema(cfg.UserInputs);
            }

            return SchemaBuilder.DefaultTextSchema();
        }
    }
}
