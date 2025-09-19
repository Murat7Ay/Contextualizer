﻿using Contextualizer.PluginContracts;
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
            var logger = ServiceLocator.SafeGet<ILoggingService>();
            using (logger?.BeginScope("HandlerManagerStartup", new Dictionary<string, object>
            {
                ["total_handlers"] = _handlers.Count,
                ["manual_handlers"] = _manualHandlers.Count,
                ["startup_time"] = DateTime.UtcNow
            }))
            {
                var stopwatch = Stopwatch.StartNew();
                
                logger?.LogInfo("HandlerManager startup initiated");
                
                await _hook.StartAsync();
                UserFeedback.ShowSuccess("Clipboard listener started");
                
                stopwatch.Stop();
                logger?.LogPerformance("handler_manager_startup", stopwatch.Elapsed, new Dictionary<string, object>
                {
                    ["handlers_loaded"] = _handlers.Count + _manualHandlers.Count
                });
                
                logger?.LogInfo("HandlerManager started successfully");
                if (logger != null)
                    await logger.LogSystemEventAsync("application_start");
            }
        }

        public void Stop()
        {
            var logger = ServiceLocator.SafeGet<ILoggingService>();
            using (logger?.BeginScope("HandlerManagerShutdown"))
            {
                logger?.LogInfo("HandlerManager shutdown initiated");
                
                _hook.Stop();
                UserFeedback.ShowActivity(LogType.Info, "Clipboard listener stopped");
                
                logger?.LogInfo("HandlerManager stopped successfully");
                _ = logger?.LogSystemEventAsync("application_stop");
            }
        }

        private async void OnTextCaptured(object? sender, ClipboardCapturedEventArgs e)
        {
            var logger = ServiceLocator.SafeGet<ILoggingService>();
            var contentLength = e.ClipboardContent?.IsText == true ? e.ClipboardContent.Text?.Length ?? 0 : 0;
            
            using (logger?.BeginScope("ClipboardProcessing", new Dictionary<string, object>
            {
                ["content_length"] = contentLength,
                ["content_type"] = e.ClipboardContent?.IsText == true ? "text" : e.ClipboardContent?.IsFile == true ? "file" : "unknown",
                ["handlers_count"] = _handlers.Count
            }))
            {
                UserFeedback.ShowActivity(LogType.Info, $"Processing clipboard content: {e.ToString()[..Math.Min(50, e.ToString().Length)]}...");
                
                logger?.LogDebug($"Clipboard content captured: {contentLength} characters");
                _ = logger?.LogUserActivityAsync("clipboard_capture", new Dictionary<string, object>
                {
                    ["content_length"] = contentLength,
                    ["content_type"] = e.ClipboardContent?.IsText == true ? "text" : e.ClipboardContent?.IsFile == true ? "file" : "unknown",
                    ["is_text"] = e.ClipboardContent?.IsText ?? false,
                    ["is_file"] = e.ClipboardContent?.IsFile ?? false
                });

                int totalHandlers = _handlers.Count;
                var handlerTasks = new List<Task<bool>>();

                // ✅ Start all handlers in parallel
                foreach (var handler in _handlers)
                {
                    var handlerTask = ExecuteHandlerAsync(handler, e.ClipboardContent, logger, contentLength);
                    handlerTasks.Add(handlerTask);
                }

                // ✅ Wait for all handlers to complete and count successful ones
                bool[] results = await Task.WhenAll(handlerTasks);
                int handlersProcessed = results.Count(r => r);
                
                // ✅ Log summary of clipboard processing attempt
                logger?.LogInfo($"Clipboard processing completed: {handlersProcessed}/{totalHandlers} handlers processed content", new Dictionary<string, object>
                {
                    ["handlers_processed"] = handlersProcessed,
                    ["total_handlers"] = totalHandlers,
                    ["content_length"] = contentLength,
                    ["processing_successful"] = handlersProcessed > 0
                });
                
                // ✅ User feedback based on results
                if (handlersProcessed == 0)
                {
                    UserFeedback.ShowActivity(LogType.Warning, $"No handlers could process the clipboard content ({contentLength} chars)");
                    
                    // ✅ Log user activity for analytics - even when no handlers match
                    _ = logger?.LogUserActivityAsync("clipboard_no_handlers_matched", new Dictionary<string, object>
                    {
                        ["content_length"] = contentLength,
                        ["total_handlers_checked"] = totalHandlers,
                        ["content_type"] = e.ClipboardContent?.IsText == true ? "text" : e.ClipboardContent?.IsFile == true ? "file" : "unknown",
                        ["content_preview"] = e.ToString()[..Math.Min(100, e.ToString().Length)]
                    });
                }
                else if (handlersProcessed == 1)
                {
                    UserFeedback.ShowActivity(LogType.Info, "1 handler processed the clipboard content");
                }
                else
                {
                    UserFeedback.ShowSuccess($"{handlersProcessed} handlers processed the clipboard content");
                }
            }
        }

        private async Task<bool> ExecuteHandlerAsync(IHandler handler, ClipboardContent clipboardContent, ILoggingService? logger, int contentLength)
        {
            using (logger?.BeginScope("HandlerExecution", new Dictionary<string, object>
            {
                ["handler_name"] = handler.HandlerConfig.Name,
                ["handler_type"] = handler.GetType().Name
            }))
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    bool wasProcessed = await handler.Execute(clipboardContent);
                    stopwatch.Stop();
                    
                    // Note: Success logging is handled within the Execute() method itself
                    // Only handlers that actually process content will generate logs
                    return wasProcessed;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    
                    UserFeedback.ShowError($"Handler {handler.GetType().Name} failed: {ex.Message}");
                    
                    logger?.LogHandlerError(
                        handler.HandlerConfig.Name, 
                        handler.GetType().Name, 
                        ex,
                        new Dictionary<string, object>
                        {
                            ["content_length"] = contentLength,
                            ["execution_time_ms"] = stopwatch.ElapsedMilliseconds
                        });
                    
                    return false;  // Failed execution
                }
            }
        }

        private void OnLogMessage(object? sender, LogMessageEventArgs e)
        {
            UserFeedback.ShowActivity(e.Level, e.Message);
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
                UserFeedback.ShowWarning($"Handler not found: {handlerName}");
                return;
            }

            if (handler is ISyntheticContent syntheticHandler)
            {
                var clipboardContent = syntheticHandler.CreateSyntheticContent(handler.HandlerConfig.SyntheticInput);

                if (!clipboardContent.Success)
                {
                    UserFeedback.ShowError("Failed to create synthetic content");
                    return;
                }

                if(syntheticHandler.GetActualHandler is not null) {
                    UserFeedback.ShowActivity(LogType.Info, $"Executing handler: {syntheticHandler.GetActualHandler.HandlerConfig.Name}");
                    syntheticHandler.GetActualHandler.Execute(clipboardContent);
                    return;
                }


                var referenceHandler = GetHandlerByName(handler.HandlerConfig.ReferenceHandler);
                if(referenceHandler != null) {
                    UserFeedback.ShowActivity(LogType.Info, $"Executing reference handler: {referenceHandler.HandlerConfig.Name}");
                    referenceHandler.Execute(clipboardContent);
                }
                else
                {
                    UserFeedback.ShowWarning($"Reference handler not found: {handler.HandlerConfig.ReferenceHandler}");
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
                    UserFeedback.ShowError(error);
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
                        UserFeedback.ShowError(error);
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
                UserFeedback.ShowSuccess(result);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"Error executing cron job {handlerConfig.Name}: {ex.Message}";
                UserFeedback.ShowError(error);
                return error;
            }
        }

        // ✨ New methods for UI dashboard
        public int GetHandlerCount()
        {
            return _handlers.Count + _manualHandlers.Count;
        }

        public async Task ExecuteManualHandlerAsync(string handlerName)
        {
            var handler = _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name == handlerName);
            if (handler != null)
            {
                try
                {
                    await handler.Execute(new ClipboardContent { Text = "", IsText = true });
                }
                catch (Exception ex)
                {
                    var logger = ServiceLocator.SafeGet<ILoggingService>();
                    logger?.LogError($"Failed to execute manual handler '{handlerName}': {ex.Message}");
                }
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
