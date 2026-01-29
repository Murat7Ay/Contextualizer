using System;
using System.Collections.Generic;
using Contextualizer.Core;
using Contextualizer.Core.FunctionProcessing.FunctionExecutors;
using Contextualizer.Core.FunctionProcessing.FunctionHelpers;

namespace Contextualizer.Core.FunctionProcessing.Parsing
{
    internal static class PipelineFunctionParser
    {
        public static string ProcessPipelineFunctions(string input, Dictionary<string, string>? context = null)
        {
            var result = input;
            var startIndex = 0;

            while (true)
            {
                var pipelineStart = result.IndexOf("$func:{{", startIndex);
                if (pipelineStart == -1) break;

                var contentStart = pipelineStart + 8; // Skip "$func:{{"
                var contentEnd = FunctionCallTokenizer.FindPipelineEnd(result, contentStart);

                if (contentEnd == -1) break;

                var pipelineContent = result.Substring(contentStart, contentEnd - contentStart);
                try
                {
                    var replacement = ProcessPipelineFunction(pipelineContent, context);
                    result = result.Substring(0, pipelineStart) + replacement + result.Substring(contentEnd + 2); // +2 for "}}"
                    startIndex = pipelineStart + replacement.Length;
                }
                catch (Exception ex)
                {
                    UserFeedback.ShowError($"Error processing pipeline function: {ex.Message}");
                    startIndex = contentEnd + 2;
                }
            }

            return result;
        }

        private static string ProcessPipelineFunction(string functionCall, Dictionary<string, string>? context = null)
        {
            try
            {
                var pipelineSteps = ParsePipelineSteps(functionCall, context);
                if (pipelineSteps.Count == 0)
                    return string.Empty;

                object result = null;

                for (int i = 0; i < pipelineSteps.Count; i++)
                {
                    var step = pipelineSteps[i];

                    if (i == 0)
                    {
                        if (step.IsLiteral)
                        {
                            result = PlaceholderResolver.ResolvePlaceholders(step.Value, context);
                        }
                        else
                        {
                            result = BaseFunctionExecutor.ProcessBaseFunction(step.Name, step.Parameters);
                        }
                    }
                    else
                    {
                        result = BaseFunctionExecutor.ProcessChainedMethod(result, step.Name, step.Parameters);
                    }
                }

                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error processing pipeline '{functionCall}': {ex.Message}");
                return string.Empty;
            }
        }

        private static List<PipelineStep> ParsePipelineSteps(string functionCall, Dictionary<string, string>? context = null)
        {
            var steps = new List<PipelineStep>();
            var parts = FunctionCallTokenizer.SplitPipelineSteps(functionCall);

            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i].Trim();
                if (string.IsNullOrEmpty(part)) continue;

                if (i == 0)
                {
                    if (!PlaceholderResolver.IsPlaceholderPattern(part) && (part.Contains('(') || FunctionCallTokenizer.IsKnownBaseFunction(part)))
                    {
                        var (name, parameters) = FunctionCallTokenizer.ParseFunctionPart(part, context);
                        steps.Add(new PipelineStep { Name = name, Parameters = parameters, IsLiteral = false });
                    }
                    else
                    {
                        var literalValue = part;
                        if (!PlaceholderResolver.IsPlaceholderPattern(part) &&
                            ((literalValue.StartsWith("\"") && literalValue.EndsWith("\"")) ||
                            (literalValue.StartsWith("'") && literalValue.EndsWith("'"))))
                        {
                            literalValue = literalValue.Substring(1, literalValue.Length - 2);
                        }
                        steps.Add(new PipelineStep { Value = literalValue, IsLiteral = true });
                    }
                }
                else
                {
                    var (name, parameters) = FunctionCallTokenizer.ParseFunctionPart(part, context);
                    steps.Add(new PipelineStep { Name = name, Parameters = parameters, IsLiteral = false });
                }
            }

            return steps;
        }

        private class PipelineStep
        {
            public string Name { get; set; } = string.Empty;
            public string[] Parameters { get; set; } = Array.Empty<string>();
            public string Value { get; set; } = string.Empty;
            public bool IsLiteral { get; set; } = false;
        }
    }
}
