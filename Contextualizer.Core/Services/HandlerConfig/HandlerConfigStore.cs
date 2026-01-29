using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Contextualizer.Core.Services.HandlerConfigOperations;

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
                return HandlerConfigFileIO.ReadAll(_settings.HandlersFilePath);
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
                var all = HandlerConfigFileIO.ReadAll(_settings.HandlersFilePath).ToList();
                var validation = _validator.ValidateForAdd(config, all);
                if (!validation.Ok)
                {
                    return StoreResult.Fail("VALIDATION_ERROR", string.Join(Environment.NewLine, validation.Errors));
                }

                all.Add(config);
                HandlerConfigFileIO.WriteAll(_settings.HandlersFilePath, all);
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
                var all = HandlerConfigFileIO.ReadAll(_settings.HandlersFilePath).ToList();
                var idx = all.FindIndex(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
                if (idx < 0)
                {
                    return StoreResult.Fail("NOT_FOUND", $"Handler not found: {handlerName}");
                }

                var removed = all[idx];
                all.RemoveAt(idx);
                HandlerConfigFileIO.WriteAll(_settings.HandlersFilePath, all);

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
                var all = HandlerConfigFileIO.ReadAll(_settings.HandlersFilePath).ToList();
                var existing = all.FirstOrDefault(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                    return StoreResult.Fail("NOT_FOUND", $"Handler not found: {handlerName}");

                var merged = HandlerConfigMerger.Merge(existing, updates, out var updatedFields);
                var validation = _validator.ValidateForUpdate(merged, all, existing.Name);
                if (!validation.Ok)
                {
                    return StoreResult.Fail("VALIDATION_ERROR", string.Join(Environment.NewLine, validation.Errors));
                }

                // Replace the config by name match (case-insensitive)
                var idx = all.FindIndex(h => h.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
                all[idx] = merged;

                HandlerConfigFileIO.WriteAll(_settings.HandlersFilePath, all);

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
