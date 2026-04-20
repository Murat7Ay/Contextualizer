using Contextualizer.Core.Services.Shell;
using Contextualizer.PluginContracts;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Contextualizer.Core.Actions
{
    public class RunShell : IAction
    {
        public string Name => "run_shell";

        public async Task Action(ConfigAction action, ContextWrapper context)
        {
            if (string.IsNullOrWhiteSpace(action?.Key))
            {
                ApplyFailure(context, "run_shell requires action.key to point at a context value containing the command.");
                return;
            }

            if (!context.TryGetValue(action.Key, out var command) || string.IsNullOrWhiteSpace(command))
            {
                ApplyFailure(context, $"run_shell could not resolve command from context key '{action.Key}'.");
                return;
            }

            var workingDirectory = TryGetContextValue(context, ContextKey._shell_working_directory);
            var timeoutSeconds = ResolveTimeoutSeconds(context);

            try
            {
                var result = await ShellCommandExecutor.ExecuteAsync(command, workingDirectory, timeoutSeconds);

                ApplyResult(context, ContextKey._shell_stdout, ContextKey._shell_stdout_key, result.StdOut);
                ApplyResult(context, ContextKey._shell_stderr, ContextKey._shell_stderr_key, result.StdErr);
                ApplyResult(context, ContextKey._shell_exit_code, ContextKey._shell_exit_code_key, result.ExitCode.ToString(CultureInfo.InvariantCulture));
                ApplyResult(context, ContextKey._shell_timed_out, ContextKey._shell_timed_out_key, result.TimedOut.ToString());
                ApplyResult(context, ContextKey._shell_elapsed_ms, ContextKey._shell_elapsed_ms_key, result.ElapsedMs.ToString(CultureInfo.InvariantCulture));

                if (result.ExitCode != 0 || result.TimedOut)
                {
                    var errorMessage = string.IsNullOrWhiteSpace(result.StdErr)
                        ? $"Shell command failed with exit code {result.ExitCode}."
                        : result.StdErr;

                    context[ContextKey._error] = errorMessage;
                    UserFeedback.ShowWarning($"run_shell finished with exit code {result.ExitCode}.");
                    return;
                }

                UserFeedback.ShowSuccess("run_shell completed successfully.");
            }
            catch (Exception ex)
            {
                ApplyFailure(context, $"run_shell failed: {ex.Message}");
            }
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
        }

        private static int ResolveTimeoutSeconds(ContextWrapper context)
        {
            if (context.TryGetValue(ContextKey._shell_timeout_seconds, out var timeoutText) &&
                int.TryParse(timeoutText, out var timeoutSeconds))
            {
                return timeoutSeconds;
            }

            return ShellCommandExecutor.DefaultTimeoutSeconds;
        }

        private static string? TryGetContextValue(ContextWrapper context, string key)
        {
            return context.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        private static void ApplyResult(ContextWrapper context, string defaultKey, string aliasKeyContextName, string value)
        {
            context[defaultKey] = value;

            if (context.TryGetValue(aliasKeyContextName, out var aliasKey) && !string.IsNullOrWhiteSpace(aliasKey))
            {
                context[aliasKey] = value;
            }
        }

        private static void ApplyFailure(ContextWrapper context, string errorMessage)
        {
            ApplyResult(context, ContextKey._shell_stdout, ContextKey._shell_stdout_key, string.Empty);
            ApplyResult(context, ContextKey._shell_stderr, ContextKey._shell_stderr_key, errorMessage);
            ApplyResult(context, ContextKey._shell_exit_code, ContextKey._shell_exit_code_key, "-1");
            ApplyResult(context, ContextKey._shell_timed_out, ContextKey._shell_timed_out_key, false.ToString());
            ApplyResult(context, ContextKey._shell_elapsed_ms, ContextKey._shell_elapsed_ms_key, "0");
            context[ContextKey._error] = errorMessage;
            UserFeedback.ShowError(errorMessage);
        }
    }
}