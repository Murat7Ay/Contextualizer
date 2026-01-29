using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contextualizer.Core.Services.HandlerConfigOperations
{
    internal static class HandlerConfigFileIO
    {
        private static readonly JsonSerializerOptions HandlersJsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };

        public static IReadOnlyList<HandlerConfig> ReadAll(string handlersFilePath)
        {
            if (string.IsNullOrWhiteSpace(handlersFilePath) || !File.Exists(handlersFilePath))
                return Array.Empty<HandlerConfig>();

            var json = File.ReadAllText(handlersFilePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<HandlerConfig>();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("handlers", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return Array.Empty<HandlerConfig>();

            var list = new List<HandlerConfig>();
            foreach (var el in arr.EnumerateArray())
            {
                try
                {
                    var cfg = JsonSerializer.Deserialize<HandlerConfig>(el.GetRawText());
                    if (cfg != null)
                        list.Add(cfg);
                }
                catch
                {
                    // Ignore invalid entries; keep app resilient.
                }
            }

            return list;
        }

        public static void WriteAll(string handlersFilePath, List<HandlerConfig> configs)
        {
            var dir = Path.GetDirectoryName(handlersFilePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var payload = new { handlers = configs };
            var json = JsonSerializer.Serialize(payload, HandlersJsonOptions);

            AtomicWriteText(handlersFilePath, json);
        }

        private static void AtomicWriteText(string filePath, string contents)
        {
            var dir = Path.GetDirectoryName(filePath) ?? ".";
            var tmp = Path.Combine(dir, $"{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

            File.WriteAllText(tmp, contents, Encoding.UTF8);

            if (File.Exists(filePath))
            {
                var backup = Path.Combine(dir, $"{Path.GetFileName(filePath)}.bak");
                try
                {
                    File.Replace(tmp, filePath, backup, ignoreMetadataErrors: true);
                    try { File.Delete(backup); } catch { /* ignore */ }
                }
                catch
                {
                    // Fallback if File.Replace fails (e.g. permissions): best-effort replace.
                    File.Copy(tmp, filePath, overwrite: true);
                    try { File.Delete(tmp); } catch { /* ignore */ }
                }
            }
            else
            {
                File.Move(tmp, filePath);
            }
        }
    }
}
