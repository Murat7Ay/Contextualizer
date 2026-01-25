using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Contextualizer.Core.Handlers.Api.Models;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.Handlers.Api
{
    internal static class HttpResponseProcessor
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null
        };

        public static async Task<ResponseInfo> ReadResponseAsync(
            HttpResponseMessage response,
            NormalizedHttpConfig config,
            Dictionary<string, string> context,
            int pageIndex,
            bool isPaginated)
        {
            context["StatusCode"] = ((int)response.StatusCode).ToString();
            context["IsSuccessful"] = response.IsSuccessStatusCode.ToString();
            context["ReasonPhrase"] = response.ReasonPhrase ?? string.Empty;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            context["ResponseContentType"] = contentType;

            if (config.IncludeHeaders)
            {
                var prefix = config.HeaderPrefix ?? "Header.";
                foreach (var header in response.Headers)
                {
                    context[$"{prefix}{header.Key}"] = string.Join(", ", header.Value);
                }
                foreach (var header in response.Content.Headers)
                {
                    context[$"{prefix}{header.Key}"] = string.Join(", ", header.Value);
                }
            }

            var maxBytes = config.ResponseMaxBytes;
            var bytes = await ReadContentWithLimitAsync(response.Content, maxBytes);
            context["ResponseBytes"] = bytes.Length.ToString();

            var outputKey = config.RawBodyKey ?? "RawResponse";

            var expect = config.ResponseExpect;
            if (string.Equals(expect, "binary", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = Convert.ToBase64String(bytes);
                context["RawResponseBase64"] = base64;
                if (config.IncludeRawBody)
                {
                    context[outputKey] = base64;
                }
                return new ResponseInfo(base64, null);
            }

            var encoding = GetResponseEncoding(response) ?? Encoding.UTF8;
            var rawBody = encoding.GetString(bytes);
            if (config.IncludeRawBody)
            {
                context[outputKey] = rawBody;
            }

            var treatAsJson = string.Equals(expect, "json", StringComparison.OrdinalIgnoreCase) ||
                              (string.IsNullOrWhiteSpace(expect) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase));

            if (treatAsJson)
            {
                try
                {
                    var doc = JsonDocument.Parse(rawBody);
                    if (config.FlattenJson)
                    {
                        var prefix = config.FlattenPrefix;
                        if (isPaginated && !string.IsNullOrWhiteSpace(prefix))
                            FlattenJsonToContext(doc.RootElement, $"{prefix}{pageIndex}.", context);
                        else if (isPaginated)
                            FlattenJsonToContext(doc.RootElement, $"page_{pageIndex}.", context);
                        else
                            FlattenJsonToContext(doc.RootElement, "", context);
                    }
                    return new ResponseInfo(rawBody, doc);
                }
                catch (JsonException ex)
                {
                    context["JsonParseError"] = ex.Message;
                    UserFeedback.ShowError($"Failed to parse JSON response: {ex.Message}");
                }
            }

            return new ResponseInfo(rawBody, null);
        }

        public static void ApplyOutputMappings(
            JsonElement root,
            NormalizedHttpConfig config,
            Dictionary<string, string> context)
        {
            if (config.OutputMappings.Count > 0)
            {
                foreach (var kvp in config.OutputMappings)
                {
                    if (TryGetJsonPathValue(root, kvp.Value, out var value))
                        context[kvp.Key] = value;
                }
            }

            if (config.OutputHeaderMappings.Count > 0)
            {
                foreach (var kvp in config.OutputHeaderMappings)
                {
                    if (context.TryGetValue($"{config.HeaderPrefix ?? "Header."}{kvp.Value}", out var headerValue))
                        context[kvp.Key] = headerValue;
                }
            }
        }

        public static void FlattenJsonToContext(JsonElement element, string prefix, Dictionary<string, string> context)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        FlattenJsonToContext(property.Value, newPrefix, context);
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? index.ToString() : $"{prefix}[{index}]";
                        FlattenJsonToContext(item, newPrefix, context);
                        index++;
                    }
                    break;

                default:
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        context[prefix] = element.ToString();
                    }
                    break;
            }
        }

        private static async Task<byte[]> ReadContentWithLimitAsync(HttpContent content, int maxBytes)
        {
            var limit = maxBytes <= 0 ? 5 * 1024 * 1024 : maxBytes;
            using var stream = await content.ReadAsStreamAsync();
            using var ms = new MemoryStream();
            var buffer = new byte[16 * 1024];
            int read;
            int total = 0;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                total += read;
                if (total > limit)
                    throw new InvalidOperationException($"Response exceeds max_bytes limit ({limit} bytes)");
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }

        private static Encoding? GetResponseEncoding(HttpResponseMessage response)
        {
            var charset = response.Content.Headers.ContentType?.CharSet;
            if (string.IsNullOrWhiteSpace(charset))
                return null;
            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch
            {
                return null;
            }
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
