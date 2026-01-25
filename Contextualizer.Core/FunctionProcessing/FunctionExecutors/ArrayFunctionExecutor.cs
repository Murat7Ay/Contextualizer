using System;
using System.Text.Json;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class ArrayFunctionExecutor
    {
        public static string ProcessArrayFunction(string functionName, string[] parameters)
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

        public static string ProcessArrayMethod(string arrayJson, string methodName, string[] parameters)
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
    }
}
