using System;
using System.Linq;
using System.Text.Json;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class JsonFunctionExecutor
    {
        public static string ProcessJsonFunction(string functionName, string[] parameters)
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
                var obj = new System.Collections.Generic.Dictionary<string, object>();

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
    }
}
