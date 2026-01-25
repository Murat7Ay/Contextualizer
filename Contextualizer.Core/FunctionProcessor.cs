using System;
using System.Collections.Generic;
using Contextualizer.Core.FunctionProcessing;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core
{
    public static class FunctionProcessor
    {
        public static string ProcessFunctions(string input, Dictionary<string, string>? context = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                var result = input;

                // First, process pipeline functions $func:{{ }}
                result = FunctionParser.ProcessPipelineFunctions(result, context);

                // Then, process regular functions $func:
                result = FunctionParser.ProcessRegularFunctions(result, context);

                return result;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error processing functions: {ex.Message}");

                var logger = ServiceLocator.Get<ILoggingService>();
                logger?.LogError("Function processing failed", ex, new Dictionary<string, object>
                {
                    ["input"] = input?.Substring(0, Math.Min(input?.Length ?? 0, 100)) ?? "null",
                    ["context_keys"] = context?.Keys.ToArray() ?? Array.Empty<string>()
                });

                return input;
            }
        }
    }
}
