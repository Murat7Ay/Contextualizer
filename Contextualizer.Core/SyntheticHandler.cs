using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class SyntheticHandler : Dispatch, IHandler, ITriggerableHandler, ISyntheticContent, IDisposable
    {
        public static string TypeName => "Synthetic";

        protected override string OutputFormat => string.Empty;

        private IHandler? _actualHandler = null;
        private bool _disposed = false;

        public SyntheticHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            if (!string.IsNullOrEmpty(handlerConfig.ActualType))
            {
                try
                {
                    // Create a copy of the config with the actual type
                    handlerConfig.Type = handlerConfig.ActualType;
                    _actualHandler = HandlerFactory.Create(handlerConfig);
                    
                    if (_actualHandler == null)
                    {
                        UserFeedback.ShowWarning($"SyntheticHandler '{handlerConfig.Name}': Failed to create actual handler of type '{handlerConfig.ActualType}'");
                    }
                }
                catch (Exception ex)
                {
                    UserFeedback.ShowError($"SyntheticHandler '{handlerConfig.Name}': Error creating actual handler - {ex.Message}");
                }
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

        // Implement IHandler.Execute to handle both ActualType and ReferenceHandler scenarios
        async Task<bool> IHandler.Execute(ClipboardContent clipboardContent)
        {
            // Scenario 1: ActualType - Use embedded _actualHandler
            if (_actualHandler != null)
            {
                try
                {
                    UserFeedback.ShowActivity(LogType.Info, $"SyntheticHandler '{HandlerConfig.Name}': Executing actual handler '{_actualHandler.HandlerConfig.Name}'");
                    return await _actualHandler.Execute(clipboardContent);
                }
                catch (Exception ex)
                {
                    UserFeedback.ShowError($"SyntheticHandler '{HandlerConfig.Name}': Error executing actual handler - {ex.Message}");
                    return false;
                }
            }

            // Scenario 2: ReferenceHandler - Find and execute existing handler
            if (!string.IsNullOrEmpty(HandlerConfig.ReferenceHandler))
            {
                try
                {
                    var handlerManager = ServiceLocator.Get<HandlerManager>();
                    var referenceHandler = handlerManager.GetHandlerByName(HandlerConfig.ReferenceHandler);
                    
                    if (referenceHandler != null)
                    {
                        UserFeedback.ShowActivity(LogType.Info, $"SyntheticHandler '{HandlerConfig.Name}': Executing reference handler '{referenceHandler.HandlerConfig.Name}'");
                        return await referenceHandler.Execute(clipboardContent);
                    }
                    else
                    {
                        UserFeedback.ShowWarning($"SyntheticHandler '{HandlerConfig.Name}': Reference handler not found: {HandlerConfig.ReferenceHandler}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    UserFeedback.ShowError($"SyntheticHandler '{HandlerConfig.Name}': Error executing reference handler - {ex.Message}");
                    return false;
                }
            }

            // Scenario 3: Fallback to base Dispatch execution if neither ActualType nor ReferenceHandler
            UserFeedback.ShowActivity(LogType.Info, $"SyntheticHandler '{HandlerConfig.Name}': No ActualType or ReferenceHandler configured, using base execution");
            return await base.Execute(clipboardContent);
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

        public IHandler? GetActualHandler => _actualHandler;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_actualHandler is IDisposable disposableHandler)
                {
                    disposableHandler.Dispose();
                }
                _disposed = true;
            }
        }
    }
}