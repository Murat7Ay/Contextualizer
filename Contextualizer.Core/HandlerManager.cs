using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
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
            await _hook.StartAsync();
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info,"Listener started.");
        }

        public void Stop()
        {
            _hook.Stop();
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, "Listener stopped.");
        }

        private void OnTextCaptured(object? sender, ClipboardCapturedEventArgs e)
        {
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Captured Text: {e.ToString()}");

            foreach (var handler in _handlers)
            {
                try
                {
                    handler.Execute(e.ClipboardContent);
                }
                catch (Exception ex)
                {
                    ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Error in handler {handler.GetType().Name}: {ex.Message}");
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

        public void Dispose()
        {
            _hook.TextCaptured -= OnTextCaptured;
            _hook.LogMessage -= OnLogMessage;
            _hook.Dispose();
        }
    }
}
