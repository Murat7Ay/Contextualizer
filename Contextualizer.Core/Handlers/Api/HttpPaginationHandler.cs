using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core.Handlers.Api.Models;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpPaginationHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null
        };

        public static async Task ExecuteHttpFlowAsync(
            HttpClient httpClient,
            NormalizedHttpConfig config,
            Dictionary<string, string> context,
            Func<Dictionary<string, string>, Dictionary<string, string>?, HttpRequestMessage> buildRequest,
            Func<HttpRequestMessage, Task<HttpResponseMessage>> sendWithRetry)
        {
            var paginationType = config.PaginationType;
            var maxPages = paginationType == null ? 1 : Math.Max(1, config.PaginationMaxPages ?? 1);
            var rawResponses = new List<string>();
            JsonDocument? lastJson = null;
            int pageIndex = 0;
            string? cursor = null;
            int? offset = config.PaginationStartOffset;
            int? page = config.PaginationStartPage;

            while (pageIndex < maxPages)
            {
                var extraQuery = BuildPaginationQuery(paginationType, cursor, offset, page, config);
                using var request = buildRequest(context, extraQuery);
                using var response = await sendWithRetry(request);

                var responseInfo = await HttpResponseProcessor.ReadResponseAsync(response, config, context, pageIndex, paginationType != null);
                rawResponses.Add(responseInfo.RawBody ?? string.Empty);
                lastJson?.Dispose();
                lastJson = responseInfo.JsonDocument;

                pageIndex++;

                if (paginationType == null || !response.IsSuccessStatusCode)
                {
                    break;
                }

                if (paginationType.Equals("cursor", StringComparison.OrdinalIgnoreCase))
                {
                    cursor = TryExtractCursor(responseInfo.JsonDocument, config.PaginationCursorPath);
                    if (string.IsNullOrWhiteSpace(cursor))
                        break;
                }
                else if (paginationType.Equals("offset", StringComparison.OrdinalIgnoreCase))
                {
                    if (!config.PaginationPageSize.HasValue)
                        break;
                    offset = (offset ?? 0) + config.PaginationPageSize.Value;
                }
                else if (paginationType.Equals("page", StringComparison.OrdinalIgnoreCase))
                {
                    page = (page ?? 1) + 1;
                }
                else
                {
                    break;
                }
            }

            if (rawResponses.Count > 1)
            {
                var json = JsonSerializer.Serialize(rawResponses, JsonOptions);
                context["RawResponses"] = json;
                context["PageCount"] = rawResponses.Count.ToString();
            }

            if (lastJson != null)
            {
                HttpResponseProcessor.ApplyOutputMappings(lastJson.RootElement, config, context);
            }
        }

        private static Dictionary<string, string>? BuildPaginationQuery(
            string? paginationType,
            string? cursor,
            int? offset,
            int? page,
            NormalizedHttpConfig config)
        {
            if (paginationType == null)
                return null;

            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (paginationType.Equals("cursor", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(config.PaginationNextParam) && !string.IsNullOrWhiteSpace(cursor))
                    query[config.PaginationNextParam] = cursor;
                if (!string.IsNullOrWhiteSpace(config.PaginationLimitParam) && config.PaginationPageSize.HasValue)
                    query[config.PaginationLimitParam] = config.PaginationPageSize.Value.ToString();
            }
            else if (paginationType.Equals("offset", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(config.PaginationOffsetParam) && offset.HasValue)
                    query[config.PaginationOffsetParam] = offset.Value.ToString();
                if (!string.IsNullOrWhiteSpace(config.PaginationLimitParam) && config.PaginationPageSize.HasValue)
                    query[config.PaginationLimitParam] = config.PaginationPageSize.Value.ToString();
            }
            else if (paginationType.Equals("page", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(config.PaginationPageParam) && page.HasValue)
                    query[config.PaginationPageParam] = page.Value.ToString();
                if (!string.IsNullOrWhiteSpace(config.PaginationLimitParam) && config.PaginationPageSize.HasValue)
                    query[config.PaginationLimitParam] = config.PaginationPageSize.Value.ToString();
            }

            return query.Count > 0 ? query : null;
        }

        private static string? TryExtractCursor(JsonDocument? doc, string? cursorPath)
        {
            if (doc == null || string.IsNullOrWhiteSpace(cursorPath))
                return null;

            if (TryGetJsonPathValue(doc.RootElement, cursorPath, out var value))
                return value;

            return null;
        }

        private static bool TryGetJsonPathValue(JsonElement element, string path, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return false;

            var current = element;
            foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var segment = part;
                int? index = null;
                var idxStart = part.IndexOf('[');
                if (idxStart >= 0 && part.EndsWith("]"))
                {
                    segment = part[..idxStart];
                    var idxText = part[(idxStart + 1)..^1];
                    if (int.TryParse(idxText, out var idx))
                        index = idx;
                }

                if (!string.IsNullOrWhiteSpace(segment))
                {
                    if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                        return false;
                }

                if (index.HasValue)
                {
                    if (current.ValueKind != JsonValueKind.Array || current.GetArrayLength() <= index.Value)
                        return false;
                    current = current[index.Value];
                }
            }

            value = current.ToString();
            return true;
        }
    }
}
