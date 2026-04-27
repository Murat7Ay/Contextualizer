using Contextualizer.Core;
using Contextualizer.Core.Services.DataTools;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfInteractionApp.Services;

namespace WpfInteractionApp.Services.Mcp
{
    internal sealed class RawSqlToolDefinition
    {
        public string ToolName { get; init; } = string.Empty;
        public string Provider { get; init; } = DataToolProviders.MsSql;
        public string ConnectionTemplate { get; init; } = string.Empty;
        public IReadOnlyCollection<string> AllowedModes { get; init; } = Array.Empty<string>();
        public string? Description { get; init; }
        public string? SourceFileType { get; init; }

        public string BuildToolDescription()
        {
            var baseDescription = string.IsNullOrWhiteSpace(Description)
                ? $"Execute raw SQL against a fixed {Provider} connection."
                : Description.Trim();

            if (AllowedModes.Count == 1)
                return $"{baseDescription} Mode is fixed to {AllowedModes.First()}.";

            return $"{baseDescription} Allowed modes: {string.Join(", ", AllowedModes)}.";
        }
    }

    internal sealed class RawSqlToolConfigPayload
    {
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("connection")]
        public string? Connection { get; set; }

        [JsonPropertyName("connection_template")]
        public string? ConnectionTemplate { get; set; }

        [JsonPropertyName("modes")]
        public List<string>? Modes { get; set; }

        [JsonPropertyName("allowed_modes")]
        public List<string>? AllowedModes { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    internal static class DataToolMcpSettings
    {
        private const string RawSqlToolsSectionName = "mcp_raw_sql_tools";

        private static readonly string[] DefaultRawSqlModes = new[]
        {
            DataToolOperationKinds.Select,
            DataToolOperationKinds.Scalar,
        };

        public static bool IsGenericDataToolsEnabled()
        {
            var settingsService = ServiceLocator.SafeGet<SettingsService>();
            if (settingsService?.Settings?.McpSettings == null)
                return false;

            return settingsService.Settings.McpSettings.GenericDataToolsEnabled;
        }

        public static IReadOnlyList<RawSqlToolDefinition> GetRawSqlTools()
        {
            var configurationService = ServiceLocator.SafeGet<IConfigurationService>();
            if (configurationService == null || !configurationService.IsEnabled)
                return Array.Empty<RawSqlToolDefinition>();

            var section = configurationService.GetSection(RawSqlToolsSectionName);
            if (section.Count == 0)
                return Array.Empty<RawSqlToolDefinition>();

            return section
                .Select(entry => ParseRawSqlTool(entry.Key, entry.Value))
                .Where(definition => definition != null)
                .Cast<RawSqlToolDefinition>()
                .OrderBy(definition => definition.ToolName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static IReadOnlyList<RawSqlToolDefinition> GetRawSqlToolsForManagement()
        {
            var configurationService = ServiceLocator.SafeGet<IConfigurationService>();
            if (configurationService == null || !configurationService.IsEnabled)
                return Array.Empty<RawSqlToolDefinition>();

            var merged = new Dictionary<string, RawSqlToolDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in ParseSection(configurationService.GetSectionFromFile("config", RawSqlToolsSectionName), "config"))
                merged[definition.ToolName] = definition;

            foreach (var definition in ParseSection(configurationService.GetSectionFromFile("secrets", RawSqlToolsSectionName), "secrets"))
                merged[definition.ToolName] = definition;

            return merged.Values
                .OrderBy(definition => definition.ToolName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string SerializeRawSqlTool(RawSqlToolDefinition definition)
        {
            var payload = new RawSqlToolConfigPayload
            {
                Provider = definition.Provider,
                Connection = definition.ConnectionTemplate,
                Modes = definition.AllowedModes
                    .Where(mode => !string.IsNullOrWhiteSpace(mode))
                    .Select(mode => mode.Trim().ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Description = string.IsNullOrWhiteSpace(definition.Description) ? null : definition.Description.Trim()
            };

            return JsonSerializer.Serialize(payload);
        }

        private static IEnumerable<RawSqlToolDefinition> ParseSection(Dictionary<string, string> section, string sourceFileType)
        {
            return section
                .Select(entry => ParseRawSqlTool(entry.Key, entry.Value, sourceFileType))
                .Where(definition => definition != null)
                .Cast<RawSqlToolDefinition>();
        }

        private static RawSqlToolDefinition? ParseRawSqlTool(string? rawToolName, string? rawValue, string? sourceFileType = null)
        {
            var toolName = rawToolName?.Trim();
            if (string.IsNullOrWhiteSpace(toolName))
                return null;

            rawValue = rawValue?.Trim();
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            var provider = DataToolProviders.MsSql;
            var connectionTemplate = rawValue;
            string? modesSegment = null;
            string? description = null;

            if (rawValue.StartsWith("{", StringComparison.Ordinal))
            {
                var jsonPayload = ParseJsonPayload(rawValue);
                if (jsonPayload == null)
                    return null;

                provider = string.IsNullOrWhiteSpace(jsonPayload.Provider)
                    ? DataToolProviders.MsSql
                    : jsonPayload.Provider.Trim().ToLowerInvariant();
                connectionTemplate = jsonPayload.ConnectionTemplate?.Trim()
                    ?? jsonPayload.Connection?.Trim()
                    ?? string.Empty;
                description = jsonPayload.Description?.Trim();

                var modes = jsonPayload.Modes ?? jsonPayload.AllowedModes;
                if (modes != null && modes.Count > 0)
                    modesSegment = string.Join(',', modes);
            }

            else if (rawValue.Contains('|'))
            {
                var parts = rawValue.Split('|', 3, StringSplitOptions.None);
                provider = string.IsNullOrWhiteSpace(parts[0]) ? DataToolProviders.MsSql : parts[0].Trim().ToLowerInvariant();
                connectionTemplate = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                modesSegment = parts.Length > 2 ? parts[2] : null;
            }

            if (!DataToolProviders.IsRelational(provider) || string.IsNullOrWhiteSpace(connectionTemplate))
                return null;

            var allowedModes = ParseAllowedModes(modesSegment);
            if (allowedModes.Count == 0)
                return null;

            return new RawSqlToolDefinition
            {
                ToolName = toolName,
                Provider = provider,
                ConnectionTemplate = connectionTemplate,
                AllowedModes = allowedModes,
                Description = description,
                SourceFileType = sourceFileType
            };
        }

        private static RawSqlToolConfigPayload? ParseJsonPayload(string rawValue)
        {
            try
            {
                return JsonSerializer.Deserialize<RawSqlToolConfigPayload>(rawValue);
            }
            catch
            {
                return null;
            }
        }

        private static IReadOnlyCollection<string> ParseAllowedModes(string? modesSegment)
        {
            if (string.IsNullOrWhiteSpace(modesSegment))
                return DefaultRawSqlModes;

            var modes = modesSegment
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(mode => mode.ToLowerInvariant())
                .Where(mode =>
                    string.Equals(mode, DataToolOperationKinds.Select, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mode, DataToolOperationKinds.Scalar, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(mode, DataToolOperationKinds.Execute, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return modes.Length == 0 ? DefaultRawSqlModes : modes;
        }
    }
}