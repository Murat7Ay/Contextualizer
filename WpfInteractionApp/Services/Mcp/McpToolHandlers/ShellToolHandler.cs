using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core.Services.Shell;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class ShellToolHandler
    {
        public const string RunShellToolName = "run_shell";

        public static async Task<JsonRpcResponse> HandleRunShellAsync(
            JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            string command = string.Empty;
            string? workingDirectory = null;
            int timeoutSeconds = ShellCommandExecutor.DefaultTimeoutSeconds;

            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("command"))
                        command = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("working_directory"))
                        workingDirectory = prop.Value.GetString();
                    else if (prop.NameEquals("timeout_seconds"))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var ts))
                            timeoutSeconds = ts;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "run_shell requires arguments.command (non-empty string)" }
                };
            }

            try
            {
                var result = await ShellCommandExecutor.ExecuteAsync(command, workingDirectory, timeoutSeconds);

                var payload = new Dictionary<string, object?>
                {
                    ["exit_code"] = result.ExitCode,
                    ["stdout"] = result.StdOut,
                    ["stderr"] = result.StdErr,
                    ["timed_out"] = result.TimedOut,
                    ["elapsed_ms"] = result.ElapsedMs
                };

                var text = JsonSerializer.Serialize(payload, jsonOptions);

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new McpToolsCallResult
                    {
                        Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                        IsError = result.ExitCode != 0
                    }
                };
            }
            catch (Exception ex)
            {
                return CreateShellErrorResult(request, $"Failed to execute command: {ex.Message}", -1, jsonOptions);
            }
        }

        private static JsonRpcResponse CreateShellErrorResult(JsonRpcRequest request, string error, int exitCode, JsonSerializerOptions jsonOptions)
        {
            var payload = new Dictionary<string, object?>
            {
                ["exit_code"] = exitCode,
                ["stdout"] = string.Empty,
                ["stderr"] = error,
                ["timed_out"] = false,
                ["elapsed_ms"] = 0
            };
            var text = JsonSerializer.Serialize(payload, jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = true
                }
            };
        }
    }
}
