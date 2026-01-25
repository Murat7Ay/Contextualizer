using System;
using System.Collections.Generic;
using System.Text;
using Contextualizer.Core.FunctionProcessing.FunctionHelpers;

namespace Contextualizer.Core.FunctionProcessing.FunctionHelpers
{
    internal static class ParameterParser
    {
        public static string[] ParseParameters(string paramString, Dictionary<string, string>? context = null)
        {
            var parameters = new List<string>();
            var current = new StringBuilder();
            var bracketDepth = 0;
            var parenDepth = 0;
            var inQuotes = false;
            var quoteChar = '\0';

            for (int i = 0; i < paramString.Length; i++)
            {
                var c = paramString[i];

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
                        case '{':
                        case '[':
                            bracketDepth++;
                            current.Append(c);
                            break;
                        case '}':
                        case ']':
                            bracketDepth--;
                            current.Append(c);
                            break;
                        case '(':
                            parenDepth++;
                            current.Append(c);
                            break;
                        case ')':
                            parenDepth--;
                            current.Append(c);
                            break;
                        case ',':
                            if (bracketDepth == 0 && parenDepth == 0)
                            {
                                var param = current.ToString().Trim();
                                // Strip quotes from parameters
                                if ((param.StartsWith("\"") && param.EndsWith("\"")) ||
                                    (param.StartsWith("'") && param.EndsWith("'")))
                                {
                                    param = param.Substring(1, param.Length - 2);
                                }
                                // Resolve placeholders in parameter
                                param = PlaceholderResolver.ResolvePlaceholders(param, context);
                                parameters.Add(param);
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
                var param = current.ToString().Trim();
                // Strip quotes from parameters
                if ((param.StartsWith("\"") && param.EndsWith("\"")) ||
                    (param.StartsWith("'") && param.EndsWith("'")))
                {
                    param = param.Substring(1, param.Length - 2);
                }
                // Resolve placeholders in parameter
                param = PlaceholderResolver.ResolvePlaceholders(param, context);
                parameters.Add(param);
            }

            return parameters.ToArray();
        }

        public static int FindMatchingParen(string text, int openPos)
        {
            var depth = 1;
            for (int i = openPos + 1; i < text.Length; i++)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')') depth--;

                if (depth == 0) return i;
            }
            return text.Length - 1;
        }
    }
}
