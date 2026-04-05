using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class ShellToolHandler
    {
        public const string RunShellToolName = "run_shell";

        private const int DefaultTimeoutSeconds = 30;
        private const int MinTimeoutSeconds = 1;
        private const int MaxTimeoutSeconds = 300;

        public static async Task<JsonRpcResponse> HandleRunShellAsync(
            JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            string command = string.Empty;
            string? workingDirectory = null;
            int timeoutSeconds = DefaultTimeoutSeconds;

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

            timeoutSeconds = Math.Clamp(timeoutSeconds, MinTimeoutSeconds, MaxTimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(workingDirectory) && !System.IO.Directory.Exists(workingDirectory))
            {
                return CreateShellErrorResult(request, $"Working directory does not exist: {workingDirectory}", -1, jsonOptions);
            }

            try
            {
                var result = await ExecuteShellCommandAsync(command, workingDirectory, timeoutSeconds);

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

        private static async Task<ShellResult> ExecuteShellCommandAsync(string command, string? workingDirectory, int timeoutSeconds)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{EscapePowerShellCommand(command)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                psi.WorkingDirectory = workingDirectory;

            var sw = Stopwatch.StartNew();

            using var process = new Process { StartInfo = psi };
            var stdOutBuilder = new StringBuilder();
            var stdErrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stdOutBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stdErrBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            bool timedOut = false;

            try
            {
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
                try { process.Kill(entireProcessTree: true); } catch { }
            }

            sw.Stop();

            var stdout = stdOutBuilder.ToString().TrimEnd();
            var stderr = stdErrBuilder.ToString().TrimEnd();

            const int maxOutputLength = 50_000;
            if (stdout.Length > maxOutputLength)
                stdout = stdout[..maxOutputLength] + $"\n... [truncated, total {stdout.Length} chars]";
            if (stderr.Length > maxOutputLength)
                stderr = stderr[..maxOutputLength] + $"\n... [truncated, total {stderr.Length} chars]";

            return new ShellResult
            {
                ExitCode = timedOut ? -1 : process.ExitCode,
                StdOut = stdout,
                StdErr = stderr,
                TimedOut = timedOut,
                ElapsedMs = sw.ElapsedMilliseconds
            };
        }

        private static string EscapePowerShellCommand(string command)
        {
            return command
                .Replace("\"", "\\\"");
        }

        private sealed class ShellResult
        {
            public int ExitCode { get; set; }
            public string StdOut { get; set; } = string.Empty;
            public string StdErr { get; set; } = string.Empty;
            public bool TimedOut { get; set; }
            public long ElapsedMs { get; set; }
        }
    }
}
