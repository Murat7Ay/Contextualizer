using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contextualizer.PluginContracts;
using Contextualizer.Core.Services;

namespace Contextualizer.Core.Management
{
    internal static class HandlerStateManager
    {
        public static bool UpdateHandlerEnabledState(
            IHandler? handler,
            bool enabled,
            List<HandlerConfig> allConfigs,
            ISettingsService settingsService)
        {
            if (handler == null)
            {
                return false;
            }

            handler.HandlerConfig.Enabled = enabled;

            try
            {
                SaveHandlersToFile(allConfigs, settingsService);
                var logger = ServiceLocator.SafeGet<ILoggingService>();
                logger?.LogInfo($"Handler '{handler.HandlerConfig.Name}' {(enabled ? "enabled" : "disabled")}");
                return true;
            }
            catch (Exception ex)
            {
                var logger = ServiceLocator.SafeGet<ILoggingService>();
                logger?.LogError($"Failed to save handler state: {ex.Message}");
                return false;
            }
        }

        public static bool UpdateHandlerMcpEnabledState(
            IHandler? handler,
            bool mcpEnabled,
            List<HandlerConfig> allConfigs,
            ISettingsService settingsService)
        {
            if (handler == null)
            {
                return false;
            }

            handler.HandlerConfig.McpEnabled = mcpEnabled;

            try
            {
                SaveHandlersToFile(allConfigs, settingsService);
                var logger = ServiceLocator.SafeGet<ILoggingService>();
                logger?.LogInfo($"Handler '{handler.HandlerConfig.Name}' MCP {(mcpEnabled ? "enabled" : "disabled")}");
                return true;
            }
            catch (Exception ex)
            {
                var logger = ServiceLocator.SafeGet<ILoggingService>();
                logger?.LogError($"Failed to save handler MCP state: {ex.Message}");
                return false;
            }
        }

        public static void SaveHandlersToFile(List<HandlerConfig> allConfigs, ISettingsService settingsService)
        {
            var handlersObject = new { handlers = allConfigs };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(handlersObject, options);
            File.WriteAllText(settingsService.HandlersFilePath, json, Encoding.UTF8);
        }
    }
}
