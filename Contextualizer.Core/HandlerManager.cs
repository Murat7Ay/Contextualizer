using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Contextualizer.Core.Services;
using Contextualizer.Core.Management;

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

                foreach (var handler in _handlers.Where(h => h.HandlerConfig.Enabled))
                {
                    var handlerTask = HandlerExecutor.ExecuteHandlerAsync(handler, e.ClipboardContent, logger, contentLength);
                    handlerTasks.Add(handlerTask);
                }

                bool[] results = await Task.WhenAll(handlerTasks);
                int handlersProcessed = results.Count(r => r);

                logger?.LogInfo($"Clipboard processing completed: {handlersProcessed}/{totalHandlers} handlers processed content", new Dictionary<string, object>
                {
                    ["handlers_processed"] = handlersProcessed,
                    ["total_handlers"] = totalHandlers,
                    ["content_length"] = contentLength,
                    ["processing_successful"] = handlersProcessed > 0
                });

                if (handlersProcessed == 0)
                {
                    UserFeedback.ShowActivity(LogType.Warning, $"No handlers could process the clipboard content ({contentLength} chars)");

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

        private void OnLogMessage(object? sender, LogMessageEventArgs e)
        {
            UserFeedback.ShowActivity(e.Level, e.Message);
        }

        public List<string> GetManualHandlerNames()
        {
            return _manualHandlers.Select(s => s.HandlerConfig.Name).ToList();
        }

        public IHandler? GetHandlerByName(string handlerName)
        {
            var handler = _handlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
            if (handler != null) return handler;

            return _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<string> ExecuteHandlerConfig(HandlerConfig handlerConfig)
        {
            return await HandlerExecutor.ExecuteHandlerConfig(handlerConfig);
        }

        public int GetHandlerCount()
        {
            return _handlers.Count + _manualHandlers.Count;
        }

        public List<HandlerConfig> GetAllHandlerConfigs()
        {
            var allConfigs = new List<HandlerConfig>();
            allConfigs.AddRange(_handlers.Select(h => h.HandlerConfig));
            allConfigs.AddRange(_manualHandlers.Select(h => h.HandlerConfig));
            return allConfigs;
        }

        public bool UpdateHandlerEnabledState(string handlerName, bool enabled)
        {
            var handler = _handlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase))
                         ?? _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));

            return HandlerStateManager.UpdateHandlerEnabledState(handler, enabled, GetAllHandlerConfigs(), _settingsService);
        }

        public bool UpdateHandlerMcpEnabledState(string handlerName, bool mcpEnabled)
        {
            var handler = _handlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase))
                         ?? _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));

            return HandlerStateManager.UpdateHandlerMcpEnabledState(handler, mcpEnabled, GetAllHandlerConfigs(), _settingsService);
        }

        public async Task ExecuteManualHandlerAsync(string handlerName)
        {
            var handler = _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
            await HandlerExecutor.ExecuteManualHandlerAsync(handler, handlerName);
        }

        public (int handlersReloaded, int newPluginsLoaded) ReloadHandlers(bool reloadPlugins = false)
        {
            var logger = ServiceLocator.SafeGet<ILoggingService>();
            logger?.LogInfo("Starting handler reload");

            int newPluginsCount = 0;

            if (reloadPlugins)
            {
                var beforeCount = AppDomain.CurrentDomain.GetAssemblies().Length;
                DynamicAssemblyLoader.LoadAssembliesFromFolder(_settingsService.PluginsDirectory);
                var afterCount = AppDomain.CurrentDomain.GetAssemblies().Length;
                newPluginsCount = afterCount - beforeCount;

                if (newPluginsCount > 0)
                {
                    IActionService actionService = new ActionService();
                    ServiceLocator.Register<IActionService>(actionService);
                    logger?.LogInfo($"Reloaded ActionService, {newPluginsCount} new assemblies loaded");
                }
            }

            foreach (var handler in _handlers.OfType<IDisposable>())
            {
                try { handler.Dispose(); } catch { /* ignore */ }
            }
            foreach (var handler in _manualHandlers.OfType<IDisposable>())
            {
                try { handler.Dispose(); } catch { /* ignore */ }
            }

            _handlers.Clear();
            _manualHandlers.Clear();

            List<IHandler> handlers = HandlerLoader.Load(_settingsService.HandlersFilePath);
            _handlers.AddRange(handlers.Where(h => h is not ITriggerableHandler));
            _manualHandlers.AddRange(handlers.Where(h => h is ITriggerableHandler));

            int totalHandlers = _handlers.Count + _manualHandlers.Count;
            logger?.LogInfo($"Handler reload completed: {totalHandlers} handlers loaded, {newPluginsCount} new plugins");

            return (totalHandlers, newPluginsCount);
        }

        public void Dispose()
        {
            _hook.TextCaptured -= OnTextCaptured;
            _hook.LogMessage -= OnLogMessage;
            _hook.Dispose();

            foreach (var handler in _handlers.OfType<IDisposable>())
            {
                handler.Dispose();
            }

            foreach (var handler in _manualHandlers.OfType<IDisposable>())
            {
                handler.Dispose();
            }
        }
    }
}
