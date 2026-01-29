using System;
using System.Collections.Generic;
using System.Linq;
using Contextualizer.Core.FunctionProcessing.FunctionHelpers;

namespace Contextualizer.Core.FunctionProcessing.Parsing
{
    internal static class ChainedCallParser
    {
        public static List<(string Name, string[] Parameters)> ParseChainedCall(string functionCall, Dictionary<string, string>? context = null)
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

        public static List<(string Name, string[] Parameters)> ParsePrefixedChainedCall(string functionCall, Dictionary<string, string>? context = null)
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
    }
}
