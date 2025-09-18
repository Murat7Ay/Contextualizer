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
            var logger = ServiceLocator.SafeGet<ILoggingService>();
            var stopwatch = Stopwatch.StartNew();
            
            bool canHandle = await CanHandleAsync(clipboardContent);
            if (canHandle)
            {
                if (HandlerConfig.RequiresConfirmation)
                {
                    bool confirmed = await ServiceLocator.Get<IUserInteractionService>().ShowConfirmationAsync("Handler Confirmation", $"Do you want to proceed with handler: {HandlerConfig.Name}? {Environment.NewLine}{HandlerConfig.Description}");

                    if (!confirmed)
                    {
                        UserFeedback.ShowWarning($"Handler {HandlerConfig.Name} cancelled");
                        return false;
                    }
                }
                var context = await CreateContextAsync(clipboardContent);
                ContextWrapper contextWrapper = new ContextWrapper(context.AsReadOnly(), HandlerConfig);
                FindSelectorKey(clipboardContent, contextWrapper);
                HandlerContextProcessor handlerContextProcessor = new HandlerContextProcessor();
                bool isUserCompleted = handlerContextProcessor.PromptUserInputsWithNavigation(HandlerConfig.UserInputs, contextWrapper);

                if(!isUserCompleted)
                {
                    UserFeedback.ShowWarning($"Handler {HandlerConfig.Name} cancelled by user input");
                    return false;
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
                return true;  // Successfully processed
            }
            
            return false;  // Cannot handle this content
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
}
