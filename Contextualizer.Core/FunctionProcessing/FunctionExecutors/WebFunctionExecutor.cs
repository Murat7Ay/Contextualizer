using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.FunctionProcessing.FunctionExecutors
{
    internal static class WebFunctionExecutor
    {
        public static string ProcessWebFunction(string functionName, string[] parameters)
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
    }
}
