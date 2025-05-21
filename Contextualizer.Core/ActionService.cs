using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class ActionService : IActionService
    {
        private readonly Dictionary<string, IAction> _actions = new();
        private readonly Dictionary<string, IHandlerContextProvider> _contextProviders = new();

        public ActionService()
        {
            LoadActions();
        }

        private void LoadActions()
        {
            // Çalışan assembly + Yüklenmiş tüm DLL'leri tara
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    // IAction implementasyonlarını bul
                    var actionTypes = assembly.GetTypes()
                        .Where(t => typeof(IAction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in actionTypes)
                    {
                        var instance = (IAction)Activator.CreateInstance(type);
                        instance.Initialize(new PluginServiceProviderImp());
                        _actions[instance.Name] = instance;
                    }

                    var validatorTypes = assembly.GetTypes()
                       .Where(t => typeof(IHandlerContextProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in validatorTypes)
                    {
                        var instance = (IHandlerContextProvider)Activator.CreateInstance(type);
                        _contextProviders[instance.Name] = instance;
                    }

                }
                catch (ReflectionTypeLoadException ex)
                {
                    Console.WriteLine($"Assembly yükleme hatası: {assembly.FullName} - {ex.Message}");
                }
            }
        }

        public IHandlerContextProvider? GetHandlerContextProvider(string name)
        {
            if (_contextProviders.TryGetValue(name, out var contextProvider))
            {
                return contextProvider;
            }
            return null;
        }

        public async Task Action(ConfigAction configAction, ContextWrapper contextWrapper)
        {
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{configAction.Name}' started.");
            HandlerContextProcessor handlerContextProcessor = new HandlerContextProcessor();
            handlerContextProcessor.PromptUserInputsAsync(configAction.UserInputs, contextWrapper);
            handlerContextProcessor.ContextSeederSeed(configAction.Seeder, contextWrapper);
            bool isConditionSuccessed = ConditionEvaluator.EvaluateCondition(configAction.Conditions, contextWrapper);
            if (!isConditionSuccessed)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Warning, $"Action {configAction.Name} condition failed.");
                return;
            }
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{configAction.Name}' condition finished.");
            if (_actions.TryGetValue(configAction.Name, out var actionInstance))
            {
                if (configAction.RequiresConfirmation)
                {
                    bool confirmed = await ServiceLocator.Get<IUserInteractionService>().ShowConfirmationAsync("Action Confirmation", $"Do you want to proceed with action: {configAction.Name}?");

                    if (!confirmed) {
                        ServiceLocator.Get<IUserInteractionService>().Log(LogType.Warning, $"Action {configAction.Name} cancelled.");
                        return;
                    }
                }

                actionInstance.Action(configAction, contextWrapper);
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{configAction.Name}' finished.");
            }
            else
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Warning, $"Action '{configAction.Name}' not found.");
            }
        }

    }
}
