using Contextualizer.PluginContracts;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services.Logging
{
    internal class LogUsageTracker : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _userId;
        private readonly string _sessionId;
        private readonly string _version;
        private LoggingConfiguration _config;

        public LogUsageTracker(LoggingConfiguration config, string userId, string sessionId, string version)
        {
            _httpClient = new HttpClient();
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _userId = userId ?? throw new ArgumentNullException(nameof(userId));
            _sessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            _version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public void UpdateConfiguration(LoggingConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task LogUsageAsync(UsageEvent usageEvent, Action<string> debugLogger)
        {
            if (!_config.EnableUsageTracking || string.IsNullOrEmpty(_config.UsageEndpointUrl))
                return;

            try
            {
                usageEvent.UserId = _userId;
                usageEvent.SessionId = _sessionId;
                usageEvent.Version = _version;

                var json = JsonSerializer.Serialize(usageEvent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                using var response = await _httpClient.PostAsync(_config.UsageEndpointUrl, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    debugLogger?.Invoke($"Failed to send usage data: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                debugLogger?.Invoke($"Error sending usage data: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
