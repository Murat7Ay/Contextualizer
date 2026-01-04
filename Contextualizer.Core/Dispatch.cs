using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public abstract class Dispatch
    {

        public HandlerConfig HandlerConfig { get; }
        protected Dispatch(HandlerConfig handlerConfig)
        {
            HandlerConfig = handlerConfig;
        }

        public async Task<bool> Execute(ClipboardContent clipboardContent)
        {
            var result = await ExecuteWithResultAsync(clipboardContent);
            return result.Processed;
        }

        /// <summary>
        /// Executes the handler pipeline and returns the generated context and formatted output.
        /// Intended for programmatic callers (e.g., MCP server) that need the handler result.
        /// </summary>
        /// <param name="clipboardContent">Synthetic or real clipboard content.</param>
        /// <param name="seedContext">
        /// Optional seed values to merge into the execution context before seeding/output formatting.
        /// Keys are added only if not already present in the handler-generated context.
        /// </param>
        public async Task<DispatchExecutionResult> ExecuteWithResultAsync(
            ClipboardContent clipboardContent,
            Dictionary<string, string>? seedContext = null)
        {
            var logger = ServiceLocator.SafeGet<ILoggingService>();
            var stopwatch = Stopwatch.StartNew();

            bool canHandle = await CanHandleAsync(clipboardContent);
            if (!canHandle)
            {
                stopwatch.Stop();
                return new DispatchExecutionResult
                {
                    CanHandle = false,
                    Processed = false,
                    Cancelled = false,
                    Context = null,
                    FormattedOutput = null
                };
            }

            if (HandlerConfig.RequiresConfirmation)
            {
                bool confirmed = await ServiceLocator.Get<IUserInteractionService>().ShowConfirmationAsync(
                    "Handler Confirmation",
                    $"Do you want to proceed with handler: {HandlerConfig.Name}? {Environment.NewLine}{HandlerConfig.Description}");

                if (!confirmed)
                {
                    stopwatch.Stop();
                    UserFeedback.ShowWarning($"Handler {HandlerConfig.Name} cancelled");
                    return new DispatchExecutionResult
                    {
                        CanHandle = true,
                        Processed = false,
                        Cancelled = true,
                        Context = null,
                        FormattedOutput = null
                    };
                }
            }

            var context = await CreateContextAsync(clipboardContent);
            ContextWrapper contextWrapper = new ContextWrapper(context.AsReadOnly(), HandlerConfig);

            // Default execution source (can be overridden by programmatic callers like MCP).
            if (!contextWrapper.ContainsKey(ContextKey._trigger))
            {
                contextWrapper[ContextKey._trigger] = "app";
            }

            // Seed optional programmatic context (e.g. MCP tool arguments) without overwriting handler-generated keys.
            if (seedContext != null && seedContext.Count > 0)
            {
                foreach (var kvp in seedContext)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                        continue;

                    // Allow programmatic callers to override trigger source.
                    if (kvp.Key.Equals(ContextKey._trigger, StringComparison.OrdinalIgnoreCase))
                    {
                        contextWrapper[ContextKey._trigger] = kvp.Value ?? string.Empty;
                    }
                    else if (!contextWrapper.ContainsKey(kvp.Key))
                    {
                        contextWrapper[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }
            }

            FindSelectorKey(clipboardContent, contextWrapper);

            HandlerContextProcessor handlerContextProcessor = new HandlerContextProcessor();
            bool isUserCompleted = handlerContextProcessor.PromptUserInputsWithNavigation(HandlerConfig.UserInputs, contextWrapper);

            if (!isUserCompleted)
            {
                stopwatch.Stop();
                UserFeedback.ShowWarning($"Handler {HandlerConfig.Name} cancelled by user input");
                return new DispatchExecutionResult
                {
                    CanHandle = true,
                    Processed = false,
                    Cancelled = true,
                    Context = contextWrapper,
                    FormattedOutput = contextWrapper.TryGetValue(ContextKey._formatted_output, out var fo) ? fo : null
                };
            }

            handlerContextProcessor.ContextResolve(HandlerConfig.ConstantSeeder, HandlerConfig.Seeder, contextWrapper);
            ContextDefaultSeed(contextWrapper);
            DispatchAction(GetActions(), contextWrapper);

            // ✅ Log successful handler execution
            stopwatch.Stop();
            logger?.LogHandlerExecution(
                HandlerConfig.Name,
                this.GetType().Name,
                stopwatch.Elapsed,
                true,
                new Dictionary<string, object>
                {
                    ["content_length"] = clipboardContent?.Text?.Length ?? 0,
                    ["can_handle"] = true,
                    ["executed_actions"] = GetActions()?.Count ?? 0
                });

            UserFeedback.ShowActivity(LogType.Info, $"Handler '{HandlerConfig.Name}' processed content successfully");

            return new DispatchExecutionResult
            {
                CanHandle = true,
                Processed = true,
                Cancelled = false,
                Context = contextWrapper,
                FormattedOutput = contextWrapper.TryGetValue(ContextKey._formatted_output, out var formattedOutput) ? formattedOutput : null
            };
        }

        protected abstract Task<bool> CanHandleAsync(ClipboardContent clipboardContent);

        protected abstract Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent);

        protected abstract List<ConfigAction> GetActions();

        protected abstract string OutputFormat { get; }

        private void DispatchAction(List<ConfigAction> actions, ContextWrapper context)
        {
            if (actions == null || actions.Count == 0)
            {
                return;
            }

            foreach (var action in actions)
            {
                Dispatcher.DispatchAction(action, context);
            }
        }

        private void FindSelectorKey(ClipboardContent clipboardContent, ContextWrapper context)
        {
            if (!clipboardContent.IsText)
            {
                return;
            }


            foreach (var kvp in context)
            {
                if (kvp.Value == clipboardContent.Text)
                {
                    context[ContextKey._selector_key] = kvp.Key;
                    return;
                }
            }
        }

        private void ContextDefaultSeed(Dictionary<string, string> context)
        {
            if (!context.ContainsKey(ContextKey._self))
            {
                var serialized = JsonSerializer.Serialize(context, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                context.TryAdd(ContextKey._self, serialized);
            }

            if (context.ContainsKey(ContextKey._formatted_output))
                return;

            if (string.IsNullOrEmpty(this.OutputFormat))
            {
                context[ContextKey._formatted_output] = context[ContextKey._self];
                return;
            }

            var formattedOutput = HandlerContextProcessor.ReplaceDynamicValues(this.OutputFormat, context);
            context[ContextKey._formatted_output] = formattedOutput;
        }

    }

    public sealed class DispatchExecutionResult
    {
        public bool CanHandle { get; init; }
        public bool Processed { get; init; }
        public bool Cancelled { get; init; }
        public Dictionary<string, string>? Context { get; init; }
        public string? FormattedOutput { get; init; }
    }
}
