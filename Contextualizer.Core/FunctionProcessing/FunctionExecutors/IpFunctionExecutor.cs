using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class IpFunctionExecutor
    {
        public static string ProcessIpFunction(string functionName, string[] parameters)
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

        private static string ProcessIpLocal()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
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

                return (bytes[0] == 10) ||
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168) ||
                       (bytes[0] == 127)
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
    }
}
