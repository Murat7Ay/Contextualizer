using Contextualizer.PluginContracts;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services.DataTools
{
    public static class DataToolExecutionService
    {
        public static bool IsDefinitionSupported(DataToolDefinition definition)
        {
            return definition != null && DataToolProviders.IsRelational(definition.Provider);
        }

        public static async Task<DataToolExecutionResult> ExecuteAsync(DataToolDefinition definition, Dictionary<string, object?> arguments)
        {
            if (definition == null)
                return DataToolExecutionResult.Error("Data tool definition is required.");

            if (!IsDefinitionSupported(definition))
                return DataToolExecutionResult.Error($"Provider '{definition.Provider}' is not supported yet.");

            try
            {
                return await ExecuteRelationalAsync(definition, arguments);
            }
            catch (Exception ex)
            {
                return DataToolExecutionResult.Error($"Data tool '{definition.Id}' failed: {ex.Message}");
            }
        }

        private static async Task<DataToolExecutionResult> ExecuteRelationalAsync(DataToolDefinition definition, Dictionary<string, object?> arguments)
        {
            var stopwatch = Stopwatch.StartNew();
            var resolvedConnection = ResolveConnection(definition.Connection);
            if (string.IsNullOrWhiteSpace(resolvedConnection))
                return DataToolExecutionResult.Error($"Data tool '{definition.Id}' could not resolve connection.");

            var handlerConfig = new HandlerConfig
            {
                Name = definition.Name ?? definition.Id,
                Connector = definition.Provider,
                ConnectionString = resolvedConnection,
                CommandTimeoutSeconds = definition.CommandTimeoutSeconds,
                ConnectionTimeoutSeconds = definition.ConnectionTimeoutSeconds,
                MaxPoolSize = definition.MaxPoolSize,
                MinPoolSize = definition.MinPoolSize,
                DisablePooling = definition.DisablePooling
            };

            using var connection = ConnectionManager.CreateConnection(resolvedConnection, definition.Provider, handlerConfig);
            var parametersResult = BuildParameters(definition, arguments);
            if (parametersResult.Error != null)
                return DataToolExecutionResult.Error(parametersResult.Error);

            var dynamicParameters = parametersResult.Parameters!;
            var outputParameterMap = parametersResult.OutputParameterMap!;

            var operation = definition.Operation.ToLowerInvariant();
            object payload;

            switch (operation)
            {
                case DataToolOperationKinds.Select:
                    payload = await ExecuteSelectAsync(connection, definition, dynamicParameters, outputParameterMap);
                    break;

                case DataToolOperationKinds.Scalar:
                    payload = await ExecuteScalarAsync(connection, definition, dynamicParameters, outputParameterMap);
                    break;

                case DataToolOperationKinds.Execute:
                    payload = await ExecuteCommandAsync(connection, definition, dynamicParameters, outputParameterMap);
                    break;

                case DataToolOperationKinds.Procedure:
                    payload = await ExecuteProcedureAsync(connection, definition, dynamicParameters, outputParameterMap);
                    break;

                default:
                    return DataToolExecutionResult.Error($"Unsupported operation '{definition.Operation}'.");
            }

            stopwatch.Stop();

            if (payload is Dictionary<string, object?> dict && definition.Result.IncludeExecutionMetadata)
            {
                dict["tool_id"] = definition.Id;
                dict["operation"] = definition.Operation;
                dict["provider"] = definition.Provider;
                dict["elapsed_ms"] = stopwatch.ElapsedMilliseconds;
            }

            return DataToolExecutionResult.Success(payload);
        }

        private static async Task<Dictionary<string, object?>> ExecuteSelectAsync(
            IDbConnection connection,
            DataToolDefinition definition,
            DynamicParameters parameters,
            Dictionary<string, string> outputParameterMap)
        {
            var statement = definition.Statement?.Trim() ?? string.Empty;
            if (!IsAllowedSelectStatement(statement))
                throw new InvalidOperationException($"Blocked unsafe select statement for '{definition.Id}'.");

            var rows = await connection.QueryAsync(statement, parameters, commandTimeout: definition.CommandTimeoutSeconds ?? 30);
            var rowList = rows.Select(ToDictionary).ToList();
            var maxRows = Math.Max(1, definition.Result.MaxRows);
            var truncated = rowList.Count > maxRows;
            var returnedRows = truncated ? rowList.Take(maxRows).ToList() : rowList;

            var payload = new Dictionary<string, object?>
            {
                ["rows"] = returnedRows,
                ["row_count_total"] = rowList.Count,
                ["row_count_returned"] = returnedRows.Count,
                ["truncated"] = truncated
            };

            AppendOutputParameters(payload, parameters, outputParameterMap, definition);
            return payload;
        }

        private static async Task<Dictionary<string, object?>> ExecuteScalarAsync(
            IDbConnection connection,
            DataToolDefinition definition,
            DynamicParameters parameters,
            Dictionary<string, string> outputParameterMap)
        {
            var statement = definition.Statement?.Trim() ?? string.Empty;
            if (!IsAllowedSelectStatement(statement))
                throw new InvalidOperationException($"Blocked unsafe scalar statement for '{definition.Id}'.");

            var value = await connection.ExecuteScalarAsync(statement, parameters, commandTimeout: definition.CommandTimeoutSeconds ?? 30);
            var payload = new Dictionary<string, object?>
            {
                ["value"] = value
            };

            AppendOutputParameters(payload, parameters, outputParameterMap, definition);
            return payload;
        }

        private static async Task<Dictionary<string, object?>> ExecuteCommandAsync(
            IDbConnection connection,
            DataToolDefinition definition,
            DynamicParameters parameters,
            Dictionary<string, string> outputParameterMap)
        {
            var statement = definition.Statement?.Trim() ?? string.Empty;
            if (!IsAllowedExecuteStatement(statement))
                throw new InvalidOperationException($"Blocked unsafe execute statement for '{definition.Id}'.");

            var affectedRows = await connection.ExecuteAsync(statement, parameters, commandTimeout: definition.CommandTimeoutSeconds ?? 30);
            var payload = new Dictionary<string, object?>
            {
                ["affected_rows"] = affectedRows
            };

            AppendOutputParameters(payload, parameters, outputParameterMap, definition);
            return payload;
        }

        private static async Task<Dictionary<string, object?>> ExecuteProcedureAsync(
            IDbConnection connection,
            DataToolDefinition definition,
            DynamicParameters parameters,
            Dictionary<string, string> outputParameterMap)
        {
            var procedureName = definition.ProcedureName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new InvalidOperationException($"Procedure name is required for '{definition.Id}'.");

            var mode = (definition.Result.Mode ?? DataToolOperationKinds.Execute).ToLowerInvariant();
            Dictionary<string, object?> payload;

            switch (mode)
            {
                case DataToolOperationKinds.Select:
                    var rows = await connection.QueryAsync(procedureName, parameters, commandTimeout: definition.CommandTimeoutSeconds ?? 30, commandType: CommandType.StoredProcedure);
                    var rowList = rows.Select(ToDictionary).ToList();
                    var maxRows = Math.Max(1, definition.Result.MaxRows);
                    var truncated = rowList.Count > maxRows;
                    var returnedRows = truncated ? rowList.Take(maxRows).ToList() : rowList;
                    payload = new Dictionary<string, object?>
                    {
                        ["rows"] = returnedRows,
                        ["row_count_total"] = rowList.Count,
                        ["row_count_returned"] = returnedRows.Count,
                        ["truncated"] = truncated
                    };
                    break;

                case DataToolOperationKinds.Scalar:
                    var affectedForScalar = await connection.ExecuteAsync(procedureName, parameters, commandTimeout: definition.CommandTimeoutSeconds ?? 30, commandType: CommandType.StoredProcedure);
                    var scalarParameterName = NormalizeParameterName(definition.Result.OutputScalarParameter);
                    if (string.IsNullOrWhiteSpace(scalarParameterName))
                        throw new InvalidOperationException($"Procedure scalar mode for '{definition.Id}' requires result.output_scalar_parameter.");

                    payload = new Dictionary<string, object?>
                    {
                        ["value"] = TryGetParameterValue(parameters, scalarParameterName),
                        ["affected_rows"] = affectedForScalar
                    };
                    break;

                default:
                    var affectedRows = await connection.ExecuteAsync(procedureName, parameters, commandTimeout: definition.CommandTimeoutSeconds ?? 30, commandType: CommandType.StoredProcedure);
                    payload = new Dictionary<string, object?>
                    {
                        ["affected_rows"] = affectedRows
                    };
                    break;
            }

            AppendOutputParameters(payload, parameters, outputParameterMap, definition);
            return payload;
        }

        private static (DynamicParameters? Parameters, Dictionary<string, string>? OutputParameterMap, string? Error) BuildParameters(
            DataToolDefinition definition,
            Dictionary<string, object?> arguments)
        {
            var dynamicParameters = new DynamicParameters();
            var outputParameterMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parameterDefinitions = definition.Parameters ?? new List<DataToolParameterDefinition>();

            if (parameterDefinitions.Count == 0)
            {
                foreach (var kvp in arguments)
                {
                    dynamicParameters.Add(NormalizeParameterName(kvp.Key), NormalizeArgumentValue(kvp.Value, serializeAsJson: false));
                }

                return (dynamicParameters, outputParameterMap, null);
            }

            foreach (var parameter in parameterDefinitions)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                    continue;

                var publicName = parameter.Name;
                var dbName = NormalizeParameterName(parameter.DbParameterName ?? publicName);
                var direction = DataToolParameterDirections.ToParameterDirection(parameter.Direction);

                object? value = null;
                var hasValue = arguments.TryGetValue(publicName, out value);

                if (!hasValue && parameter.DefaultValue.HasValue)
                {
                    value = DataToolArgumentConverter.FromJsonValue(parameter.DefaultValue.Value);
                    hasValue = true;
                }

                if ((direction == ParameterDirection.Input || direction == ParameterDirection.InputOutput) && parameter.Required && !hasValue)
                {
                    return (null, null, $"Missing required parameter '{publicName}' for data tool '{definition.Id}'.");
                }

                dynamicParameters.Add(
                    dbName,
                    NormalizeArgumentValue(value, parameter.SerializeAsJson),
                    dbType: ResolveDbType(parameter.DbType),
                    direction: direction);

                if (direction != ParameterDirection.Input)
                {
                    outputParameterMap[publicName] = dbName;
                }
            }

            foreach (var kvp in arguments)
            {
                if (parameterDefinitions.Any(p => string.Equals(p.Name, kvp.Key, StringComparison.OrdinalIgnoreCase)))
                    continue;

                dynamicParameters.Add(NormalizeParameterName(kvp.Key), NormalizeArgumentValue(kvp.Value, serializeAsJson: false));
            }

            return (dynamicParameters, outputParameterMap, null);
        }

        private static void AppendOutputParameters(
            Dictionary<string, object?> payload,
            DynamicParameters parameters,
            Dictionary<string, string> outputParameterMap,
            DataToolDefinition definition)
        {
            if (!definition.Result.IncludeOutputParameters || outputParameterMap.Count == 0)
                return;

            var outputValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in outputParameterMap)
            {
                outputValues[kvp.Key] = TryGetParameterValue(parameters, kvp.Value);
            }

            payload["output_parameters"] = outputValues;
        }

        private static object? TryGetParameterValue(DynamicParameters parameters, string parameterName)
        {
            try
            {
                return parameters.Get<dynamic>(parameterName);
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveConnection(string connectionTemplate)
        {
            return HandlerContextProcessor.ReplaceDynamicValues(
                connectionTemplate,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        private static string NormalizeParameterName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().TrimStart('@', ':');
        }

        private static object? NormalizeArgumentValue(object? value, bool serializeAsJson)
        {
            if (value == null)
                return null;

            if (serializeAsJson)
                return JsonSerializer.Serialize(value);

            return value switch
            {
                Dictionary<string, object?> => JsonSerializer.Serialize(value),
                List<object?> => JsonSerializer.Serialize(value),
                _ => value
            };
        }

        private static DbType? ResolveDbType(string? dbType)
        {
            return dbType?.ToLowerInvariant() switch
            {
                "string" => DbType.String,
                "ansi_string" => DbType.AnsiString,
                "int16" => DbType.Int16,
                "int32" or "int" or "integer" => DbType.Int32,
                "int64" or "long" => DbType.Int64,
                "decimal" => DbType.Decimal,
                "double" => DbType.Double,
                "single" or "float" => DbType.Single,
                "boolean" or "bool" => DbType.Boolean,
                "datetime" => DbType.DateTime,
                "date" => DbType.Date,
                "time" => DbType.Time,
                "guid" => DbType.Guid,
                "binary" => DbType.Binary,
                _ => null
            };
        }

        private static Dictionary<string, object?> ToDictionary(object row)
        {
            if (row is IDictionary<string, object> dict)
            {
                return dict.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
            }

            return row.GetType()
                .GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(row), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsAllowedSelectStatement(string statement)
        {
            if (string.IsNullOrWhiteSpace(statement))
                return false;

            var normalized = statement.ToLowerInvariant().AsSpan().Trim();
            if (!(normalized.StartsWith("select ") || normalized.StartsWith("with ")))
                return false;

            return !ContainsForbiddenContent(normalized);
        }

        private static bool IsAllowedExecuteStatement(string statement)
        {
            if (string.IsNullOrWhiteSpace(statement))
                return false;

            var normalized = statement.ToLowerInvariant().AsSpan().Trim();
            var isAllowedStart =
                normalized.StartsWith("insert ") ||
                normalized.StartsWith("update ") ||
                normalized.StartsWith("delete ") ||
                normalized.StartsWith("merge ");

            return isAllowedStart && !ContainsForbiddenContent(normalized);
        }

        private static bool ContainsForbiddenContent(ReadOnlySpan<char> statement)
        {
            var forbiddenKeywords = new[]
            {
                "drop ",
                "alter ",
                "create ",
                "exec ",
                "execute ",
                "truncate ",
                "grant ",
                "revoke ",
                "shutdown",
                "--",
                "/*",
                "*/",
                "xp_",
                "sp_",
                ";"
            };

            foreach (var keyword in forbiddenKeywords)
            {
                if (statement.Contains(keyword, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}