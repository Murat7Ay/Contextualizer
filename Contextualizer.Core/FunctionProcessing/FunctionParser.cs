using System;
using System.Collections.Generic;
using System.Text;
using Contextualizer.Core.FunctionProcessing.FunctionHelpers;
using Contextualizer.Core.FunctionProcessing.FunctionExecutors;
using Contextualizer.Core;

namespace Contextualizer.Core.FunctionProcessing
{
    internal static class FunctionParser
    {
        public static string ProcessRegularFunctions(string input, Dictionary<string, string>? context = null)
        {
            var result = input;
            var startIndex = 0;

            while (true)
            {
                var funcStart = result.IndexOf("$func:", startIndex);
                if (funcStart == -1) break;

                // Skip pipeline functions that start with $func:{{
                if (funcStart + 6 < result.Length && result.Substring(funcStart + 6, 2) == "{{")
                {
                    startIndex = funcStart + 8;
                    continue;
                }

                var funcCallStart = funcStart + 6; // Skip "$func:"
                var funcCallEnd = FindFunctionEnd(result, funcCallStart);

                if (funcCallEnd == -1) break;

                var functionCall = result.Substring(funcCallStart, funcCallEnd - funcCallStart);
                var replacement = ProcessSingleFunction(functionCall, context);

                result = result.Substring(0, funcStart) + replacement + result.Substring(funcCallEnd);
                startIndex = funcStart + replacement.Length;
            }

            return result;
        }

