using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core
{
    public static class FunctionProcessor
    {
        private static readonly Regex FunctionRegex = new(@"\$func:([^$\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Random Random = new();

        public static string ProcessFunctions(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                return FunctionRegex.Replace(input, match =>
                {
                    var functionCall = match.Groups[1].Value;
                    
                    return ProcessSingleFunction(functionCall);
                });
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Error processing functions: {ex.Message}");
                return input;
            }
        }

        private static string ProcessSingleFunction(string functionCall)
        {
            try
            {
                // Parse chained function calls using better parsing
                var parts = ParseChainedCall(functionCall);
                object result = null;

                for (int i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];

                    if (i == 0)
                    {
                        // Base function
                        result = ProcessBaseFunction(part.Name, part.Parameters);
                    }
                    else
                    {
                        // Chained method
                        result = ProcessChainedMethod(result, part.Name, part.Parameters);
                    }
                }

                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Error processing function '{functionCall}': {ex.Message}");
                return $"$func:{functionCall}";
            }
        }

        private static List<(string Name, string[] Parameters)> ParseChainedCall(string functionCall)
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
                        parameters = paramString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim()).ToArray();
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
                _ => throw new NotSupportedException($"Function '{functionName}' is not supported")
            };
        }

        private static object ProcessChainedMethod(object input, string methodName, string[] parameters)
        {
            return input switch
            {
                DateTime dateTime => ProcessDateTimeMethod(dateTime, methodName, parameters),
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
    }
}