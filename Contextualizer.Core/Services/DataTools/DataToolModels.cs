using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contextualizer.Core.Services.DataTools
{
    public static class DataToolOperationKinds
    {
        public const string Select = "select";
        public const string Scalar = "scalar";
        public const string Execute = "execute";
        public const string Procedure = "procedure";

        public static bool IsValid(string? value)
        {
            return value?.ToLowerInvariant() switch
            {
                Select or Scalar or Execute or Procedure => true,
                _ => false
            };
        }
    }

    public static class DataToolProviders
    {
        public const string MsSql = "mssql";
        public const string Oracle = "plsql";

        public static bool IsRelational(string? value)
        {
            return value?.ToLowerInvariant() switch
            {
                MsSql or Oracle => true,
                _ => false
            };
        }
    }

    public static class DataToolParameterDirections
    {
        public const string Input = "input";
        public const string Output = "output";
        public const string InputOutput = "input_output";

        public static ParameterDirection ToParameterDirection(string? value)
        {
            return value?.ToLowerInvariant() switch
            {
                Output => ParameterDirection.Output,
                InputOutput => ParameterDirection.InputOutput,
                _ => ParameterDirection.Input
            };
        }
    }

    public sealed class DataToolRegistryDocument
    {
        [JsonPropertyName("definitions")]
        public List<DataToolDefinition> Definitions { get; set; } = new();
    }

    public sealed class DataToolDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("tool_name")]
        public string? ToolName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("connection")]
        public string Connection { get; set; } = string.Empty;

        [JsonPropertyName("statement")]
        public string? Statement { get; set; }

        [JsonPropertyName("procedure_name")]
        public string? ProcedureName { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("expose_as_tool")]
        public bool ExposeAsTool { get; set; } = true;

        [JsonPropertyName("command_timeout_seconds")]
        public int? CommandTimeoutSeconds { get; set; }

        [JsonPropertyName("connection_timeout_seconds")]
        public int? ConnectionTimeoutSeconds { get; set; }

        [JsonPropertyName("max_pool_size")]
        public int? MaxPoolSize { get; set; }

        [JsonPropertyName("min_pool_size")]
        public int? MinPoolSize { get; set; }

        [JsonPropertyName("disable_pooling")]
        public bool? DisablePooling { get; set; }

        [JsonPropertyName("parameters")]
        public List<DataToolParameterDefinition> Parameters { get; set; } = new();

        [JsonPropertyName("input_schema")]
        public JsonElement? InputSchema { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("result")]
        public DataToolResultOptions Result { get; set; } = new();

        [JsonPropertyName("provider_options")]
        public JsonElement? ProviderOptions { get; set; }
    }

    public sealed class DataToolParameterDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("db_parameter_name")]
        public string? DbParameterName { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("default_value")]
        public JsonElement? DefaultValue { get; set; }

        [JsonPropertyName("enum")]
        public List<string>? Enum { get; set; }

        [JsonPropertyName("array_item_type")]
        public string? ArrayItemType { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = DataToolParameterDirections.Input;

        [JsonPropertyName("db_type")]
        public string? DbType { get; set; }

        [JsonPropertyName("serialize_as_json")]
        public bool SerializeAsJson { get; set; }
    }

    public sealed class DataToolResultOptions
    {
        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("max_rows")]
        public int MaxRows { get; set; } = 200;

        [JsonPropertyName("include_execution_metadata")]
        public bool IncludeExecutionMetadata { get; set; } = true;

        [JsonPropertyName("include_output_parameters")]
        public bool IncludeOutputParameters { get; set; } = true;

        [JsonPropertyName("output_scalar_parameter")]
        public string? OutputScalarParameter { get; set; }
    }

    public sealed class DataToolExecutionResult
    {
        public bool IsError { get; init; }
        public string? ErrorMessage { get; init; }
        public object? Payload { get; init; }

        public static DataToolExecutionResult Success(object payload)
        {
            return new DataToolExecutionResult
            {
                IsError = false,
                Payload = payload
            };
        }

        public static DataToolExecutionResult Error(string message, object? payload = null)
        {
            return new DataToolExecutionResult
            {
                IsError = true,
                ErrorMessage = message,
                Payload = payload ?? new Dictionary<string, object?> { ["error"] = message }
            };
        }
    }
}