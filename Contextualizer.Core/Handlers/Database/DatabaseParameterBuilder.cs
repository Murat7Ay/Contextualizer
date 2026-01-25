using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Contextualizer.PluginContracts;
using Dapper;

namespace Contextualizer.Core.Handlers.Database
{
    internal static class DatabaseParameterBuilder
    {
        private const int MAX_PARAMETER_LENGTH = 4000;

        public static void PrepareParametersForExecution(
            ClipboardContent clipboardContent,
            Regex? optionalRegex,
            HandlerConfig handlerConfig,
            Dictionary<string, string> parameters)
        {
            parameters.Clear();

            var seed = clipboardContent.SeedContext;
            var isMcpCall =
                seed != null &&
                seed.TryGetValue(ContextKey._trigger, out var t) &&
                string.Equals(t, "mcp", StringComparison.OrdinalIgnoreCase);

            if (clipboardContent.IsText && !string.IsNullOrEmpty(clipboardContent.Text))
            {
                string input = clipboardContent.Text;
                string safeInput = input.Length > MAX_PARAMETER_LENGTH
                    ? input.Substring(0, MAX_PARAMETER_LENGTH)
                    : input;

                if (input.Length > MAX_PARAMETER_LENGTH)
                {
                    UserFeedback.ShowWarning($"DatabaseHandler '{handlerConfig.Name}': Input truncated to {MAX_PARAMETER_LENGTH} characters");
                }

                parameters["p_input"] = safeInput;

                if (optionalRegex != null)
                {
                    DatabaseRegexProcessor.ProcessRegexMatch(optionalRegex, clipboardContent.Text, handlerConfig, parameters);
                }
            }

            if (seed != null && seed.Count > 0)
            {
                foreach (var kvp in seed)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                        continue;

                    if (kvp.Key.Equals(ContextKey._trigger, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (kvp.Key.StartsWith("_", StringComparison.Ordinal))
                        continue;

                    var v = kvp.Value ?? string.Empty;
                    if (v.Length > MAX_PARAMETER_LENGTH)
                        v = v.Substring(0, MAX_PARAMETER_LENGTH);

                    if (isMcpCall && handlerConfig.McpSeedOverwrite)
                    {
                        parameters[kvp.Key] = v;
                    }
                    else if (!parameters.ContainsKey(kvp.Key))
                    {
                        parameters[kvp.Key] = v;
                    }
                }

                if (!parameters.ContainsKey("p_input"))
                {
                    if (seed.TryGetValue("text", out var seedText) && !string.IsNullOrEmpty(seedText))
                    {
                        parameters["p_input"] = seedText.Length > MAX_PARAMETER_LENGTH ? seedText.Substring(0, MAX_PARAMETER_LENGTH) : seedText;
                    }
                    else
                    {
                        parameters["p_input"] = string.Empty;
                    }
                }
            }

            if (!parameters.ContainsKey("p_input"))
                parameters["p_input"] = string.Empty;
        }

        public static DynamicParameters CreateDynamicParameters(
            Dictionary<string, string> parameters,
            string connector)
        {
            string alias = GetParameterAlias(connector);
            var dynamicParameters = new DynamicParameters();
            foreach (var item in parameters)
            {
                dynamicParameters.Add($"{alias}{item.Key}", item.Value);
            }
            return dynamicParameters;
        }

        private static string GetParameterAlias(string connector)
        {
            return connector.ToLowerInvariant() switch
            {
                "mssql" => "@",
                "plsql" => ":",
                _ => throw new NotSupportedException($"Connector type '{connector}' is not supported.")
            };
        }
    }
}
