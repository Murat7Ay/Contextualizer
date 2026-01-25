using System;
using System.Net;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class UrlFunctionExecutor
    {
        public static string ProcessUrlFunction(string functionName, string[] parameters)
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
    }
}
