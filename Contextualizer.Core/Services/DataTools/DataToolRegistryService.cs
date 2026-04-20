using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contextualizer.Core.Services.DataTools
{
    public sealed class DataToolRegistryMutationResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public DataToolDefinition? Definition { get; init; }
    }

    public sealed class DataToolRegistryService
    {
        private static readonly JsonSerializerOptions RegistrySerializerOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string _registryPath;
        private readonly ILoggingService? _loggingService;
        private readonly object _syncRoot = new();
        private List<DataToolDefinition> _definitions = new();
        private DateTime _lastWriteUtc = DateTime.MinValue;

        public DataToolRegistryService(string registryPath, ILoggingService? loggingService = null)
        {
            _registryPath = registryPath ?? throw new ArgumentNullException(nameof(registryPath));
            _loggingService = loggingService;

            EnsureRegistryFileExists();
            Reload();
        }

        public string RegistryPath => _registryPath;

        public IReadOnlyList<DataToolDefinition> GetAllDefinitions()
        {
            EnsureFresh();
            lock (_syncRoot)
            {
                return _definitions.ToList();
            }
        }

        public IReadOnlyList<DataToolDefinition> GetEnabledDefinitions()
        {
            EnsureFresh();
            lock (_syncRoot)
            {
                return _definitions.Where(IsEnabledDefinition).ToList();
            }
        }

        public IReadOnlyList<DataToolDefinition> GetSupportedExposedDefinitions()
        {
            EnsureFresh();
            lock (_syncRoot)
            {
                return _definitions
                    .Where(IsEnabledDefinition)
                    .Where(d => d.ExposeAsTool)
                    .Where(DataToolExecutionService.IsDefinitionSupported)
                    .OrderBy(ResolveToolName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        public DataToolDefinition? TryGetById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            EnsureFresh();
            lock (_syncRoot)
            {
                return _definitions.FirstOrDefault(d =>
                    IsEnabledDefinition(d) &&
                    string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
            }
        }

        public DataToolDefinition? TryGetAnyById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            EnsureFresh();
            lock (_syncRoot)
            {
                return _definitions.FirstOrDefault(d =>
                    string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
            }
        }

        public DataToolDefinition? TryGetByToolName(string? toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
                return null;

            EnsureFresh();
            lock (_syncRoot)
            {
                return _definitions.FirstOrDefault(d =>
                    IsEnabledDefinition(d) &&
                    d.ExposeAsTool &&
                    DataToolExecutionService.IsDefinitionSupported(d) &&
                    string.Equals(ResolveToolName(d), toolName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Reload()
        {
            lock (_syncRoot)
            {
                try
                {
                    EnsureRegistryFileExists();

                    var json = File.ReadAllText(_registryPath, System.Text.Encoding.UTF8);
                    var document = JsonSerializer.Deserialize<DataToolRegistryDocument>(json) ?? new DataToolRegistryDocument();

                    _definitions = document.Definitions
                        .Where(IsValidDefinition)
                        .ToList();

                    _lastWriteUtc = File.GetLastWriteTimeUtc(_registryPath);
                }
                catch (Exception ex)
                {
                    _definitions = new List<DataToolDefinition>();
                    _loggingService?.LogError("Failed to load data tools registry", ex, new Dictionary<string, object>
                    {
                        ["registry_path"] = _registryPath
                    });
                }
            }
        }

        public DataToolRegistryMutationResult Create(DataToolDefinition definition)
        {
            if (definition == null)
                return new DataToolRegistryMutationResult { Success = false, Error = "Definition is required." };

            lock (_syncRoot)
            {
                try
                {
                    EnsureRegistryFileExists();
                    var normalized = NormalizeDefinition(definition);
                    if (!IsValidDefinition(normalized))
                        return new DataToolRegistryMutationResult { Success = false, Error = "Definition is not valid." };

                    if (_definitions.Any(d => string.Equals(d.Id, normalized.Id, StringComparison.OrdinalIgnoreCase)))
                        return new DataToolRegistryMutationResult { Success = false, Error = $"A definition with id '{normalized.Id}' already exists." };

                    _definitions.Add(normalized);
                    SaveLocked();
                    return new DataToolRegistryMutationResult { Success = true, Definition = normalized };
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError("Failed to create data tool definition", ex, new Dictionary<string, object>
                    {
                        ["registry_path"] = _registryPath,
                        ["definition_id"] = definition.Id ?? string.Empty
                    });
                    return new DataToolRegistryMutationResult { Success = false, Error = ex.Message };
                }
            }
        }

        public DataToolRegistryMutationResult Update(string originalId, DataToolDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(originalId))
                return new DataToolRegistryMutationResult { Success = false, Error = "Original id is required." };

            if (definition == null)
                return new DataToolRegistryMutationResult { Success = false, Error = "Definition is required." };

            lock (_syncRoot)
            {
                try
                {
                    EnsureRegistryFileExists();
                    var index = _definitions.FindIndex(d => string.Equals(d.Id, originalId, StringComparison.OrdinalIgnoreCase));
                    if (index < 0)
                        return new DataToolRegistryMutationResult { Success = false, Error = $"Definition '{originalId}' was not found." };

                    var normalized = NormalizeDefinition(definition);
                    if (!IsValidDefinition(normalized))
                        return new DataToolRegistryMutationResult { Success = false, Error = "Definition is not valid." };

                    var duplicate = _definitions.Any(d =>
                        !string.Equals(d.Id, originalId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(d.Id, normalized.Id, StringComparison.OrdinalIgnoreCase));
                    if (duplicate)
                        return new DataToolRegistryMutationResult { Success = false, Error = $"A definition with id '{normalized.Id}' already exists." };

                    _definitions[index] = normalized;
                    SaveLocked();
                    return new DataToolRegistryMutationResult { Success = true, Definition = normalized };
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError("Failed to update data tool definition", ex, new Dictionary<string, object>
                    {
                        ["registry_path"] = _registryPath,
                        ["definition_id"] = originalId
                    });
                    return new DataToolRegistryMutationResult { Success = false, Error = ex.Message };
                }
            }
        }

        public DataToolRegistryMutationResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new DataToolRegistryMutationResult { Success = false, Error = "Definition id is required." };

            lock (_syncRoot)
            {
                try
                {
                    EnsureRegistryFileExists();
                    var removed = _definitions.RemoveAll(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
                    if (removed == 0)
                        return new DataToolRegistryMutationResult { Success = false, Error = $"Definition '{id}' was not found." };

                    SaveLocked();
                    return new DataToolRegistryMutationResult { Success = true };
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError("Failed to delete data tool definition", ex, new Dictionary<string, object>
                    {
                        ["registry_path"] = _registryPath,
                        ["definition_id"] = id
                    });
                    return new DataToolRegistryMutationResult { Success = false, Error = ex.Message };
                }
            }
        }

        public static string ResolveToolName(DataToolDefinition definition)
        {
            return Slugify(!string.IsNullOrWhiteSpace(definition.ToolName)
                ? definition.ToolName
                : !string.IsNullOrWhiteSpace(definition.Name)
                    ? definition.Name
                    : definition.Id);
        }

        private void EnsureFresh()
        {
            try
            {
                if (!File.Exists(_registryPath))
                {
                    Reload();
                    return;
                }

                var currentWriteUtc = File.GetLastWriteTimeUtc(_registryPath);
                if (currentWriteUtc > _lastWriteUtc)
                {
                    Reload();
                }
            }
            catch
            {
            }
        }

        private void EnsureRegistryFileExists()
        {
            var directory = Path.GetDirectoryName(_registryPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(_registryPath))
                return;

            File.WriteAllText(_registryPath, CreateDefaultRegistryJson(), System.Text.Encoding.UTF8);
        }

        private void SaveLocked()
        {
            var document = new DataToolRegistryDocument
            {
                Definitions = _definitions.ToList()
            };

            var json = JsonSerializer.Serialize(document, RegistrySerializerOptions);
            File.WriteAllText(_registryPath, json, System.Text.Encoding.UTF8);
            _lastWriteUtc = File.GetLastWriteTimeUtc(_registryPath);
        }

        private static bool IsEnabledDefinition(DataToolDefinition definition)
        {
            return definition.Enabled && IsValidDefinition(definition);
        }

        private static bool IsValidDefinition(DataToolDefinition definition)
        {
            if (definition == null)
                return false;

            if (string.IsNullOrWhiteSpace(definition.Id) ||
                string.IsNullOrWhiteSpace(definition.Provider) ||
                !DataToolOperationKinds.IsValid(definition.Operation) ||
                string.IsNullOrWhiteSpace(definition.Connection))
            {
                return false;
            }

            return definition.Operation.ToLowerInvariant() switch
            {
                DataToolOperationKinds.Procedure => !string.IsNullOrWhiteSpace(definition.ProcedureName),
                _ => !string.IsNullOrWhiteSpace(definition.Statement)
            };
        }

        private static DataToolDefinition NormalizeDefinition(DataToolDefinition definition)
        {
            return new DataToolDefinition
            {
                Id = (definition.Id ?? string.Empty).Trim(),
                Name = TrimToNull(definition.Name),
                ToolName = TrimToNull(definition.ToolName),
                Description = TrimToNull(definition.Description),
                Provider = (definition.Provider ?? string.Empty).Trim(),
                Operation = (definition.Operation ?? string.Empty).Trim().ToLowerInvariant(),
                Connection = (definition.Connection ?? string.Empty).Trim(),
                Statement = TrimToNull(definition.Statement),
                ProcedureName = TrimToNull(definition.ProcedureName),
                Enabled = definition.Enabled,
                ExposeAsTool = definition.ExposeAsTool,
                CommandTimeoutSeconds = definition.CommandTimeoutSeconds,
                ConnectionTimeoutSeconds = definition.ConnectionTimeoutSeconds,
                MaxPoolSize = definition.MaxPoolSize,
                MinPoolSize = definition.MinPoolSize,
                DisablePooling = definition.DisablePooling,
                Parameters = (definition.Parameters ?? new List<DataToolParameterDefinition>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .Select(NormalizeParameter)
                    .ToList(),
                InputSchema = definition.InputSchema,
                Tags = (definition.Tags ?? new List<string>())
                    .Select(t => (t ?? string.Empty).Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Result = NormalizeResult(definition.Result),
                ProviderOptions = definition.ProviderOptions
            };
        }

        private static DataToolParameterDefinition NormalizeParameter(DataToolParameterDefinition parameter)
        {
            return new DataToolParameterDefinition
            {
                Name = (parameter.Name ?? string.Empty).Trim(),
                DbParameterName = TrimToNull(parameter.DbParameterName),
                Type = string.IsNullOrWhiteSpace(parameter.Type) ? "string" : parameter.Type.Trim(),
                Description = TrimToNull(parameter.Description),
                Required = parameter.Required,
                DefaultValue = parameter.DefaultValue,
                Enum = parameter.Enum?.Select(v => (v ?? string.Empty).Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                ArrayItemType = TrimToNull(parameter.ArrayItemType),
                Direction = string.IsNullOrWhiteSpace(parameter.Direction) ? DataToolParameterDirections.Input : parameter.Direction.Trim().ToLowerInvariant(),
                DbType = TrimToNull(parameter.DbType),
                SerializeAsJson = parameter.SerializeAsJson
            };
        }

        private static DataToolResultOptions NormalizeResult(DataToolResultOptions? result)
        {
            result ??= new DataToolResultOptions();
            return new DataToolResultOptions
            {
                Mode = TrimToNull(result.Mode),
                MaxRows = result.MaxRows <= 0 ? 200 : result.MaxRows,
                IncludeExecutionMetadata = result.IncludeExecutionMetadata,
                IncludeOutputParameters = result.IncludeOutputParameters,
                OutputScalarParameter = TrimToNull(result.OutputScalarParameter)
            };
        }

        private static string? TrimToNull(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private static string Slugify(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "tool";

            var chars = new List<char>(value.Length);
            foreach (var ch in value.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    chars.Add(char.ToLowerInvariant(ch));
                    continue;
                }

                if (ch == ' ' || ch == '-' || ch == '_' || ch == '.')
                {
                    if (chars.Count == 0 || chars[^1] == '_')
                        continue;

                    chars.Add('_');
                }
            }

            var result = new string(chars.ToArray()).Trim('_');
            return string.IsNullOrWhiteSpace(result) ? "tool" : result;
        }

        private static string CreateDefaultRegistryJson()
        {
            return """
{
  "definitions": [
    {
      "id": "sample_customer_by_code_mssql",
      "name": "Sample Customer By Code (MSSQL)",
      "tool_name": "get_customer_by_code",
      "description": "Example read-only statement for SQL Server. Enable and adjust table/connection before use.",
      "provider": "mssql",
      "operation": "select",
      "connection": "$config:connections.main_mssql",
      "statement": "SELECT TOP 10 customer_code, customer_name FROM dbo.customers WHERE customer_code = @institution_code",
      "enabled": false,
      "expose_as_tool": true,
      "parameters": [
        {
          "name": "institution_code",
          "db_parameter_name": "institution_code",
          "type": "string",
          "description": "Institution code to search for",
          "required": true
        }
      ],
      "tags": ["sample", "mssql", "customer"],
      "result": {
        "max_rows": 50,
        "include_execution_metadata": true
      }
    },
    {
      "id": "sample_customer_balance_oracle",
      "name": "Sample Customer Balance (Oracle)",
      "tool_name": "get_customer_balance",
      "description": "Example scalar statement for Oracle. Enable and adjust table/connection before use.",
      "provider": "plsql",
      "operation": "scalar",
      "connection": "$config:connections.main_oracle",
      "statement": "SELECT NVL(SUM(balance), 0) FROM customer_balance WHERE customer_code = :institution_code",
      "enabled": false,
      "expose_as_tool": true,
      "parameters": [
        {
          "name": "institution_code",
          "db_parameter_name": "institution_code",
          "type": "string",
          "description": "Institution code to calculate balance for",
          "required": true
        }
      ],
      "tags": ["sample", "oracle", "scalar"]
    }
  ]
}
""";
        }
    }
}