using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Contextualizer.Core.Services.Shell
{
    public sealed class ShellCommandResult
    {
        public int ExitCode { get; init; }
        public string StdOut { get; init; } = string.Empty;
        public string StdErr { get; init; } = string.Empty;
        public bool TimedOut { get; init; }
        public long ElapsedMs { get; init; }
    }

    public static class ShellCommandExecutor
    {
        public const int DefaultTimeoutSeconds = 30;
        public const int MinTimeoutSeconds = 1;
        public const int MaxTimeoutSeconds = 300;

        private const int MaxOutputLength = 50_000;

        public static async Task<ShellCommandResult> ExecuteAsync(
            string command,
            string? workingDirectory = null,
            int timeoutSeconds = DefaultTimeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command cannot be empty.", nameof(command));

            timeoutSeconds = Math.Clamp(timeoutSeconds, MinTimeoutSeconds, MaxTimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(workingDirectory) && !Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory does not exist: {workingDirectory}");

            var psi = new ProcessStartInfo
            {
                FileName = ResolveShellExecutable(),
                Arguments = BuildShellArguments(command),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                psi.WorkingDirectory = workingDirectory;

            var stopwatch = Stopwatch.StartNew();
            using var process = new Process { StartInfo = psi };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            bool timedOut = false;

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                timedOut = true;
                TryKillProcess(process);
            }

            if (timedOut)
            {
                try
                {
                    await process.WaitForExitAsync();
                }
                catch
                {
                }
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            stopwatch.Stop();

            return new ShellCommandResult
            {
                ExitCode = timedOut ? -1 : process.ExitCode,
                StdOut = Truncate(stdout),
                StdErr = Truncate(stderr),
                TimedOut = timedOut,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }

        private static string ResolveShellExecutable()
        {
            return OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
        }

        private static string BuildShellArguments(string command)
        {
            var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
            return $"-NoProfile -NonInteractive -EncodedCommand {encodedCommand}";
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }

        private static string Truncate(string? value)
        {
            var normalized = (value ?? string.Empty).TrimEnd();
            if (normalized.Length <= MaxOutputLength)
                return normalized;

            var totalLength = normalized.Length;
            return normalized[..MaxOutputLength] + $"\n... [truncated, total {totalLength} chars]";
        }
    }
}