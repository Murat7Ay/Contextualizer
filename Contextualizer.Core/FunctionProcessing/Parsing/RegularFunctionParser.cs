using System;
using System.Collections.Generic;
using System.Linq;
using Contextualizer.Core;
using Contextualizer.Core.FunctionProcessing.FunctionExecutors;
using Contextualizer.Core.FunctionProcessing.FunctionHelpers;

namespace Contextualizer.Core.FunctionProcessing.Parsing
{
    internal static class RegularFunctionParser
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
                var funcCallEnd = FunctionCallTokenizer.FindFunctionEnd(result, funcCallStart);

                if (funcCallEnd == -1) break;

                var functionCall = result.Substring(funcCallStart, funcCallEnd - funcCallStart);
                var replacement = ProcessSingleFunction(functionCall, context);

                result = result.Substring(0, funcStart) + replacement + result.Substring(funcCallEnd);
                startIndex = funcStart + replacement.Length;
            }

            return result;
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
                            var parts = ChainedCallParser.ParsePrefixedChainedCall(functionCall, context);
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
                        var parts = ChainedCallParser.ParseChainedCall(functionCall, context);
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
    }
}
