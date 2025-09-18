using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class SyntheticHandler : Dispatch, IHandler, ITriggerableHandler, ISyntheticContent
    {
        public static string TypeName => "Synthetic";

        protected override string OutputFormat => string.Empty;

        private IHandler? _actualHandler = null;

        public SyntheticHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            if (!string.IsNullOrEmpty(handlerConfig.ActualType))
            {
                handlerConfig.Type = handlerConfig.ActualType;
                _actualHandler = HandlerFactory.Create(handlerConfig);
            }
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            return true;
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            return new Dictionary<string, string>();
        }

        protected override List<ConfigAction> GetActions()
        {
            return new List<ConfigAction>();
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }

        public ClipboardContent CreateSyntheticContent(UserInputRequest? userInputRequest)
        {
            if (userInputRequest is null)
            {
                UserFeedback.ShowError("User input request was null or invalid");
                return new ClipboardContent { Success = false };
            }

            var userInput = ServiceLocator.Get<IUserInteractionService>().GetUserInput(userInputRequest);
            if (string.IsNullOrEmpty(userInput))
            {
                UserFeedback.ShowError("User input was null or invalid");
                return new ClipboardContent { Success = false };
            }

            if (userInputRequest.IsFilePicker)
            {
                return new ClipboardContent
                {
                    Success = true,
                    IsFile = true,
                    Files = new[] { userInput }
                };
            }
            return new ClipboardContent
            {
                Success = true,
                IsText = true,
                Text = userInput
            };
        }

        public IHandler? GetActualHandler  => _actualHandler;
    }
}