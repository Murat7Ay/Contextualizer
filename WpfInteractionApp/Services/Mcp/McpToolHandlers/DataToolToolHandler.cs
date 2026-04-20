using Contextualizer.Core;
using Contextualizer.Core.Services.DataTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WpfInteractionApp.Services.Mcp.McpModels;
using WpfInteractionApp.Services.Mcp.McpSchemas;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class DataToolToolHandler
    {
        public const string DataStatementsListToolName = "db_statements_list";
        public const string DataStatementGetToolName = "db_statement_get";
        public const string DbSelectStatementToolName = "db_select_statement";
        public const string DbScalarToolName = "db_scalar";
        public const string DbExecuteToolName = "db_execute";
        public const string DbProcedureExecuteToolName = "db_procedure_execute";

        public static async Task<JsonRpcResponse?> TryHandleBuiltInAsync(JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            var registry = ServiceLocator.SafeGet<DataToolRegistryService>();
            if (registry == null)
                return null;

            if (string.Equals(callParams.Name, DataStatementsListToolName, StringComparison.OrdinalIgnoreCase))
                return CreateStatementsListResponse(request, callParams, registry, jsonOptions);

            if (string.Equals(callParams.Name, DataStatementGetToolName, StringComparison.OrdinalIgnoreCase))
                return CreateStatementGetResponse(request, callParams, registry, jsonOptions);

            if (string.Equals(callParams.Name, DbSelectStatementToolName, StringComparison.OrdinalIgnoreCase))
                return await ExecuteGenericStatementAsync(request, callParams, registry, DataToolOperationKinds.Select, "statement_id", jsonOptions);

            if (string.Equals(callParams.Name, DbScalarToolName, StringComparison.OrdinalIgnoreCase))
                return await ExecuteGenericStatementAsync(request, callParams, registry, DataToolOperationKinds.Scalar, "statement_id", jsonOptions);

            if (string.Equals(callParams.Name, DbExecuteToolName, StringComparison.OrdinalIgnoreCase))
                return await ExecuteGenericStatementAsync(request, callParams, registry, DataToolOperationKinds.Execute, "statement_id", jsonOptions);

            if (string.Equals(callParams.Name, DbProcedureExecuteToolName, StringComparison.OrdinalIgnoreCase))
                return await ExecuteGenericStatementAsync(request, callParams, registry, DataToolOperationKinds.Procedure, "procedure_id", jsonOptions);

            return null;
        }

        public static async Task<JsonRpcResponse?> TryHandleDynamicAsync(JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            var registry = ServiceLocator.SafeGet<DataToolRegistryService>();
            if (registry == null)
                return null;

            var definition = registry.TryGetByToolName(callParams.Name);
            if (definition == null)
                return null;

            var arguments = ExtractDirectArguments(callParams);
            return await ExecuteDefinitionAsync(request, definition, arguments, jsonOptions);
        }

        private static JsonRpcResponse CreateStatementsListResponse(JsonRpcRequest request, McpToolsCallParams callParams, DataToolRegistryService registry, JsonSerializerOptions jsonOptions)
        {
            var args = callParams.Arguments;
            string? provider = null;
            string? operation = null;
            string? tag = null;
            string? search = null;

            if (args.HasValue && args.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in args.Value.EnumerateObject())
                {
                    if (prop.NameEquals("provider")) provider = prop.Value.GetString();
                    else if (prop.NameEquals("operation")) operation = prop.Value.GetString();
                    else if (prop.NameEquals("tag")) tag = prop.Value.GetString();
                    else if (prop.NameEquals("search")) search = prop.Value.GetString();
                }
            }

            var definitions = registry.GetAllDefinitions()
                .Where(d => d.Enabled)
                .Where(d => string.IsNullOrWhiteSpace(provider) || string.Equals(d.Provider, provider, StringComparison.OrdinalIgnoreCase))
                .Where(d => string.IsNullOrWhiteSpace(operation) || string.Equals(d.Operation, operation, StringComparison.OrdinalIgnoreCase))
                .Where(d => string.IsNullOrWhiteSpace(tag) || d.Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                .Where(d => MatchesSearch(d, search))
                .Select(BuildDefinitionSummary)
                .ToList();

            var text = JsonSerializer.Serialize(new Dictionary<string, object?> { ["definitions"] = definitions }, jsonOptions);
            return CreateToolResponse(request, text, isError: false);
        }

        private static JsonRpcResponse CreateStatementGetResponse(JsonRpcRequest request, McpToolsCallParams callParams, DataToolRegistryService registry, JsonSerializerOptions jsonOptions)
        {
            if (!TryGetStringArgument(callParams, "id", out var id) || string.IsNullOrWhiteSpace(id))
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = "db_statement_get requires arguments.id"
                }, jsonOptions), isError: true);
            }

            var definition = registry.TryGetById(id);
            if (definition == null)
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"Data tool definition not found: {id}"
                }, jsonOptions), isError: true);
            }

            var payload = new Dictionary<string, object?>
            {
                ["definition"] = BuildDefinitionDetails(definition)
            };

            return CreateToolResponse(request, JsonSerializer.Serialize(payload, jsonOptions), isError: false);
        }

        private static async Task<JsonRpcResponse> ExecuteGenericStatementAsync(
            JsonRpcRequest request,
            McpToolsCallParams callParams,
            DataToolRegistryService registry,
            string requiredOperation,
            string idFieldName,
            JsonSerializerOptions jsonOptions)
        {
            if (!TryGetStringArgument(callParams, idFieldName, out var statementId) || string.IsNullOrWhiteSpace(statementId))
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"{callParams.Name} requires arguments.{idFieldName}"
                }, jsonOptions), isError: true);
            }

            var definition = registry.TryGetById(statementId);
            if (definition == null)
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"Data tool definition not found: {statementId}"
                }, jsonOptions), isError: true);
            }

            if (!string.Equals(definition.Operation, requiredOperation, StringComparison.OrdinalIgnoreCase))
            {
                return CreateToolResponse(request, JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["error"] = $"Definition '{statementId}' is '{definition.Operation}', not '{requiredOperation}'."
                }, jsonOptions), isError: true);
            }

            var arguments = ExtractGenericArguments(callParams, idFieldName);
            return await ExecuteDefinitionAsync(request, definition, arguments, jsonOptions);
        }

        private static async Task<JsonRpcResponse> ExecuteDefinitionAsync(
            JsonRpcRequest request,
            DataToolDefinition definition,
            Dictionary<string, object?> arguments,
            JsonSerializerOptions jsonOptions)
        {
            var executionResult = await DataToolExecutionService.ExecuteAsync(definition, arguments);
            var text = JsonSerializer.Serialize(executionResult.Payload ?? new Dictionary<string, object?>(), jsonOptions);
            return CreateToolResponse(request, text, executionResult.IsError);
        }

        private static Dictionary<string, object?> ExtractGenericArguments(McpToolsCallParams callParams, string idFieldName)
        {
            if (!callParams.Arguments.HasValue || callParams.Arguments.Value.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var argsElement = callParams.Arguments.Value;
            if (argsElement.TryGetProperty("arguments", out var nestedArgs) && nestedArgs.ValueKind == JsonValueKind.Object)
            {
                return DataToolArgumentConverter.FromJsonObject(nestedArgs);
            }

            return DataToolArgumentConverter.FromJsonObject(argsElement, new[] { idFieldName });
        }

        private static Dictionary<string, object?> ExtractDirectArguments(McpToolsCallParams callParams)
        {
            if (!callParams.Arguments.HasValue || callParams.Arguments.Value.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            return DataToolArgumentConverter.FromJsonObject(callParams.Arguments.Value);
        }

        private static bool TryGetStringArgument(McpToolsCallParams callParams, string name, out string? value)
        {
            value = null;
            if (!callParams.Arguments.HasValue || callParams.Arguments.Value.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var prop in callParams.Arguments.Value.EnumerateObject())
            {
                if (!prop.NameEquals(name))
                    continue;

                value = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.ToString();
                return true;
            }

            return false;
        }

        private static bool MatchesSearch(DataToolDefinition definition, string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return
                definition.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(definition.Name) && definition.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(definition.Description) && definition.Description.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                definition.Tags.Any(t => t.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, object?> BuildDefinitionSummary(DataToolDefinition definition)
        {
            return new Dictionary<string, object?>
            {
                ["id"] = definition.Id,
                ["name"] = definition.Name,
                ["tool_name"] = DataToolRegistryService.ResolveToolName(definition),
                ["description"] = definition.Description,
                ["provider"] = definition.Provider,
                ["operation"] = definition.Operation,
                ["tags"] = definition.Tags,
                ["expose_as_tool"] = definition.ExposeAsTool,
                ["is_supported"] = DataToolExecutionService.IsDefinitionSupported(definition)
            };
        }

        private static Dictionary<string, object?> BuildDefinitionDetails(DataToolDefinition definition)
        {
            return new Dictionary<string, object?>
            {
                ["id"] = definition.Id,
                ["name"] = definition.Name,
                ["tool_name"] = DataToolRegistryService.ResolveToolName(definition),
                ["description"] = definition.Description,
                ["provider"] = definition.Provider,
                ["operation"] = definition.Operation,
                ["tags"] = definition.Tags,
                ["parameters"] = definition.Parameters,
                ["input_schema"] = DataToolSchemas.SchemaForDefinition(definition),
                ["expose_as_tool"] = definition.ExposeAsTool,
                ["is_supported"] = DataToolExecutionService.IsDefinitionSupported(definition)
            };
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