using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contextualizer.Core.Services;

namespace Contextualizer.Core
{
    public class HandlerManager : IDisposable
    {
        private readonly List<IHandler> _handlers;
        private readonly List<IHandler> _manualHandlers;
        private readonly KeyboardHook _hook;
        private readonly ISettingsService _settingsService;

        public HandlerManager(IUserInteractionService userInteractionService, ISettingsService settingsService)
        {
            _settingsService = settingsService;
            DynamicAssemblyLoader.LoadAssembliesFromFolder(_settingsService.PluginsDirectory);

            IActionService actionService = new ActionService();
            ServiceLocator.Register<IActionService>(actionService);
            ServiceLocator.Register<IClipboardService>(new WindowsClipboardService());
            ServiceLocator.Register<IUserInteractionService>(userInteractionService);
            ServiceLocator.Register<HandlerManager>(this);
            List<IHandler> handlers = HandlerLoader.Load(_settingsService.HandlersFilePath);
            _handlers = handlers.Where(h => h is not ITriggerableHandler).ToList();
            _manualHandlers = handlers.Where(h => h is ITriggerableHandler).ToList();
            _hook = new KeyboardHook(_settingsService);
            _hook.TextCaptured += OnTextCaptured;
            _hook.LogMessage += OnLogMessage;
        }

        public async Task StartAsync()
        {
            var logger = ServiceLocator.Get<ILoggingService>();
            
            await _hook.StartAsync();
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info,"Listener started.");
            
            logger.LogInfo("HandlerManager started successfully");
            await logger.LogSystemEventAsync("application_start");
        }

        public void Stop()
        {
            var logger = ServiceLocator.Get<ILoggingService>();
            
            _hook.Stop();
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, "Listener stopped.");
            
            logger.LogInfo("HandlerManager stopped");
            _ = logger.LogSystemEventAsync("application_stop");
        }

        private void OnTextCaptured(object? sender, ClipboardCapturedEventArgs e)
        {
            var logger = ServiceLocator.Get<ILoggingService>();
            
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Captured Text: {e.ToString()}");
            
            var contentLength = e.ClipboardContent?.IsText == true ? e.ClipboardContent.Text?.Length ?? 0 : 0;
            logger.LogDebug($"Clipboard content captured: {contentLength} characters");
            _ = logger.LogUserActivityAsync("clipboard_capture", new Dictionary<string, object>
            {
                ["content_length"] = contentLength,
                ["content_type"] = e.ClipboardContent?.IsText == true ? "text" : e.ClipboardContent?.IsFile == true ? "file" : "unknown",
                ["is_text"] = e.ClipboardContent?.IsText ?? false,
                ["is_file"] = e.ClipboardContent?.IsFile ?? false
            });

            foreach (var handler in _handlers)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    handler.Execute(e.ClipboardContent);
                    stopwatch.Stop();
                    
                    logger.LogHandlerExecution(
                        handler.HandlerConfig.Name, 
                        handler.GetType().Name, 
                        stopwatch.Elapsed, 
                        true,
                        new Dictionary<string, object>
                        {
                            ["content_length"] = contentLength
                        });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Error in handler {handler.GetType().Name}: {ex.Message}");
                    
                    logger.LogHandlerError(
                        handler.HandlerConfig.Name, 
                        handler.GetType().Name, 
                        ex,
                        new Dictionary<string, object>
                        {
                            ["content_length"] = contentLength,
                            ["execution_time_ms"] = stopwatch.ElapsedMilliseconds
                        });
                }
            }
        }

        private void OnLogMessage(object? sender, LogMessageEventArgs e)
        {
            ServiceLocator.Get<IUserInteractionService>().Log(e.Level, e.Message);
        }

        public List<string> GetManualHandlerNames()
        {
            return _manualHandlers.Select(s => s.HandlerConfig.Name).ToList();
        }

        public void ExecuteManualHandler(string handlerName)
        {
            var handler = _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Warning, $"Handler not found: {handlerName}");
                return;
            }

            if (handler is ISyntheticContent syntheticHandler)
            {
                var clipboardContent = syntheticHandler.CreateSyntheticContent(handler.HandlerConfig.SyntheticInput);

                if (!clipboardContent.Success)
                {
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, "Failed to create synthetic content.");
                    return;
                }

                if(syntheticHandler.GetActualHandler is not null) {
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Executing synthetic handler: {syntheticHandler.GetActualHandler.HandlerConfig.Name}");
                    syntheticHandler.GetActualHandler.Execute(clipboardContent);
                    return;
                }


                var referenceHandler = GetHandlerByName(handler.HandlerConfig.ReferenceHandler);
                if(referenceHandler != null) {
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Executing reference handler: {referenceHandler.HandlerConfig.Name}");
                    referenceHandler.Execute(clipboardContent);
                }
                else
                {
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Warning, $"Reference handler not found: {handler.HandlerConfig.ReferenceHandler}");
                }
                return;
            }

            var context = new ClipboardContent(); 
            handler.Execute(context);
        }

        public IHandler? GetHandlerByName(string handlerName)
        {
            // Search in both regular and manual handlers (from private readonly List<IHandler> _handlers)
            var handler = _handlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
            if (handler != null) return handler;

            return _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Execute a handler configuration through synthetic content (used by cron scheduler)
        /// </summary>
        /// <param name="handlerConfig">Handler configuration to execute</param>
        /// <returns>Result description</returns>
        public async Task<string> ExecuteHandlerConfig(HandlerConfig handlerConfig)
        {
            try
            {
                // Create a temporary handler from the configuration
                var handler = HandlerFactory.Create(handlerConfig);
                if (handler == null)
                {
                    var error = $"Failed to create handler of type: {handlerConfig.Type}";
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, error);
                    return error;
                }

                // Create synthetic clipboard content based on handler type
                ClipboardContent clipboardContent;
                
                if (handler is ISyntheticContent syntheticHandler && handlerConfig.SyntheticInput != null)
                {
                    // Use synthetic content creation
                    clipboardContent = syntheticHandler.CreateSyntheticContent(handlerConfig.SyntheticInput);
                    if (!clipboardContent.Success)
                    {
                        var error = "Failed to create synthetic content for cron job";
                        ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, error);
                        return error;
                    }
                }
                else
                {
                    // Create default synthetic content for cron trigger
                    clipboardContent = new ClipboardContent
                    {
                        Success = true,
                        IsText = true,
                        Text = $"Cron trigger: {handlerConfig.Name} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    };
                }

                // Execute the handler
                await Task.Run(() => handler.Execute(clipboardContent));
                
                var result = $"Successfully executed cron job: {handlerConfig.Name}";
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, result);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"Error executing cron job {handlerConfig.Name}: {ex.Message}";
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, error);
                return error;
            }
        }

        public void Dispose()
        {
            _hook.TextCaptured -= OnTextCaptured;
            _hook.LogMessage -= OnLogMessage;
            _hook.Dispose();
        }
    }
}