        public static string ProcessPipelineFunctions(string input, Dictionary<string, string>? context = null)
        {
            var result = input;
            var startIndex = 0;

            while (true)
            {
                var pipelineStart = result.IndexOf("$func:{{", startIndex);
                if (pipelineStart == -1) break;

                var contentStart = pipelineStart + 8; // Skip "$func:{{"
                var contentEnd = FindPipelineEnd(result, contentStart);

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

        private static int FindFunctionEnd(string text, int startPos)
        {
            var parenDepth = 0;
            var i = startPos;

            while (i < text.Length)
            {
                var c = text[i];

                if (c == '(')
                {
                    parenDepth++;
                }
                else if (c == ')')
                {
                    parenDepth--;
                }
                else if (parenDepth == 0 && (char.IsWhiteSpace(c) || c == '$'))
                {
                    return i;
                }

                i++;
            }

            return text.Length;
        }

        private static int FindPipelineEnd(string text, int startPos)
        {
            var braceDepth = 0;
            var inQuotes = false;
            var quoteChar = '\0';

            for (int i = startPos; i < text.Length - 1; i++)
            {
                var c = text[i];
                var nextChar = text[i + 1];

                if (!inQuotes && (c == '"' || c == '\''))
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                }
                else if (!inQuotes)
                {
                    if (c == '{')
                    {
                        braceDepth++;
                    }
                    else if (c == '}')
                    {
                        if (braceDepth > 0)
                        {
                            braceDepth--;
                        }
                        else if (nextChar == '}')
                        {
                            return i;
                        }
                    }
                }
            }

            return -1;
        }

        private static string ProcessSingleFunction(string functionCall, Dictionary<string, string>? context = null)
        {
            try
            {
                if (functionCall.Contains('.'))
                {
                    if (functionCall.StartsWith("url.") || functionCall.StartsWith("web.") || functionCall.StartsWith("ip.") ||
                        functionCall.StartsWith("hash.") || functionCall.StartsWith("json.") || functionCall.StartsWith("string.") ||
                        functionCall.StartsWith("math.") || functionCall.StartsWith("array."))
                    {
                        var dotCount = functionCall.Count(c => c == '.');
                        var hasChaining = dotCount > 1;

                        if (hasChaining)
                        {
                            var parts = ParsePrefixedChainedCall(functionCall, context);
                            object result = null;

                            for (int i = 0; i < parts.Count; i++)
                            {
                                var part = parts[i];

                                if (i == 0)
                                {
                                    result = BaseFunctionExecutor.ProcessBaseFunction(part.Name, part.Parameters);
                                }
                                else
                                {
                                    result = BaseFunctionExecutor.ProcessChainedMethod(result, part.Name, part.Parameters);
                                }
                            }

                            return result?.ToString() ?? string.Empty;
                        }
                        else
                        {
                            var parenIndex = functionCall.IndexOf('(');
                            if (parenIndex != -1)
                            {
                                var functionName = functionCall.Substring(0, parenIndex);
                                var closeParenPos = ParameterParser.FindMatchingParen(functionCall, parenIndex);
                                var paramString = functionCall.Substring(parenIndex + 1, closeParenPos - parenIndex - 1);
                                var parameters = string.IsNullOrEmpty(paramString)
                                    ? Array.Empty<string>()
                                    : ParameterParser.ParseParameters(paramString, context);

                                return BaseFunctionExecutor.ProcessBaseFunction(functionName, parameters).ToString();
                            }
                            else
                            {
                                return BaseFunctionExecutor.ProcessBaseFunction(functionCall, Array.Empty<string>()).ToString();
                            }
                        }
                    }
                    else
                    {
                        var parts = ParseChainedCall(functionCall, context);
                        object result = null;

                        for (int i = 0; i < parts.Count; i++)
                        {
                            var part = parts[i];

                            if (i == 0)
                            {
                                result = BaseFunctionExecutor.ProcessBaseFunction(part.Name, part.Parameters);
                            }
                            else
                            {
                                result = BaseFunctionExecutor.ProcessChainedMethod(result, part.Name, part.Parameters);
                            }
                        }

                        return result?.ToString() ?? string.Empty;
                    }
                }
                else
                {
                    var parenPos = functionCall.IndexOf('(');
                    if (parenPos != -1)
                    {
                        var functionName = functionCall.Substring(0, parenPos);
                        var closeParenPos = ParameterParser.FindMatchingParen(functionCall, parenPos);
                        var paramString = functionCall.Substring(parenPos + 1, closeParenPos - parenPos - 1);
                        var parameters = string.IsNullOrEmpty(paramString)
                            ? Array.Empty<string>()
                            : ParameterParser.ParseParameters(paramString, context);

                        return BaseFunctionExecutor.ProcessBaseFunction(functionName, parameters).ToString();
                    }
                    else
                    {
                        return BaseFunctionExecutor.ProcessBaseFunction(functionCall, Array.Empty<string>()).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error processing function '{functionCall}': {ex.Message}");
                return $"$func:{functionCall}";
            }
        }

        private static List<(string Name, string[] Parameters)> ParsePrefixedChainedCall(string functionCall, Dictionary<string, string>? context = null)
        {
            var parts = new List<(string Name, string[] Parameters)>();

            var firstDotPos = functionCall.IndexOf('.');
            var firstParenPos = functionCall.IndexOf('(', firstDotPos);

            if (firstParenPos != -1)
            {
                var firstCloseParenPos = ParameterParser.FindMatchingParen(functionCall, firstParenPos);
                var baseFunctionCall = functionCall.Substring(0, firstCloseParenPos + 1);

                var baseParenPos = baseFunctionCall.IndexOf('(');
                var baseFunctionName = baseFunctionCall.Substring(0, baseParenPos);
                var baseParamString = baseFunctionCall.Substring(baseParenPos + 1, baseParenPos == firstCloseParenPos ? 0 : firstCloseParenPos - baseParenPos - 1);
                var baseParameters = string.IsNullOrEmpty(baseParamString) ? Array.Empty<string>() : ParameterParser.ParseParameters(baseParamString, context);

                parts.Add((baseFunctionName, baseParameters));

                var remainingCall = functionCall.Substring(firstCloseParenPos + 1);
                if (remainingCall.StartsWith("."))
                {
                    remainingCall = remainingCall.Substring(1);
                    var chainedParts = ParseChainedCall(remainingCall, context);
                    parts.AddRange(chainedParts);
                }
            }

            return parts;
        }

        private static List<(string Name, string[] Parameters)> ParseChainedCall(string functionCall, Dictionary<string, string>? context = null)
        {
            var parts = new List<(string Name, string[] Parameters)>();
            var currentPos = 0;

            while (currentPos < functionCall.Length)
            {
                var dotPos = functionCall.IndexOf('.', currentPos);
                var parenPos = functionCall.IndexOf('(', currentPos);

                string methodName;
                string[] parameters = Array.Empty<string>();

                if (parenPos != -1 && (dotPos == -1 || parenPos < dotPos))
                {
                    methodName = functionCall.Substring(currentPos, parenPos - currentPos);
                    var closeParenPos = ParameterParser.FindMatchingParen(functionCall, parenPos);
                    var paramString = functionCall.Substring(parenPos + 1, closeParenPos - parenPos - 1);

                    if (!string.IsNullOrEmpty(paramString))
                    {
                        parameters = ParameterParser.ParseParameters(paramString, context);
                    }

                    currentPos = closeParenPos + 1;
                    if (currentPos < functionCall.Length && functionCall[currentPos] == '.')
                        currentPos++;
                }
                else if (dotPos != -1)
                {
                    methodName = functionCall.Substring(currentPos, dotPos - currentPos);
                    currentPos = dotPos + 1;
                }
                else
                {
                    methodName = functionCall.Substring(currentPos);
                    currentPos = functionCall.Length;
                }

                parts.Add((methodName, parameters));
            }

            return parts;
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
            var parts = SplitPipelineSteps(functionCall);

            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i].Trim();
                if (string.IsNullOrEmpty(part)) continue;

                if (i == 0)
                {
                    if (!PlaceholderResolver.IsPlaceholderPattern(part) && (part.Contains('(') || IsKnownBaseFunction(part)))
                    {
                        var (name, parameters) = ParseFunctionPart(part, context);
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
                    var (name, parameters) = ParseFunctionPart(part, context);
                    steps.Add(new PipelineStep { Name = name, Parameters = parameters, IsLiteral = false });
                }
            }

            return steps;
        }

        private static List<string> SplitPipelineSteps(string functionCall)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            var parenDepth = 0;
            var inQuotes = false;
            var quoteChar = '\0';

            for (int i = 0; i < functionCall.Length; i++)
            {
                var c = functionCall[i];

                if (!inQuotes && (c == '"' || c == '\''))
                {
                    inQuotes = true;
                    quoteChar = c;
                    current.Append(c);
                }
                else if (inQuotes && c == quoteChar)
                {
                    inQuotes = false;
                    current.Append(c);
                }
                else if (!inQuotes)
                {
                    switch (c)
                    {
                        case '(':
                            parenDepth++;
                            current.Append(c);
                            break;
                        case ')':
                            parenDepth--;
                            current.Append(c);
                            break;
                        case '|':
                            if (parenDepth == 0)
                            {
                                parts.Add(current.ToString().Trim());
                                current.Clear();
                            }
                            else
                            {
                                current.Append(c);
                            }
                            break;
                        default:
                            current.Append(c);
                            break;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString().Trim());
            }

            return parts;
        }

        private static (string Name, string[] Parameters) ParseFunctionPart(string part, Dictionary<string, string>? context = null)
        {
            var parenPos = part.IndexOf('(');
            if (parenPos != -1)
            {
                var functionName = part.Substring(0, parenPos).Trim();
                var closeParenPos = ParameterParser.FindMatchingParen(part, parenPos);
                var paramString = part.Substring(parenPos + 1, closeParenPos - parenPos - 1);
                var parameters = string.IsNullOrEmpty(paramString)
                    ? Array.Empty<string>()
                    : ParameterParser.ParseParameters(paramString, context);

                return (functionName, parameters);
            }
            else
            {
                return (part.Trim(), Array.Empty<string>());
            }
        }

        private static bool IsKnownBaseFunction(string functionName)
        {
            var knownFunctions = new[] {
                "today", "now", "yesterday", "tomorrow", "guid", "random",
                "base64encode", "base64decode", "env", "username", "computername"
            };

            var lowerName = functionName.ToLower();
            return Array.Exists(knownFunctions, f => f == lowerName) ||
                   lowerName.StartsWith("hash.") ||
                   lowerName.StartsWith("url.") ||
                   lowerName.StartsWith("web.") ||
                   lowerName.StartsWith("ip.") ||
                   lowerName.StartsWith("json.") ||
                   lowerName.StartsWith("string.") ||
                   lowerName.StartsWith("math.") ||
                   lowerName.StartsWith("array.");
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
