using System;
using System.Collections.Generic;
using System.Text.Json;
using Contextualizer.Core;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers.Management
{
    internal static class ConfigManagementTools
    {
        public const string ConfigGetKeysToolName = "config_get_keys";
        public const string ConfigGetSectionToolName = "config_get_section";
        public const string ConfigSetValueToolName = "config_set_value";
        public const string ConfigReloadToolName = "config_reload";

        public static JsonRpcResponse HandleConfigGetKeys(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return ManagementToolHelpers.CreateToolError(request, "IConfigurationService is not available", jsonOptions);

            var keys = cfg.GetAllKeys();
            return ManagementToolHelpers.CreateToolOk(request, new { success = true, keys }, jsonOptions);
        }

        public static JsonRpcResponse HandleConfigGetSection(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("section", out var s) || s.ValueKind != JsonValueKind.String)
                return ManagementToolHelpers.CreateToolError(request, "config_get_section requires arguments.section", jsonOptions);

            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return ManagementToolHelpers.CreateToolError(request, "IConfigurationService is not available", jsonOptions);

            var section = s.GetString() ?? string.Empty;
            var values = cfg.GetSection(section);

            var masked = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var shouldMaskAll = section.Equals("api_keys", StringComparison.OrdinalIgnoreCase) ||
                                section.Equals("credentials", StringComparison.OrdinalIgnoreCase) ||
                                section.Equals("connections", StringComparison.OrdinalIgnoreCase);
            foreach (var kvp in values)
            {
                if (shouldMaskAll || kvp.Key.Contains("token", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("key", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("password", StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains("secret", StringComparison.OrdinalIgnoreCase))
                    masked[kvp.Key] = "***";
                else
                    masked[kvp.Key] = kvp.Value;
            }

            return ManagementToolHelpers.CreateToolOk(request, new { success = true, section, values = masked }, jsonOptions);
        }

        public static JsonRpcResponse HandleConfigSetValue(JsonRpcRequest request, JsonElement args, JsonSerializerOptions jsonOptions)
        {
            if (args.ValueKind != JsonValueKind.Object) return ManagementToolHelpers.CreateToolError(request, "config_set_value requires arguments", jsonOptions);
            if (!args.TryGetProperty("file_type", out var ft) || ft.ValueKind != JsonValueKind.String) return ManagementToolHelpers.CreateToolError(request, "config_set_value requires file_type", jsonOptions);
            if (!args.TryGetProperty("section", out var sec) || sec.ValueKind != JsonValueKind.String) return ManagementToolHelpers.CreateToolError(request, "config_set_value requires section", jsonOptions);
            if (!args.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String) return ManagementToolHelpers.CreateToolError(request, "config_set_value requires key", jsonOptions);
            if (!args.TryGetProperty("value", out var val)) return ManagementToolHelpers.CreateToolError(request, "config_set_value requires value", jsonOptions);

            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return ManagementToolHelpers.CreateToolError(request, "IConfigurationService is not available", jsonOptions);

            cfg.SetValue(ft.GetString() ?? "config", sec.GetString() ?? "", key.GetString() ?? "", val.ToString());
            return ManagementToolHelpers.CreateToolOk(request, new { success = true }, jsonOptions);
        }

        public static JsonRpcResponse HandleConfigReload(JsonRpcRequest request, JsonSerializerOptions jsonOptions)
        {
            var cfg = ServiceLocator.SafeGet<IConfigurationService>();
            if (cfg == null) return ManagementToolHelpers.CreateToolError(request, "IConfigurationService is not available", jsonOptions);
            cfg.ReloadConfig();
            return ManagementToolHelpers.CreateToolOk(request, new { success = true }, jsonOptions);
        }
    }
}
