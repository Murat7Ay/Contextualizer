using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Text.Json;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core
{
    public static class FunctionProcessor
    {
        private static readonly Regex FunctionRegex = new(@"\$func:([^$\s]*(?:\([^)]*\))?(?:\.[^$\s]*(?:\([^)]*\))?)*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PlaceholderRegex = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);
        private static readonly Random Random = new();

        public static string ProcessFunctions(string input, Dictionary<string, string> context = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                var result = input;
                
                // First, process pipeline functions $func:{{ }}
                result = ProcessPipelineFunctions(result, context);
                
                // Then, process regular functions $func:
                result = ProcessRegularFunctions(result, context);

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

        private static string ProcessPipelineFunctions(string input, Dictionary<string, string> context = null)
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
                            // Found the closing "}}"
                            return i;
                        }
                    }
                }
            }

            return -1; // No matching close found
        }

        private static string ProcessRegularFunctions(string input, Dictionary<string, string> context = null)
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

        private static string ProcessSingleFunction(string functionCall, Dictionary<string, string> context = null)
        {
            try
            {
                // Check if it has chaining (multiple dots after function name)
                if (functionCall.Contains('.'))
                {
                    // Check if it's a prefixed function (like url.encode, math.add, etc.)
                    if (functionCall.StartsWith("url.") || functionCall.StartsWith("web.") || functionCall.StartsWith("ip.") || 
                        functionCall.StartsWith("hash.") || functionCall.StartsWith("json.") || functionCall.StartsWith("string.") || 
                        functionCall.StartsWith("math.") || functionCall.StartsWith("array."))
                    {
                        // Count dots to see if there's chaining
                        var dotCount = functionCall.Count(c => c == '.');
                        var hasChaining = dotCount > 1;
                        
                        if (hasChaining) // Has chaining
                        {
                            // Parse prefixed function chaining specially
                            var parts = ParsePrefixedChainedCall(functionCall, context);
                            object result = null;

                            for (int i = 0; i < parts.Count; i++)
                            {
                                var part = parts[i];

                                if (i == 0)
                                {
                                    result = ProcessBaseFunction(part.Name, part.Parameters);
                                }
                                else
                                {
                                    result = ProcessChainedMethod(result, part.Name, part.Parameters);
                                }
                            }

                            return result?.ToString() ?? string.Empty;
                        }
                        else
                        {
                            // No chaining, use direct processing
                            var parenIndex = functionCall.IndexOf('(');
                            if (parenIndex != -1)
                            {
                                var functionName = functionCall.Substring(0, parenIndex);
                                var closeParenPos = FindMatchingParen(functionCall, parenIndex);
                                var paramString = functionCall.Substring(parenIndex + 1, closeParenPos - parenIndex - 1);
                                var parameters = string.IsNullOrEmpty(paramString) 
                                    ? new string[0] 
                                    : ParseParameters(paramString, context);
                                
                                return ProcessBaseFunction(functionName, parameters).ToString();
                            }
                            else
                            {
                                return ProcessBaseFunction(functionCall, new string[0]).ToString();
                            }
                        }
                    }
                    else
                    {
                        // Parse chained function calls using better parsing
                        var parts = ParseChainedCall(functionCall, context);
                        object result = null;

                        for (int i = 0; i < parts.Count; i++)
                        {
                            var part = parts[i];

                            if (i == 0)
                            {
                                result = ProcessBaseFunction(part.Name, part.Parameters);
                            }
                            else
                            {
                                result = ProcessChainedMethod(result, part.Name, part.Parameters);
                            }
                        }

                        return result?.ToString() ?? string.Empty;
                    }
                }
                else
                {
                    // No dots, simple function
                    var parenPos = functionCall.IndexOf('(');
                    if (parenPos != -1)
                    {
                        var functionName = functionCall.Substring(0, parenPos);
                        var closeParenPos = FindMatchingParen(functionCall, parenPos);
                        var paramString = functionCall.Substring(parenPos + 1, closeParenPos - parenPos - 1);
                        var parameters = string.IsNullOrEmpty(paramString) 
                            ? new string[0] 
                            : ParseParameters(paramString, context);
                        
                        return ProcessBaseFunction(functionName, parameters).ToString();
                    }
                    else
                    {
                        return ProcessBaseFunction(functionCall, new string[0]).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error processing function '{functionCall}': {ex.Message}");
                return $"$func:{functionCall}";
            }
        }

        private static int FindMatchingParen(string text, int openPos)
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

        private static string[] ParseParameters(string paramString, Dictionary<string, string> context = null)
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
                                param = ResolvePlaceholders(param, context);
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
                param = ResolvePlaceholders(param, context);
                parameters.Add(param);
            }

            return parameters.ToArray();
        }

        private static List<(string Name, string[] Parameters)> ParsePrefixedChainedCall(string functionCall, Dictionary<string, string> context = null)
        {
            var parts = new List<(string Name, string[] Parameters)>();
            
            // Find the first method with its parameters
            var firstDotPos = functionCall.IndexOf('.');
            var firstParenPos = functionCall.IndexOf('(', firstDotPos);
            
            if (firstParenPos != -1)
            {
                var firstCloseParenPos = FindMatchingParen(functionCall, firstParenPos);
                var baseFunctionCall = functionCall.Substring(0, firstCloseParenPos + 1);
                
                // Parse base function
                var baseParenPos = baseFunctionCall.IndexOf('(');
                var baseFunctionName = baseFunctionCall.Substring(0, baseParenPos);
                var baseParamString = baseFunctionCall.Substring(baseParenPos + 1, baseParenPos == firstCloseParenPos ? 0 : firstCloseParenPos - baseParenPos - 1);
                var baseParameters = string.IsNullOrEmpty(baseParamString) ? new string[0] : ParseParameters(baseParamString, context);
                
                parts.Add((baseFunctionName, baseParameters));
                
                // Parse remaining chained methods
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

        private static List<(string Name, string[] Parameters)> ParseChainedCall(string functionCall, Dictionary<string, string> context = null)
        {
            var parts = new List<(string Name, string[] Parameters)>();
            var currentPos = 0;

            while (currentPos < functionCall.Length)
            {
                var dotPos = functionCall.IndexOf('.', currentPos);
                var parenPos = functionCall.IndexOf('(', currentPos);

                string methodName;
                string[] parameters = new string[0];

                if (parenPos != -1 && (dotPos == -1 || parenPos < dotPos))
                {
                    // Method with parameters
                    methodName = functionCall.Substring(currentPos, parenPos - currentPos);
                    var closeParenPos = FindMatchingParen(functionCall, parenPos);
                    var paramString = functionCall.Substring(parenPos + 1, closeParenPos - parenPos - 1);
                    
                    if (!string.IsNullOrEmpty(paramString))
                    {
                        parameters = ParseParameters(paramString, context);
                    }
                    
                    currentPos = closeParenPos + 1;
                    if (currentPos < functionCall.Length && functionCall[currentPos] == '.')
                        currentPos++;
                }
                else if (dotPos != -1)
                {
                    // Method without parameters, but there's more chain
                    methodName = functionCall.Substring(currentPos, dotPos - currentPos);
                    currentPos = dotPos + 1;
                }
                else
                {
                    // Last method without parameters
                    methodName = functionCall.Substring(currentPos);
                    currentPos = functionCall.Length;
                }

                parts.Add((methodName, parameters));
            }

            return parts;
        }

        private static string ProcessPipelineFunction(string functionCall, Dictionary<string, string> context = null)
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
                        // First step - could be a base function or a literal value
                        if (step.IsLiteral)
                        {
                            // If the literal value contains placeholders, resolve them from context
                            result = ResolvePlaceholders(step.Value, context);
                        }
                        else
                        {
                            result = ProcessBaseFunction(step.Name, step.Parameters);
                        }
                    }
                    else
                    {
                        // Subsequent steps - apply as chained methods
                        result = ProcessChainedMethod(result, step.Name, step.Parameters);
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

        private static List<PipelineStep> ParsePipelineSteps(string functionCall, Dictionary<string, string> context = null)
        {
            var steps = new List<PipelineStep>();
            var parts = SplitPipelineSteps(functionCall);

            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i].Trim();
                if (string.IsNullOrEmpty(part)) continue;

                if (i == 0)
                {
                    // First part - check if it's a function call, placeholder, or literal value
                    if (!IsPlaceholderPattern(part) && (part.Contains('(') || IsKnownBaseFunction(part)))
                    {
                        // It's a function call
                        var (name, parameters) = ParseFunctionPart(part, context);
                        steps.Add(new PipelineStep { Name = name, Parameters = parameters, IsLiteral = false });
                    }
                    else
                    {
                        // It's a literal value or placeholder
                        var literalValue = part;
                        // Strip quotes from literal strings (but not from placeholders)
                        if (!IsPlaceholderPattern(part) && 
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
                    // Subsequent parts - must be function calls
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

        private static (string Name, string[] Parameters) ParseFunctionPart(string part, Dictionary<string, string> context = null)
        {
            var parenPos = part.IndexOf('(');
            if (parenPos != -1)
            {
                var functionName = part.Substring(0, parenPos).Trim();
                var closeParenPos = FindMatchingParen(part, parenPos);
                var paramString = part.Substring(parenPos + 1, closeParenPos - parenPos - 1);
                var parameters = string.IsNullOrEmpty(paramString) 
                    ? new string[0] 
                    : ParseParameters(paramString, context);
                
                return (functionName, parameters);
            }
            else
            {
                return (part.Trim(), new string[0]);
            }
        }

        private static bool IsKnownBaseFunction(string functionName)
        {
            var knownFunctions = new[] {
                "today", "now", "yesterday", "tomorrow", "guid", "random",
                "base64encode", "base64decode", "env", "username", "computername"
            };

            var lowerName = functionName.ToLower();
            return knownFunctions.Contains(lowerName) ||
                   lowerName.StartsWith("hash.") ||
                   lowerName.StartsWith("url.") ||
                   lowerName.StartsWith("web.") ||
                   lowerName.StartsWith("ip.") ||
                   lowerName.StartsWith("json.") ||
                   lowerName.StartsWith("string.") ||
                   lowerName.StartsWith("math.") ||
                   lowerName.StartsWith("array.");
        }

        private static bool IsPlaceholderPattern(string text)
        {
            return PlaceholderRegex.IsMatch(text);
        }

        private class PipelineStep
        {
            public string Name { get; set; } = string.Empty;
            public string[] Parameters { get; set; } = new string[0];
            public string Value { get; set; } = string.Empty;
            public bool IsLiteral { get; set; } = false;
        }

        private static object ProcessBaseFunction(string functionName, string[] parameters)
        {
            return functionName.ToLower() switch
            {
                "today" => DateTime.Today,
                "now" => DateTime.Now,
                "yesterday" => DateTime.Today.AddDays(-1),
                "tomorrow" => DateTime.Today.AddDays(1),
                "guid" => Guid.NewGuid(),
                "random" => ProcessRandomFunction(parameters),
                "base64encode" => ProcessBase64Encode(parameters),
                "base64decode" => ProcessBase64Decode(parameters),
                "env" => ProcessEnvironmentVariable(parameters),
                "username" => Environment.UserName,
                "computername" => Environment.MachineName,
                _ when functionName.StartsWith("hash.") => ProcessHashFunction(functionName, parameters),
                _ when functionName.StartsWith("url.") => ProcessUrlFunction(functionName, parameters),
                _ when functionName.StartsWith("web.") => ProcessWebFunction(functionName, parameters),
                _ when functionName.StartsWith("ip.") => ProcessIpFunction(functionName, parameters),
                _ when functionName.StartsWith("json.") => ProcessJsonFunction(functionName, parameters),
                _ when functionName.StartsWith("string.") => ProcessStringFunction(functionName, parameters),
                _ when functionName.StartsWith("math.") => ProcessMathFunction(functionName, parameters),
                _ when functionName.StartsWith("array.") => ProcessArrayFunction(functionName, parameters),
                _ => throw new NotSupportedException($"Function '{functionName}' is not supported")
            };
        }

        private static object ProcessChainedMethod(object input, string methodName, string[] parameters)
        {
            // Handle prefixed method names (e.g., string.upper, array.get, math.add)
            string actualMethodName = methodName;
            
            if (methodName.StartsWith("string."))
            {
                actualMethodName = methodName.Substring(7); // Remove "string." prefix
                return ProcessStringMethod(input.ToString(), actualMethodName, parameters);
            }
            else if (methodName.StartsWith("array."))
            {
                actualMethodName = methodName.Substring(6); // Remove "array." prefix
                return ProcessArrayMethod(input.ToString(), actualMethodName, parameters);
            }
            else if (methodName.StartsWith("math."))
            {
                actualMethodName = methodName.Substring(5); // Remove "math." prefix
                // Convert input to number and process math operation
                if (double.TryParse(input.ToString(), out var number))
                {
                    // For math operations, we need to call the base math function with the input as first parameter
                    var newParams = new string[parameters.Length + 1];
                    newParams[0] = input.ToString();
                    Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                    return ProcessMathFunction($"math.{actualMethodName}", newParams);
                }
                return input.ToString();
            }
            else if (methodName.StartsWith("url."))
            {
                actualMethodName = methodName.Substring(4); // Remove "url." prefix
                var newParams = new string[parameters.Length + 1];
                newParams[0] = input.ToString();
                Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                return ProcessUrlFunction($"url.{actualMethodName}", newParams);
            }
            else if (methodName.StartsWith("hash."))
            {
                actualMethodName = methodName.Substring(5); // Remove "hash." prefix
                var newParams = new string[parameters.Length + 1];
                newParams[0] = input.ToString();
                Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                return ProcessHashFunction($"hash.{actualMethodName}", newParams);
            }
            else if (methodName.StartsWith("json."))
            {
                actualMethodName = methodName.Substring(5); // Remove "json." prefix
                var newParams = new string[parameters.Length + 1];
                newParams[0] = input.ToString();
                Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                return ProcessJsonFunction($"json.{actualMethodName}", newParams);
            }
            else if (methodName == "base64encode")
            {
                return ProcessBase64Encode(new[] { input.ToString() });
            }
            else if (methodName == "base64decode")
            {
                return ProcessBase64Decode(new[] { input.ToString() });
            }
            
            // Handle non-prefixed method names based on input type
            return input switch
            {
                DateTime dateTime => ProcessDateTimeMethod(dateTime, actualMethodName, parameters),
                string str when str.StartsWith("[") && str.EndsWith("]") => ProcessArrayMethod(str, actualMethodName, parameters),
                string str => ProcessStringMethod(str, actualMethodName, parameters),
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported for type '{input?.GetType().Name}'")
            };
        }

        private static object ProcessDateTimeMethod(DateTime dateTime, string methodName, string[] parameters)
        {
            return methodName.ToLower() switch
            {
                "add" => ProcessDateTimeAdd(dateTime, parameters),
                "subtract" => ProcessDateTimeSubtract(dateTime, parameters),
                "format" => ProcessDateTimeFormat(dateTime, parameters),
                _ => throw new NotSupportedException($"DateTime method '{methodName}' is not supported")
            };
        }

        private static DateTime ProcessDateTimeAdd(DateTime dateTime, string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Add method requires 2 parameters: unit and value");

            var unit = parameters[0].ToLower();
            if (!int.TryParse(parameters[1], out var value))
                throw new ArgumentException($"Invalid numeric value: {parameters[1]}");

            return unit switch
            {
                "days" or "day" => dateTime.AddDays(value),
                "hours" or "hour" => dateTime.AddHours(value),
                "minutes" or "minute" => dateTime.AddMinutes(value),
                "seconds" or "second" => dateTime.AddSeconds(value),
                "months" or "month" => dateTime.AddMonths(value),
                "years" or "year" => dateTime.AddYears(value),
                _ => throw new ArgumentException($"Unsupported time unit: {unit}")
            };
        }

        private static DateTime ProcessDateTimeSubtract(DateTime dateTime, string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Subtract method requires 2 parameters: unit and value");

            var unit = parameters[0].ToLower();
            if (!int.TryParse(parameters[1], out var value))
                throw new ArgumentException($"Invalid numeric value: {parameters[1]}");

            return unit switch
            {
                "days" or "day" => dateTime.AddDays(-value),
                "hours" or "hour" => dateTime.AddHours(-value),
                "minutes" or "minute" => dateTime.AddMinutes(-value),
                "seconds" or "second" => dateTime.AddSeconds(-value),
                "months" or "month" => dateTime.AddMonths(-value),
                "years" or "year" => dateTime.AddYears(-value),
                _ => throw new ArgumentException($"Unsupported time unit: {unit}")
            };
        }

        private static string ProcessDateTimeFormat(DateTime dateTime, string[] parameters)
        {
            if (parameters.Length == 0)
                return dateTime.ToString();

            var format = parameters[0];
            return dateTime.ToString(format, CultureInfo.InvariantCulture);
        }

        private static int ProcessRandomFunction(string[] parameters)
        {
            return parameters.Length switch
            {
                0 => Random.Next(),
                1 when int.TryParse(parameters[0], out var max) => Random.Next(max),
                2 when int.TryParse(parameters[0], out var min) && int.TryParse(parameters[1], out var max) => Random.Next(min, max),
                _ => throw new ArgumentException("Random function accepts 0, 1, or 2 integer parameters")
            };
        }

        private static string ProcessBase64Encode(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Base64Encode requires 1 parameter: text to encode");

            var bytes = Encoding.UTF8.GetBytes(parameters[0]);
            return Convert.ToBase64String(bytes);
        }

        private static string ProcessBase64Decode(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Base64Decode requires 1 parameter: base64 text to decode");

            var bytes = Convert.FromBase64String(parameters[0]);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string ProcessEnvironmentVariable(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Environment variable function requires 1 parameter: variable name");

            return Environment.GetEnvironmentVariable(parameters[0]) ?? string.Empty;
        }

        private static string ProcessHashFunction(string functionName, string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Hash functions require 1 parameter: text to hash");

            var text = parameters[0];
            var hashType = functionName.Substring(5).ToLower(); // Remove "hash." prefix

            return hashType switch
            {
                "md5" => ComputeMD5Hash(text),
                "sha256" => ComputeSHA256Hash(text),
                _ => throw new NotSupportedException($"Hash type '{hashType}' is not supported")
            };
        }

        private static string ComputeMD5Hash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        private static string ComputeSHA256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        private static string ProcessUrlFunction(string functionName, string[] parameters)
        {
            var urlFunction = functionName.Substring(4); // Remove "url." prefix

            return urlFunction.ToLower() switch
            {
                "encode" => ProcessUrlEncode(parameters),
                "decode" => ProcessUrlDecode(parameters),
                "domain" => ProcessUrlDomain(parameters),
                "path" => ProcessUrlPath(parameters),
                "query" => ProcessUrlQuery(parameters),
                "combine" => ProcessUrlCombine(parameters),
                _ => throw new NotSupportedException($"URL function '{urlFunction}' is not supported")
            };
        }

        private static string ProcessWebFunction(string functionName, string[] parameters)
        {
            var webFunction = functionName.Substring(4); // Remove "web." prefix

            return webFunction.ToLower() switch
            {
                "get" => ProcessWebGet(parameters),
                "post" => ProcessWebPost(parameters),
                "put" => ProcessWebPut(parameters),
                "delete" => ProcessWebDelete(parameters),
                _ => throw new NotSupportedException($"Web function '{webFunction}' is not supported")
            };
        }

        private static string ProcessIpFunction(string functionName, string[] parameters)
        {
            var ipFunction = functionName.Substring(3); // Remove "ip." prefix

            return ipFunction.ToLower() switch
            {
                "local" => ProcessIpLocal(),
                "public" => ProcessIpPublic(),
                "isprivate" => ProcessIpIsPrivate(parameters),
                "ispublic" => ProcessIpIsPublic(parameters),
                _ => throw new NotSupportedException($"IP function '{ipFunction}' is not supported")
            };
        }

        // URL Functions
        private static string ProcessUrlEncode(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("URL encode requires 1 parameter: text to encode");

            return WebUtility.UrlEncode(parameters[0]);
        }

        private static string ProcessUrlDecode(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("URL decode requires 1 parameter: text to decode");

            return WebUtility.UrlDecode(parameters[0]);
        }

        private static string ProcessUrlDomain(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("URL domain requires 1 parameter: URL");

            try
            {
                var uri = new Uri(parameters[0]);
                return uri.Host;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessUrlPath(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("URL path requires 1 parameter: URL");

            try
            {
                var uri = new Uri(parameters[0]);
                return uri.AbsolutePath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessUrlQuery(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("URL query requires 1 parameter: URL");

            try
            {
                var uri = new Uri(parameters[0]);
                return uri.Query.TrimStart('?');
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessUrlCombine(string[] parameters)
        {
            if (parameters.Length < 2)
                throw new ArgumentException("URL combine requires at least 2 parameters: base URL and path segments");

            try
            {
                var baseUrl = parameters[0].TrimEnd('/');
                for (int i = 1; i < parameters.Length; i++)
                {
                    var segment = parameters[i].Trim('/');
                    baseUrl += "/" + segment;
                }
                return baseUrl;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Web Functions
        private static string ProcessWebGet(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Web GET requires 1 parameter: URL");

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var response = client.GetStringAsync(parameters[0]).Result;
                return response;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Web GET error: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ProcessWebPost(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Web POST requires 2 parameters: URL and data");

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(parameters[1], Encoding.UTF8, "application/json");
                var response = client.PostAsync(parameters[0], content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Web POST error: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ProcessWebPut(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Web PUT requires 2 parameters: URL and data");

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(parameters[1], Encoding.UTF8, "application/json");
                var response = client.PutAsync(parameters[0], content).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Web PUT error: {ex.Message}");
                return string.Empty;
            }
        }

        private static string ProcessWebDelete(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Web DELETE requires 1 parameter: URL");

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var response = client.DeleteAsync(parameters[0]).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Web DELETE error: {ex.Message}");
                return string.Empty;
            }
        }

        // IP Functions
        private static string ProcessIpLocal()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                var localIp = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return localIp?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private static string ProcessIpPublic()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = client.GetStringAsync("https://api.ipify.org").Result;
                return response.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessIpIsPrivate(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("IP isPrivate requires 1 parameter: IP address");

            try
            {
                var ip = IPAddress.Parse(parameters[0]);
                var bytes = ip.GetAddressBytes();
                
                // Check for private IP ranges
                return (bytes[0] == 10) ||
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168) ||
                       (bytes[0] == 127) // localhost
                       ? "true" : "false";
            }
            catch
            {
                return "false";
            }
        }

        private static string ProcessIpIsPublic(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("IP isPublic requires 1 parameter: IP address");

            var isPrivate = ProcessIpIsPrivate(parameters);
            return isPrivate == "true" ? "false" : "true";
        }

        // JSON Functions
        private static string ProcessJsonFunction(string functionName, string[] parameters)
        {
            var jsonFunction = functionName.Substring(5); // Remove "json." prefix

            return jsonFunction.ToLower() switch
            {
                "get" => ProcessJsonGet(parameters),
                "length" => ProcessJsonLength(parameters),
                "first" => ProcessJsonFirst(parameters),
                "last" => ProcessJsonLast(parameters),
                "create" => ProcessJsonCreate(parameters),
                _ => throw new NotSupportedException($"JSON function '{jsonFunction}' is not supported")
            };
        }

        private static string ProcessJsonGet(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("JSON get requires 2 parameters: JSON string and path");

            try
            {
                var jsonDoc = JsonDocument.Parse(parameters[0]);
                var path = parameters[1];
                var result = GetJsonValue(jsonDoc.RootElement, path);
                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"JSON get error: {ex.Message}");
                return string.Empty;
            }
        }

        private static JsonElement? GetJsonValue(JsonElement element, string path)
        {
            var parts = path.Split('.');
            var current = element;

            foreach (var part in parts)
            {
                if (part.Contains('[') && part.Contains(']'))
                {
                    var propName = part.Substring(0, part.IndexOf('['));
                    var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);
                    
                    if (!string.IsNullOrEmpty(propName))
                    {
                        if (!current.TryGetProperty(propName, out current))
                            return null;
                    }

                    if (int.TryParse(indexStr, out var index) && current.ValueKind == JsonValueKind.Array)
                    {
                        var array = current.EnumerateArray().ToArray();
                        if (index >= 0 && index < array.Length)
                            current = array[index];
                        else
                            return null;
                    }
                    else
                        return null;
                }
                else
                {
                    if (!current.TryGetProperty(part, out current))
                        return null;
                }
            }

            return current;
        }

        private static string ProcessJsonLength(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("JSON length requires 2 parameters: JSON string and array path");

            try
            {
                var jsonDoc = JsonDocument.Parse(parameters[0]);
                var element = GetJsonValue(jsonDoc.RootElement, parameters[1]);
                
                if (element?.ValueKind == JsonValueKind.Array)
                    return element.Value.GetArrayLength().ToString();
                
                return "0";
            }
            catch
            {
                return "0";
            }
        }

        private static string ProcessJsonFirst(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("JSON first requires 2 parameters: JSON string and array path");

            try
            {
                var jsonDoc = JsonDocument.Parse(parameters[0]);
                var element = GetJsonValue(jsonDoc.RootElement, parameters[1]);
                
                if (element?.ValueKind == JsonValueKind.Array)
                {
                    var first = element.Value.EnumerateArray().FirstOrDefault();
                    return first.ToString();
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessJsonLast(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("JSON last requires 2 parameters: JSON string and array path");

            try
            {
                var jsonDoc = JsonDocument.Parse(parameters[0]);
                var element = GetJsonValue(jsonDoc.RootElement, parameters[1]);
                
                if (element?.ValueKind == JsonValueKind.Array)
                {
                    var last = element.Value.EnumerateArray().LastOrDefault();
                    return last.ToString();
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessJsonCreate(string[] parameters)
        {
            if (parameters.Length % 2 != 0)
                throw new ArgumentException("JSON create requires even number of parameters: key1, value1, key2, value2, ...");

            try
            {
                var obj = new Dictionary<string, object>();
                
                for (int i = 0; i < parameters.Length; i += 2)
                {
                    obj[parameters[i]] = parameters[i + 1];
                }

                return JsonSerializer.Serialize(obj);
            }
            catch
            {
                return "{}";
            }
        }

        // String Functions
        private static string ProcessStringFunction(string functionName, string[] parameters)
        {
            var stringFunction = functionName.Substring(7); // Remove "string." prefix

            return stringFunction.ToLower() switch
            {
                "upper" => ProcessStringUpper(parameters),
                "lower" => ProcessStringLower(parameters),
                "trim" => ProcessStringTrim(parameters),
                "replace" => ProcessStringReplace(parameters),
                "substring" => ProcessStringSubstring(parameters),
                "contains" => ProcessStringContains(parameters),
                "startswith" => ProcessStringStartsWith(parameters),
                "endswith" => ProcessStringEndsWith(parameters),
                "split" => ProcessStringSplit(parameters),
                "length" => ProcessStringLength(parameters),
                _ => throw new NotSupportedException($"String function '{stringFunction}' is not supported")
            };
        }

        private static string ProcessStringMethod(string input, string methodName, string[] parameters)
        {
            return methodName.ToLower() switch
            {
                "upper" => input.ToUpper(),
                "lower" => input.ToLower(),
                "trim" => input.Trim(),
                "replace" => parameters.Length == 2 ? input.Replace(parameters[0], parameters[1]) : input,
                "substring" => ProcessStringSubstringChained(input, parameters),
                "contains" => parameters.Length == 1 ? input.Contains(parameters[0]).ToString().ToLower() : "false",
                "startswith" => parameters.Length == 1 ? input.StartsWith(parameters[0]).ToString().ToLower() : "false",
                "endswith" => parameters.Length == 1 ? input.EndsWith(parameters[0]).ToString().ToLower() : "false",
                "split" => parameters.Length == 1 ? JsonSerializer.Serialize(input.Split(parameters[0], StringSplitOptions.RemoveEmptyEntries)) : input,
                "length" => input.Length.ToString(),
                _ => throw new NotSupportedException($"String method '{methodName}' is not supported")
            };
        }

        private static string ProcessStringUpper(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("String upper requires 1 parameter: text");
            
            return parameters[0].ToUpper();
        }

        private static string ProcessStringLower(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("String lower requires 1 parameter: text");
            
            return parameters[0].ToLower();
        }

        private static string ProcessStringTrim(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("String trim requires 1 parameter: text");
            
            return parameters[0].Trim();
        }

        private static string ProcessStringReplace(string[] parameters)
        {
            if (parameters.Length != 3)
                throw new ArgumentException("String replace requires 3 parameters: text, old, new");
            
            return parameters[0].Replace(parameters[1], parameters[2]);
        }

        private static string ProcessStringSubstring(string[] parameters)
        {
            if (parameters.Length < 2 || parameters.Length > 3)
                throw new ArgumentException("String substring requires 2-3 parameters: text, start, [length]");
            
            var text = parameters[0];
            if (!int.TryParse(parameters[1], out var start))
                throw new ArgumentException("Invalid start index");

            if (start < 0 || start >= text.Length)
                return string.Empty;

            if (parameters.Length == 3)
            {
                if (!int.TryParse(parameters[2], out var length))
                    throw new ArgumentException("Invalid length");
                
                if (start + length > text.Length)
                    length = text.Length - start;
                
                return text.Substring(start, length);
            }
            
            return text.Substring(start);
        }

        private static string ProcessStringSubstringChained(string input, string[] parameters)
        {
            if (parameters.Length < 1 || parameters.Length > 2)
                return input;
            
            if (!int.TryParse(parameters[0], out var start))
                return input;

            if (start < 0 || start >= input.Length)
                return string.Empty;

            if (parameters.Length == 2)
            {
                if (!int.TryParse(parameters[1], out var length))
                    return input;
                
                if (start + length > input.Length)
                    length = input.Length - start;
                
                return input.Substring(start, length);
            }
            
            return input.Substring(start);
        }

        private static string ProcessStringContains(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("String contains requires 2 parameters: text, searchText");
            
            return parameters[0].Contains(parameters[1]).ToString().ToLower();
        }

        private static string ProcessStringStartsWith(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("String startsWith requires 2 parameters: text, prefix");
            
            return parameters[0].StartsWith(parameters[1]).ToString().ToLower();
        }

        private static string ProcessStringEndsWith(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("String endsWith requires 2 parameters: text, suffix");
            
            return parameters[0].EndsWith(parameters[1]).ToString().ToLower();
        }

        private static string ProcessStringSplit(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("String split requires 2 parameters: text, separator");
            
            var parts = parameters[0].Split(parameters[1], StringSplitOptions.RemoveEmptyEntries);
            return JsonSerializer.Serialize(parts);
        }

        private static string ProcessStringLength(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("String length requires 1 parameter: text");
            
            return parameters[0].Length.ToString();
        }

        // Math Functions
        private static string ProcessMathFunction(string functionName, string[] parameters)
        {
            var mathFunction = functionName.Substring(5); // Remove "math." prefix

            return mathFunction.ToLower() switch
            {
                "add" => ProcessMathAdd(parameters),
                "subtract" => ProcessMathSubtract(parameters),
                "multiply" => ProcessMathMultiply(parameters),
                "divide" => ProcessMathDivide(parameters),
                "round" => ProcessMathRound(parameters),
                "floor" => ProcessMathFloor(parameters),
                "ceil" => ProcessMathCeil(parameters),
                "min" => ProcessMathMin(parameters),
                "max" => ProcessMathMax(parameters),
                "abs" => ProcessMathAbs(parameters),
                _ => throw new NotSupportedException($"Math function '{mathFunction}' is not supported")
            };
        }

        private static string ProcessMathAdd(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math add requires 2 parameters: number1, number2");
            
            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return (a + b).ToString();
            
            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathSubtract(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math subtract requires 2 parameters: number1, number2");
            
            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return (a - b).ToString();
            
            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathMultiply(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math multiply requires 2 parameters: number1, number2");
            
            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return (a * b).ToString();
            
            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathDivide(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math divide requires 2 parameters: number1, number2");
            
            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
            {
                if (b == 0)
                    throw new DivideByZeroException("Division by zero");
                return (a / b).ToString();
            }
            
            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathRound(string[] parameters)
        {
            if (parameters.Length < 1 || parameters.Length > 2)
                throw new ArgumentException("Math round requires 1-2 parameters: number, [digits]");
            
            if (!double.TryParse(parameters[0], out var number))
                throw new ArgumentException("Invalid numeric value");

            if (parameters.Length == 2)
            {
                if (!int.TryParse(parameters[1], out var digits))
                    throw new ArgumentException("Invalid digits value");
                return Math.Round(number, digits).ToString();
            }
            
            return Math.Round(number).ToString();
        }

        private static string ProcessMathFloor(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Math floor requires 1 parameter: number");
            
            if (double.TryParse(parameters[0], out var number))
                return Math.Floor(number).ToString();
            
            throw new ArgumentException("Invalid numeric value");
        }

        private static string ProcessMathCeil(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Math ceil requires 1 parameter: number");
            
            if (double.TryParse(parameters[0], out var number))
                return Math.Ceiling(number).ToString();
            
            throw new ArgumentException("Invalid numeric value");
        }

        private static string ProcessMathMin(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math min requires 2 parameters: number1, number2");
            
            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return Math.Min(a, b).ToString();
            
            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathMax(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Math max requires 2 parameters: number1, number2");
            
            if (double.TryParse(parameters[0], out var a) && double.TryParse(parameters[1], out var b))
                return Math.Max(a, b).ToString();
            
            throw new ArgumentException("Invalid numeric values");
        }

        private static string ProcessMathAbs(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Math abs requires 1 parameter: number");
            
            if (double.TryParse(parameters[0], out var number))
                return Math.Abs(number).ToString();
            
            throw new ArgumentException("Invalid numeric value");
        }

        // Array Functions
        private static string ProcessArrayFunction(string functionName, string[] parameters)
        {
            var arrayFunction = functionName.Substring(6); // Remove "array." prefix

            return arrayFunction.ToLower() switch
            {
                "get" => ProcessArrayGet(parameters),
                "length" => ProcessArrayLength(parameters),
                "join" => ProcessArrayJoin(parameters),
                _ => throw new NotSupportedException($"Array function '{arrayFunction}' is not supported")
            };
        }

        private static string ProcessArrayMethod(string arrayJson, string methodName, string[] parameters)
        {
            return methodName.ToLower() switch
            {
                "get" => ProcessArrayGetChained(arrayJson, parameters),
                "length" => ProcessArrayLengthChained(arrayJson),
                "join" => ProcessArrayJoinChained(arrayJson, parameters),
                _ => throw new NotSupportedException($"Array method '{methodName}' is not supported")
            };
        }

        private static string ProcessArrayGet(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Array get requires 2 parameters: array JSON and index");
            
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(parameters[0]);
                if (int.TryParse(parameters[1], out var index))
                {
                    // Support negative indexing (e.g., -1 for last element)
                    if (index < 0)
                        index = array.Length + index;
                    
                    if (index >= 0 && index < array.Length)
                        return array[index];
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessArrayGetChained(string arrayJson, string[] parameters)
        {
            if (parameters.Length != 1)
                return string.Empty;
            
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(arrayJson);
                if (int.TryParse(parameters[0], out var index))
                {
                    // Support negative indexing (e.g., -1 for last element)
                    if (index < 0)
                        index = array.Length + index;
                    
                    if (index >= 0 && index < array.Length)
                        return array[index];
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessArrayLength(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Array length requires 1 parameter: array JSON");
            
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(parameters[0]);
                return array.Length.ToString();
            }
            catch
            {
                return "0";
            }
        }

        private static string ProcessArrayLengthChained(string arrayJson)
        {
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(arrayJson);
                return array.Length.ToString();
            }
            catch
            {
                return "0";
            }
        }

        private static string ProcessArrayJoin(string[] parameters)
        {
            if (parameters.Length != 2)
                throw new ArgumentException("Array join requires 2 parameters: array JSON and separator");
            
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(parameters[0]);
                return string.Join(parameters[1], array);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ProcessArrayJoinChained(string arrayJson, string[] parameters)
        {
            if (parameters.Length != 1)
                return string.Empty;
            
            try
            {
                var array = JsonSerializer.Deserialize<string[]>(arrayJson);
                return string.Join(parameters[0], array);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolvePlaceholders(string input, Dictionary<string, string> context)
        {
            if (string.IsNullOrEmpty(input) || context == null)
                return input;

            return PlaceholderRegex.Replace(input, match =>
            {
                var key = match.Groups[1].Value;
                if (string.IsNullOrEmpty(key))
                    return match.Value;

                return context.TryGetValue(key, out var value) ? value : match.Value;
            });
        }
    }
}