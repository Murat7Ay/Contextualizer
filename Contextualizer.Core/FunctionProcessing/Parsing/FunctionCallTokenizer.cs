using System;
using System.Collections.Generic;
using System.Text;
using Contextualizer.Core.FunctionProcessing.FunctionHelpers;

namespace Contextualizer.Core.FunctionProcessing.Parsing
{
    internal static class FunctionCallTokenizer
    {
        public static int FindFunctionEnd(string text, int startPos)
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

        public static int FindPipelineEnd(string text, int startPos)
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

        public static List<string> SplitPipelineSteps(string functionCall)
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

        public static (string Name, string[] Parameters) ParseFunctionPart(string part, Dictionary<string, string>? context = null)
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

        public static bool IsKnownBaseFunction(string functionName)
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
    }
}
