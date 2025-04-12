using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class PrintSingleKey : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "simple_print_key";

        public void Action(Core.ConfigAction action, ContextWrapper context)
        {
            pluginServiceProvider.GetService<IUserInteractionService>().ShowNotification("SIMPLE PRINT KEY");
            pluginServiceProvider.GetService<IUserInteractionService>().ShowNotification(context[action.Key]);
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
