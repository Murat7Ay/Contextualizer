using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services
{
    /// <summary>
    /// Single source of truth for reading/writing/updating handlers.json.
    /// Shared by UI (WebView2) and MCP management tools.
    /// </summary>
    public sealed class HandlerConfigStore
    {
        private readonly ISettingsService _settings;
        private readonly HandlerConfigValidator _validator;
        private static readonly SemaphoreSlim FileLock = new(1, 1);

        private static readonly JsonSerializerOptions HandlersJsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = null
        };
        
        // Used for merge/update flows where explicit null should be preserved (to allow clearing fields).
        private static readonly JsonSerializerOptions HandlersJsonOptionsIncludeNulls = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = null
        };

        public HandlerConfigStore(ISettingsService settings, HandlerConfigValidator? validator = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _validator = validator ?? new HandlerConfigValidator();
        }

        public async Task<IReadOnlyList<HandlerConfig>> ReadAllAsync(CancellationToken ct = default)
        {
            await FileLock.WaitAsync(ct);
            try
            {
                return ReadAllUnsafe();
            }
            finally
            {
                FileLock.Release();
            }
        }

        public async Task<HandlerConfig?> GetByNameAsync(string handlerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(handlerName)) return null;
            var all = await ReadAllAsync(ct);
            return all.FirstOrDefault(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<StoreResult> AddAsync(HandlerConfig config, CancellationToken ct = default)
        {
            await FileLock.WaitAsync(ct);
            try
            {
                var all = ReadAllUnsafe().ToList();
                var validation = _validator.ValidateForAdd(config, all);
                if (!validation.Ok)
                {
                    return StoreResult.Fail("VALIDATION_ERROR", string.Join(Environment.NewLine, validation.Errors));
                }

                all.Add(config);
                WriteAllUnsafe(all);
                return StoreResult.OkResult(new StorePayload
                {
                    HandlerName = config.Name,
                    HandlerType = config.Type,
                    UpdatedFields = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return StoreResult.Fail("FILE_WRITE_ERROR", ex.Message);
            }
            finally
            {
                FileLock.Release();
            }
        }

        public async Task<StoreResult> DeleteAsync(string handlerName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(handlerName))
                return StoreResult.Fail("INVALID_PARAMS", "handler_name is required.");

            await FileLock.WaitAsync(ct);
            try
            {
                var all = ReadAllUnsafe().ToList();
                var idx = all.FindIndex(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
                if (idx < 0)
                {
                    return StoreResult.Fail("NOT_FOUND", $"Handler not found: {handlerName}");
                }

                var removed = all[idx];
                all.RemoveAt(idx);
                WriteAllUnsafe(all);

                return StoreResult.OkResult(new StorePayload
                {
                    HandlerName = removed.Name,
                    HandlerType = removed.Type,
                    UpdatedFields = new List<string>()
                });
            }
            catch (Exception ex)
            {
                return StoreResult.Fail("FILE_WRITE_ERROR", ex.Message);
            }
            finally
            {
                FileLock.Release();
            }
        }

        /// <summary>
        /// Applies partial updates to an existing handler config. Updates must use HandlerConfig JSON property names.
        /// Name/type changes are ignored.
        /// </summary>
        public async Task<StoreResult> UpdatePartialAsync(string handlerName, JsonElement updates, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(handlerName))
                return StoreResult.Fail("INVALID_PARAMS", "handler_name is required.");

            if (updates.ValueKind != JsonValueKind.Object)
                return StoreResult.Fail("INVALID_PARAMS", "updates must be a JSON object.");

            await FileLock.WaitAsync(ct);
            try
            {
                var all = ReadAllUnsafe().ToList();
                var existing = all.FirstOrDefault(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    return StoreResult.Fail("NOT_FOUND", $"Handler not found: {handlerName}");

                var merged = Merge(existing, updates, out var updatedFields);
                var validation = _validator.ValidateForUpdate(merged, all, existing.Name);
                if (!validation.Ok)
                {
                    return StoreResult.Fail("VALIDATION_ERROR", string.Join(Environment.NewLine, validation.Errors));
                }

                // Replace the config by name match (case-insensitive)
                var idx = all.FindIndex(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
                all[idx] = merged;

                WriteAllUnsafe(all);

                return StoreResult.OkResult(new StorePayload
                {
                    HandlerName = merged.Name,
                    HandlerType = merged.Type,
                    UpdatedFields = updatedFields
                });
            }
            catch (Exception ex)
            {
                return StoreResult.Fail("FILE_WRITE_ERROR", ex.Message);
            }
            finally
            {
                FileLock.Release();
            }
        }

        private IReadOnlyList<HandlerConfig> ReadAllUnsafe()
        {
            var path = _settings.HandlersFilePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return Array.Empty<HandlerConfig>();

            var json = File.ReadAllText(path, Encoding.UTF8);
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

        private void WriteAllUnsafe(List<HandlerConfig> configs)
        {
            var path = _settings.HandlersFilePath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var payload = new { handlers = configs };
            var json = JsonSerializer.Serialize(payload, HandlersJsonOptions);

            AtomicWriteText(path, json);
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

        private static HandlerConfig Merge(HandlerConfig existing, JsonElement updates, out List<string> updatedFields)
        {
            updatedFields = new List<string>();

            var existingJson = JsonSerializer.Serialize(existing, HandlersJsonOptionsIncludeNulls);
            var baseNode = JsonNode.Parse(existingJson) as JsonObject ?? new JsonObject();
            var updatesNode = JsonNode.Parse(updates.GetRawText()) as JsonObject ?? new JsonObject();

            // Ignore identity fields
            updatesNode.Remove("name");
            updatesNode.Remove("type");

            foreach (var kvp in updatesNode)
            {
                // JsonNode cannot be attached to multiple parents; clone to avoid "node already has a parent".
                baseNode[kvp.Key] = kvp.Value?.DeepClone();
                updatedFields.Add(kvp.Key);
            }

            // Re-apply original identity fields to be safe
            baseNode["name"] = existing.Name;
            baseNode["type"] = existing.Type;

            var mergedJson = baseNode.ToJsonString(HandlersJsonOptionsIncludeNulls);
            var merged = JsonSerializer.Deserialize<HandlerConfig>(mergedJson, HandlersJsonOptionsIncludeNulls);
            if (merged == null)
                return existing;

            // Preserve identity even if deserializer produced empty values
            merged.Name = existing.Name;
            merged.Type = existing.Type;

            return merged;
        }

        public sealed class StorePayload
        {
            public string HandlerName { get; set; } = string.Empty;
            public string HandlerType { get; set; } = string.Empty;
            public List<string> UpdatedFields { get; set; } = new();
        }

        public sealed class StoreResult
        {
            public bool Success { get; set; }
            public string Code { get; set; } = string.Empty;
            public string? Error { get; set; }
            public StorePayload? Payload { get; set; }

            public static StoreResult OkResult(StorePayload payload) => new()
            {
                Success = true,
                Code = "OK",
                Payload = payload
            };

            public static StoreResult Fail(string code, string error) => new()
            {
                Success = false,
                Code = code,
                Error = error
            };
        }
    }
}


