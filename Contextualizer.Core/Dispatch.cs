using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

        public void Execute(string input)
        {
            if (CanHandle(input))
            {
                var context = CreateContext(input);
                ContextWrapper contextWrapper = new ContextWrapper(context.AsReadOnly(), HandlerConfig);
                FindSelectorKey(input, contextWrapper);
                PromptUserInputsAsync(contextWrapper);
                ContextSeederSeed(contextWrapper);
                ContextDefaultSeed(contextWrapper);
                DispatchAction(GetActions(), contextWrapper);
            }
        }

        protected abstract bool CanHandle(string input);

        protected abstract Dictionary<string,string> CreateContext(string input);

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

        private void FindSelectorKey(string input, ContextWrapper context)
        {
            foreach (var kvp in context)
            {
                if (kvp.Value == input)
                {
                    context[ContextKey._selector_key] = kvp.Key;
                    return;
                }
            }
        }

        private void PromptUserInputsAsync(Dictionary<string, string> context)
        {
            if (HandlerConfig.UserInputs is null)
                return;

            foreach (var inpt in HandlerConfig.UserInputs)
            {
                context[inpt.Key] = ServiceLocator.Get<IUserInteractionService>().GetUserInput(inpt)!;
            }
        }

        private void ContextSeederSeed(Dictionary<string, string> context)
        {
            if (HandlerConfig.Seeder is null)
                return;

            foreach (var kvp in HandlerConfig.Seeder)
            {
                context[kvp.Key] = ReplaceDynamicValues(kvp.Value, context);
            }
        }

        private void ContextDefaultSeed(Dictionary<string, string> context)
        {
            if (!context.ContainsKey(ContextKey._self))
            {
                context.TryAdd(ContextKey._self, System.Text.Json.JsonSerializer.Serialize(context, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
            }

            if (!context.ContainsKey(ContextKey._formatted_output))
            {
                context[ContextKey._formatted_output] = string.IsNullOrEmpty(this.OutputFormat)
                    ? context[ContextKey._self]
                    : ReplaceDynamicValues(this.OutputFormat, context);
            }
        }

        private string ReplaceDynamicValues(string input, Dictionary<string, string> context)
        {
            foreach (var kvp in context)
            {
                input = input.Replace($"$({kvp.Key})", kvp.Value);  // $(key) -> value ile değiştir
            }
            return input;
        }
    }
}
