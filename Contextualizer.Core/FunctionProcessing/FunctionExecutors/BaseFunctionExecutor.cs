using System;
using System.Collections.Generic;
using Contextualizer.Core.FunctionProcessing.FunctionExecutors;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class BaseFunctionExecutor
    {
        private static readonly Random Random = new();

        public static object ProcessBaseFunction(string functionName, string[] parameters)
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
                _ when functionName.StartsWith("hash.") => HashFunctionExecutor.ProcessHashFunction(functionName, parameters),
                _ when functionName.StartsWith("url.") => UrlFunctionExecutor.ProcessUrlFunction(functionName, parameters),
                _ when functionName.StartsWith("web.") => WebFunctionExecutor.ProcessWebFunction(functionName, parameters),
                _ when functionName.StartsWith("ip.") => IpFunctionExecutor.ProcessIpFunction(functionName, parameters),
                _ when functionName.StartsWith("json.") => JsonFunctionExecutor.ProcessJsonFunction(functionName, parameters),
                _ when functionName.StartsWith("string.") => StringFunctionExecutor.ProcessStringFunction(functionName, parameters),
                _ when functionName.StartsWith("math.") => MathFunctionExecutor.ProcessMathFunction(functionName, parameters),
                _ when functionName.StartsWith("array.") => ArrayFunctionExecutor.ProcessArrayFunction(functionName, parameters),
                _ => throw new NotSupportedException($"Function '{functionName}' is not supported")
            };
        }

        public static object ProcessChainedMethod(object input, string methodName, string[] parameters)
        {
            string actualMethodName = methodName;

            if (methodName.StartsWith("string."))
            {
                actualMethodName = methodName.Substring(7);
                return StringFunctionExecutor.ProcessStringMethod(input.ToString() ?? string.Empty, actualMethodName, parameters);
            }
            else if (methodName.StartsWith("array."))
            {
                actualMethodName = methodName.Substring(6);
                return ArrayFunctionExecutor.ProcessArrayMethod(input.ToString() ?? string.Empty, actualMethodName, parameters);
            }
            else if (methodName.StartsWith("math."))
            {
                actualMethodName = methodName.Substring(5);
                if (double.TryParse(input.ToString(), out var number))
                {
                    var newParams = new string[parameters.Length + 1];
                    newParams[0] = input.ToString() ?? string.Empty;
                    Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                    return MathFunctionExecutor.ProcessMathFunction($"math.{actualMethodName}", newParams);
                }
                return input.ToString() ?? string.Empty;
            }
            else if (methodName.StartsWith("url."))
            {
                actualMethodName = methodName.Substring(4);
                var newParams = new string[parameters.Length + 1];
                newParams[0] = input.ToString() ?? string.Empty;
                Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                return UrlFunctionExecutor.ProcessUrlFunction($"url.{actualMethodName}", newParams);
            }
            else if (methodName.StartsWith("hash."))
            {
                actualMethodName = methodName.Substring(5);
                var newParams = new string[parameters.Length + 1];
                newParams[0] = input.ToString() ?? string.Empty;
                Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                return HashFunctionExecutor.ProcessHashFunction($"hash.{actualMethodName}", newParams);
            }
            else if (methodName.StartsWith("json."))
            {
                actualMethodName = methodName.Substring(5);
                var newParams = new string[parameters.Length + 1];
                newParams[0] = input.ToString() ?? string.Empty;
                Array.Copy(parameters, 0, newParams, 1, parameters.Length);
                return JsonFunctionExecutor.ProcessJsonFunction($"json.{actualMethodName}", newParams);
            }
            else if (methodName == "base64encode")
            {
                return ProcessBase64Encode(new[] { input.ToString() ?? string.Empty });
            }
            else if (methodName == "base64decode")
            {
                return ProcessBase64Decode(new[] { input.ToString() ?? string.Empty });
            }

            return input switch
            {
                DateTime dateTime => DateTimeFunctionExecutor.ProcessDateTimeMethod(dateTime, actualMethodName, parameters),
                string str when str.StartsWith("[") && str.EndsWith("]") => ArrayFunctionExecutor.ProcessArrayMethod(str, actualMethodName, parameters),
                string str => StringFunctionExecutor.ProcessStringMethod(str, actualMethodName, parameters),
                _ => throw new NotSupportedException($"Method '{methodName}' is not supported for type '{input?.GetType().Name}'")
            };
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

            var bytes = System.Text.Encoding.UTF8.GetBytes(parameters[0]);
            return Convert.ToBase64String(bytes);
        }

        private static string ProcessBase64Decode(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Base64Decode requires 1 parameter: base64 text to decode");

            var bytes = Convert.FromBase64String(parameters[0]);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        private static string ProcessEnvironmentVariable(string[] parameters)
        {
            if (parameters.Length != 1)
                throw new ArgumentException("Environment variable function requires 1 parameter: variable name");

            return Environment.GetEnvironmentVariable(parameters[0]) ?? string.Empty;
        }
    }
}
