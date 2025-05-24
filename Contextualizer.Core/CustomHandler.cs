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

        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            var validator = _actionService.GetContentValidator(HandlerConfig.Validator);
            return validator != null && validator.Validate(clipboardContent);
        }

        bool IHandler.CanHandle(ClipboardContent clipboardContent) => CanHandle(clipboardContent);

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            var contextProvider = _actionService.GetContextProvider(HandlerConfig.ContextProvider);
            return contextProvider?.CreateContext(clipboardContent) ?? new Dictionary<string, string>();
        }

        protected override List<ConfigAction> GetActions() => base.HandlerConfig.Actions; 
    }
}
