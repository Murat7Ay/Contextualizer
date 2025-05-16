using Contextualizer.PluginContracts;
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


        public static void DispatchAction(ConfigAction action, ContextWrapper context)
        {
            ServiceLocator.Get<IActionService>().Action(action, context);
        }
    }
}
