using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contextualizer.Core
{
    public class CustomHandler : Dispatch, IHandler
    {
        private readonly IActionService _actionService;

        public CustomHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            _actionService = ServiceLocator.Get<IActionService>();
        }

        public static string TypeName => "Custom";

        public HandlerConfig HandlerConfig => base.HandlerConfig;

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (clipboardContent == null || !clipboardContent.Success)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, "Invalid clipboard content provided to CustomHandler.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(HandlerConfig.Validator))
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, "No validator configured for CustomHandler.");
                return false;
            }

            var validator = _actionService.GetContentValidator(HandlerConfig.Validator);

            if (validator == null)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Validator {HandlerConfig.Validator} not found.");
                return false;
            }

            return await validator.Validate(clipboardContent);
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent) => await CanHandleAsync(clipboardContent);

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            var contextProvider = _actionService.GetContextProvider(HandlerConfig.ContextProvider);

            if (contextProvider == null)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Context provider {HandlerConfig.ContextProvider} not found.");
                return new Dictionary<string, string>();
            }

            if (clipboardContent == null || !clipboardContent.Success)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, "Invalid clipboard content provided to CustomHandler.");
                return new Dictionary<string, string>();
            }

            return await contextProvider.CreateContext(clipboardContent);
        }

        protected override List<ConfigAction> GetActions() => base.HandlerConfig.Actions;
    }
}
