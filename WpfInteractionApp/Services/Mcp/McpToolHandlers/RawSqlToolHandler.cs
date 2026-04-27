using Contextualizer.Core.Services.DataTools;
using Contextualizer.PluginContracts;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class RawSqlToolHandler
    {
        private static readonly string[] ReservedArgumentNames =
        {
            "sql",
            "mode",
            "parameters",
            "max_rows",
            "command_timeout_seconds"
        };

        public static async Task<JsonRpcResponse?> TryHandleAsync(
            JsonRpcRequest request,
            McpToolsCallParams callParams,
            JsonSerializerOptions jsonOptions)
        {
            var toolDefinition = DataToolMcpSettings.GetRawSqlTools()
                .FirstOrDefault(tool => string.Equals(tool.ToolName, callParams.Name, StringComparison.OrdinalIgnoreCase));

            if (toolDefinition == null)
                return null;

            if (!TryReadArguments(callParams, out var sql, out var mode, out var maxRows, out var commandTimeoutSeconds))
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"{callParams.Name} requires arguments.sql"
                }, jsonOptions), isError: true);
            }

            mode = string.IsNullOrWhiteSpace(mode)
                ? toolDefinition.AllowedModes.First()
                : mode.Trim().ToLowerInvariant();

            if (!toolDefinition.AllowedModes.Contains(mode, StringComparer.OrdinalIgnoreCase))
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"Mode '{mode}' is not enabled for tool '{toolDefinition.ToolName}'. Allowed modes: {string.Join(", ", toolDefinition.AllowedModes)}"
                }, jsonOptions), isError: true);
            }

            var resolvedConnection = DataToolExecutionService.ResolveConnectionTemplate(toolDefinition.ConnectionTemplate);
            if (string.IsNullOrWhiteSpace(resolvedConnection))
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"Configured connection could not be resolved for '{toolDefinition.ToolName}'."
                }, jsonOptions), isError: true);
            }

            var arguments = ExtractParameters(callParams);
            var parameters = BuildParameters(arguments);

            var handlerConfig = new HandlerConfig
            {
                Name = toolDefinition.ToolName,
                Connector = toolDefinition.Provider,
                ConnectionString = resolvedConnection,
                CommandTimeoutSeconds = commandTimeoutSeconds
            };

            try
            {
                using var connection = ConnectionManager.CreateConnection(resolvedConnection, toolDefinition.Provider, handlerConfig);
                var payload = await ExecuteAsync(connection, toolDefinition, sql, mode, parameters, maxRows, commandTimeoutSeconds);
                var text = JsonSerializer.Serialize(payload, jsonOptions);
                return CreateToolResponse(request, text, isError: false);
            }
            catch (Exception ex)
            {
                var text = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"Raw SQL tool '{toolDefinition.ToolName}' failed: {ex.Message}"
                }, jsonOptions);
                return CreateToolResponse(request, text, isError: true);
            }
        }

        private static async Task<Dictionary<string, object?>> ExecuteAsync(
            IDbConnection connection,
            RawSqlToolDefinition toolDefinition,
            string sql,
            string mode,
            DynamicParameters parameters,
            int maxRows,
            int? commandTimeoutSeconds)
        {
            var stopwatch = Stopwatch.StartNew();

            Dictionary<string, object?> payload = mode switch
            {
                DataToolOperationKinds.Select => await ExecuteSelectAsync(connection, sql, parameters, maxRows, commandTimeoutSeconds),
                DataToolOperationKinds.Scalar => await ExecuteScalarAsync(connection, sql, parameters, commandTimeoutSeconds),
                DataToolOperationKinds.Execute => await ExecuteCommandAsync(connection, sql, parameters, commandTimeoutSeconds),
                _ => throw new InvalidOperationException($"Unsupported mode '{mode}'.")
            };

            stopwatch.Stop();
            payload["tool_name"] = toolDefinition.ToolName;
            payload["provider"] = toolDefinition.Provider;
            payload["mode"] = mode;
            payload["elapsed_ms"] = stopwatch.ElapsedMilliseconds;
            return payload;
        }

        private static async Task<Dictionary<string, object?>> ExecuteSelectAsync(
            IDbConnection connection,
            string sql,
            DynamicParameters parameters,
            int maxRows,
            int? commandTimeoutSeconds)
        {
            if (!DataToolExecutionService.IsSafeReadStatement(sql))
                throw new InvalidOperationException("Blocked unsafe SELECT/CTE statement.");

            var rows = await connection.QueryAsync(sql, parameters, commandTimeout: commandTimeoutSeconds ?? 30);
            var rowList = rows.Select(ToDictionary).ToList();
            var boundedMaxRows = Math.Max(1, maxRows);
            var truncated = rowList.Count > boundedMaxRows;
            var returnedRows = truncated ? rowList.Take(boundedMaxRows).ToList() : rowList;

            return new Dictionary<string, object?>
            {
                ["rows"] = returnedRows,
                ["row_count_total"] = rowList.Count,
                ["row_count_returned"] = returnedRows.Count,
                ["truncated"] = truncated
            };
        }

        private static async Task<Dictionary<string, object?>> ExecuteScalarAsync(
            IDbConnection connection,
            string sql,
            DynamicParameters parameters,
            int? commandTimeoutSeconds)
        {
            if (!DataToolExecutionService.IsSafeReadStatement(sql))
                throw new InvalidOperationException("Blocked unsafe scalar statement.");

            var value = await connection.ExecuteScalarAsync(sql, parameters, commandTimeout: commandTimeoutSeconds ?? 30);
            return new Dictionary<string, object?>
            {
                ["value"] = value
            };
        }

        private static async Task<Dictionary<string, object?>> ExecuteCommandAsync(
            IDbConnection connection,
            string sql,
            DynamicParameters parameters,
            int? commandTimeoutSeconds)
        {
            if (!DataToolExecutionService.IsSafeExecuteStatement(sql))
                throw new InvalidOperationException("Blocked unsafe execute statement.");

            var affectedRows = await connection.ExecuteAsync(sql, parameters, commandTimeout: commandTimeoutSeconds ?? 30);
            return new Dictionary<string, object?>
            {
                ["affected_rows"] = affectedRows
            };
        }

        private static DynamicParameters BuildParameters(Dictionary<string, object?> arguments)
        {
            var parameters = new DynamicParameters();
            foreach (var kvp in arguments)
            {
                parameters.Add(NormalizeParameterName(kvp.Key), NormalizeValue(kvp.Value));
            }

            return parameters;
        }

        private static Dictionary<string, object?> ExtractParameters(McpToolsCallParams callParams)
        {
            if (!callParams.Arguments.HasValue || callParams.Arguments.Value.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var arguments = callParams.Arguments.Value;
            if (arguments.TryGetProperty("parameters", out var nestedParameters) && nestedParameters.ValueKind == JsonValueKind.Object)
                return DataToolArgumentConverter.FromJsonObject(nestedParameters);

            return DataToolArgumentConverter.FromJsonObject(arguments, ReservedArgumentNames);
        }

        private static bool TryReadArguments(
            McpToolsCallParams callParams,
            out string sql,
            out string mode,
            out int maxRows,
            out int? commandTimeoutSeconds)
        {
            sql = string.Empty;
            mode = string.Empty;
            maxRows = 200;
            commandTimeoutSeconds = null;

            if (!callParams.Arguments.HasValue || callParams.Arguments.Value.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var prop in callParams.Arguments.Value.EnumerateObject())
            {
                if (prop.NameEquals("sql"))
                    sql = prop.Value.GetString() ?? string.Empty;
                else if (prop.NameEquals("mode"))
                    mode = prop.Value.GetString() ?? string.Empty;
                else if (prop.NameEquals("max_rows") && prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var maxRowsValue))
                    maxRows = maxRowsValue;
                else if (prop.NameEquals("command_timeout_seconds") && prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var timeoutValue))
                    commandTimeoutSeconds = timeoutValue;
            }

            return !string.IsNullOrWhiteSpace(sql);
        }

        private static string NormalizeParameterName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().TrimStart('@', ':');
        }

        private static object? NormalizeValue(object? value)
        {
            return value switch
            {
                Dictionary<string, object?> => JsonSerializer.Serialize(value),
                List<object?> => JsonSerializer.Serialize(value),
                _ => value
            };
        }

        private static Dictionary<string, object?> ToDictionary(object row)
        {
            if (row is IDictionary<string, object> dict)
                return dict.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value, StringComparer.OrdinalIgnoreCase);

            return row.GetType()
                .GetProperties()
                .ToDictionary(property => property.Name, property => property.GetValue(row), StringComparer.OrdinalIgnoreCase);
        }

        private static JsonRpcResponse CreateToolResponse(JsonRpcRequest request, string text, bool isError)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = isError
                }
            };
        }
    }
}