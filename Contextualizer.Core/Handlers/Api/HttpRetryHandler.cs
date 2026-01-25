using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Contextualizer.Core.Handlers.Api.Models;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpRetryHandler
    {
        public static async Task<HttpResponseMessage> SendWithRetryAsync(
            Func<Task<HttpResponseMessage>> send,
            NormalizedHttpConfig config)
        {
            var retry = config.RetryEnabled;
            var maxAttempts = Math.Max(1, config.RetryMaxAttempts);
            var attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var response = await send();
                    if (!retry || attempt >= maxAttempts)
                        return response;

                    var status = (int)response.StatusCode;
                    if (!config.RetryOnStatus.Contains(status))
                        return response;

                    await DelayForRetryAsync(attempt, config);
                    continue;
                }
                catch (Exception ex) when (retry && attempt < maxAttempts && ShouldRetryException(ex, config))
                {
                    await DelayForRetryAsync(attempt, config);
                }
            }
        }

        private static async Task DelayForRetryAsync(int attempt, NormalizedHttpConfig config)
        {
            var baseDelay = Math.Max(50, config.RetryBaseDelayMs);
            var maxDelay = Math.Max(baseDelay, config.RetryMaxDelayMs);
            var exp = Math.Min(maxDelay, baseDelay * (int)Math.Pow(2, attempt - 1));
            var delay = config.RetryJitter ? exp + Random.Shared.Next(0, baseDelay) : exp;
            await Task.Delay(delay);
        }

        private static bool ShouldRetryException(Exception ex, NormalizedHttpConfig config)
        {
            if (config.RetryOnExceptions.Count == 0) return true;
            var typeName = ex.GetType().Name;
            return config.RetryOnExceptions.Any(t => string.Equals(t, typeName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
