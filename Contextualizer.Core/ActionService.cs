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

                }
                catch (ReflectionTypeLoadException ex)
                {
                    Console.WriteLine($"Assembly yükleme hatası: {assembly.FullName} - {ex.Message}");
                }
            }
        }

        public async Task Action(ConfigAction action, ContextWrapper context)
        {
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{action.Name}' başlıyor.");
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{action.Name}' koşul başlıyor.");
            bool isConditionSuccessed = ConditionEvaluator.EvaluateCondition(action.Conditions, context);

            if (!isConditionSuccessed)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action {action.Name} koşul sağlanamadı.");
                return;
            }
            ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{action.Name}' koşul bitti.");
            if (_actions.TryGetValue(action.Name, out var actionInstance))
            {
                if (action.RequiresConfirmation)
                {
                    bool confirmed = await ServiceLocator.Get<IUserInteractionService>().ShowConfirmationAsync("Action Confirmation", $"Do you want to proceed with action: {action.Name}?");

                    if (!confirmed) {
                        ServiceLocator.Get<IUserInteractionService>().ShowNotification($"Action {action.Name} cancelled.");
                    }
                }

                actionInstance.Action(action, context);
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{action.Name}' bitti.");
            }
            else
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Info, $"Action '{action.Name}' bulunamadı.");
            }
        }
    }
}
