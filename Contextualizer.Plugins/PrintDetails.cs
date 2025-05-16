using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class PrintDetails : IAction
    {
        private IPluginServiceProvider _pluginServiceProvider;
        public string Name => "print_details";

        public void Action(ConfigAction action, ContextWrapper context)
        {
            var userInteractionService = _pluginServiceProvider.GetService<IUserInteractionService>();

            userInteractionService.ShowNotification("Context Details:");

            if (context != null && context.Count > 0)
            {
                foreach (var kvp in context)
                {
                    string message = $"{kvp.Key}: {kvp.Value}";
                    userInteractionService.ShowNotification(message, LogType.Info, durationInSeconds: 3); 
                }
            }
            else
            {
                userInteractionService.ShowNotification("Context empty.", LogType.Warning);
            }
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _pluginServiceProvider = serviceProvider;
        }
    }
}
