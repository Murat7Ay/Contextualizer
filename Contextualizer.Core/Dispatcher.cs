using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public static class Dispatcher
    {
        private static readonly Dictionary<string, IAction> Actions = [];

        static Dispatcher()
        {
            var actionList = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IAction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();

            foreach (var action in actionList)
            {
                var instance = (IAction)Activator.CreateInstance(action);
                Actions.TryAdd(instance.Name, instance);
            }
        }

        public static void DispatchAction(string action, ContextWrapper context)
        {
            ServiceLocator.Get<IActionService>().Action(action, context);
        }

        public static void DispatchExecute(string execute, string contextKey, ContextWrapper context)
        {
            ServiceLocator.Get<IActionService>().Action(execute, contextKey, context);
        }
    }
}
