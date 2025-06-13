using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Represents an action that displays the details of a given context using a notification service.
    /// </summary>
    /// <remarks>This action retrieves a user interaction service from the provided plugin service provider
    /// and uses it to display key-value pairs from the specified context. If the context is empty or null, a warning
    /// notification is displayed instead.</remarks>
    public class PrintDetails : IAction
    {
        private IPluginServiceProvider _pluginServiceProvider;
        public string Name => "print_details";

        public Task Action(ConfigAction action, ContextWrapper context)
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
            return Task.CompletedTask;  
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _pluginServiceProvider = serviceProvider;
        }
    }
}
