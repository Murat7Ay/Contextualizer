using System;
using System.Text.Json;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class StringFunctionExecutor
    {
        public static string ProcessStringFunction(string functionName, string[] parameters)
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

        public static string ProcessStringMethod(string input, string methodName, string[] parameters)
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
    }
}
