using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class HandlerManager : IDisposable
    {
        private readonly List<IHandler> _handlers;
        private readonly List<IHandler> _manualHandlers;
        private readonly KeyboardHook _hook;

        public HandlerManager(IUserInteractionService userInteractionService, string handlersFilePath)
        {
            DynamicAssemblyLoader.LoadAssembliesFromFolder(@"C:\Finder\Plugins");

            IActionService actionService = new ActionService();
            ServiceLocator.Register<IActionService>(actionService);
            ServiceLocator.Register<IUserInteractionService>(userInteractionService);
            List<IHandler> handlers = HandlerLoader.Load(handlersFilePath);
            _handlers = handlers.Where(h => h is not ITriggerableHandler).ToList();
            _manualHandlers = handlers.Where(h => h is ITriggerableHandler).ToList();
            _hook = new KeyboardHook();
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
            return _manualHandlers.Select(s => s.HandlerConfig.Name).ToList(); ;
        }

        public void ExecuteManualHandler(string handlerName)
        {
            var handler = _manualHandlers.FirstOrDefault(h => h.HandlerConfig.Name.Equals(handlerName, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Warning, $"Handler not found: {handlerName}");
                return;
            }

            var context = new ClipboardContent(); 
            handler.Execute(context);
        }

        public void Dispose()
        {
            _hook.TextCaptured -= OnTextCaptured;
            _hook.LogMessage -= OnLogMessage;
            _hook.Dispose();
        }
    }
}
