using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Contextualizer.Core
{
    public class CustomHandler : Dispatch, IHandler
    {
        private readonly IActionService _actionService;
        private readonly IContextValidator? _cachedValidator;
        private readonly IContextProvider? _cachedContextProvider;

        public CustomHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            _actionService = ServiceLocator.Get<IActionService>();
            
            // Cache plugins at construction time for better performance
            if (!string.IsNullOrWhiteSpace(handlerConfig.Validator))
            {
                _cachedValidator = _actionService.GetContextValidator(handlerConfig.Validator);
                if (_cachedValidator == null)
                {
                    UserFeedback.ShowError($"Validator '{handlerConfig.Validator}' not found during CustomHandler initialization");
                }
            }
            
            if (!string.IsNullOrWhiteSpace(handlerConfig.ContextProvider))
            {
                _cachedContextProvider = _actionService.GetContextProvider(handlerConfig.ContextProvider);
                if (_cachedContextProvider == null)
                {
                    UserFeedback.ShowError($"Context provider '{handlerConfig.ContextProvider}' not found during CustomHandler initialization");
                }
            }
        }

        public static string TypeName => "Custom";

        public HandlerConfig HandlerConfig => base.HandlerConfig;

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            // Validation chain with early returns for better performance
            if (!IsValidClipboardContent(clipboardContent)) return false;
            if (!IsValidatorConfigured()) return false;
            if (!IsValidatorAvailable()) return false;

            return await _cachedValidator!.Validate(clipboardContent, HandlerConfig);
        }

        private bool IsValidClipboardContent(ClipboardContent clipboardContent)
        {
            if (clipboardContent == null || !clipboardContent.Success)
            {
                UserFeedback.ShowError($"Invalid clipboard content provided to CustomHandler '{HandlerConfig.Name}'");
                return false;
            }
            return true;
        }

        private bool IsValidatorConfigured()
        {
            if (string.IsNullOrWhiteSpace(HandlerConfig.Validator))
            {
                UserFeedback.ShowError($"No validator configured for CustomHandler '{HandlerConfig.Name}'");
                return false;
            }
            return true;
        }

        private bool IsValidatorAvailable()
        {
            if (_cachedValidator == null)
            {
                UserFeedback.ShowError($"Validator '{HandlerConfig.Validator}' not available for CustomHandler '{HandlerConfig.Name}'");
                return false;
            }
            return true;
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent) => await CanHandleAsync(clipboardContent);

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            // Validation chain for context creation
            if (!IsValidClipboardContent(clipboardContent)) 
                return new Dictionary<string, string>();
            
            if (!IsContextProviderConfigured()) 
                return new Dictionary<string, string>();
            
            if (!IsContextProviderAvailable()) 
                return new Dictionary<string, string>();

            try
            {
                return await _cachedContextProvider!.CreateContext(clipboardContent, HandlerConfig);
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error creating context in CustomHandler '{HandlerConfig.Name}': {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        private bool IsContextProviderConfigured()
        {
            if (string.IsNullOrWhiteSpace(HandlerConfig.ContextProvider))
            {
                UserFeedback.ShowError($"No context provider configured for CustomHandler '{HandlerConfig.Name}'");
                return false;
            }
            return true;
        }

        private bool IsContextProviderAvailable()
        {
            if (_cachedContextProvider == null)
            {
                UserFeedback.ShowError($"Context provider '{HandlerConfig.ContextProvider}' not available for CustomHandler '{HandlerConfig.Name}'");
                return false;
            }
            return true;
        }

        protected override List<ConfigAction> GetActions() => base.HandlerConfig.Actions;
    }
}
