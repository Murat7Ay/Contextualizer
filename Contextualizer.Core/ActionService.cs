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
            LoadActionsAndExecutions();
        }

        private void LoadActionsAndExecutions()
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
                        _actions[instance.Name] = instance;
                    }

                }
                catch (ReflectionTypeLoadException ex)
                {
                    Console.WriteLine($"Assembly yükleme hatası: {assembly.FullName} - {ex.Message}");
                }
            }
        }

        public async Task Action(string actionName, ContextWrapper context)
        {
            ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Action '{actionName}' başlıyor.");
            ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Action '{actionName}' koşul başlıyor.");
            bool isConditionSuccessed = ConditionEvaluator.EvaluateCondition(context.Conditions, context);

            if (!isConditionSuccessed)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Action {actionName} koşul sağlanamadı.");
                return;
            }
            ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Action '{actionName}' koşul bitti.");
            if (_actions.TryGetValue(actionName, out var actionInstance))
            {
                if (context.Confirmable)
                {
                    bool confirmed = await ServiceLocator.Get<IUserInteractionService>().ShowConfirmationAsync("Action Confirmation", $"Do you want to proceed with action: {actionName}?");

                    if (!confirmed) {
                        ServiceLocator.Get<IUserInteractionService>().ShowNotification($"Action {actionName} cancelled.");
                    }
                }

                actionInstance.Action(context);
                ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Action '{actionName}' bitti.");
            }
            else
            {
                ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Action '{actionName}' bulunamadı.");
            }
        }

        public void Action(string name, string contextKey, ContextWrapper context)
        {
            ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Execute '{name}' başlıyor.");

            if (_actions.TryGetValue(name, out var executeInstance))
            {
                executeInstance.Action(contextKey, context);

                ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Execute '{name}' bitti.");
            }
            else
            {
                ServiceLocator.Get<IUserInteractionService>().Log(NotificationType.Info, $"Execute '{name}' bulunamadı.");
            }
        }
    }
}
