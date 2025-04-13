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

            userInteractionService.ShowNotification("ContextWrapper Detayları:");

            if (context != null && context.Count > 0)
            {
                foreach (var kvp in context)
                {
                    string message = $"{kvp.Key}: {kvp.Value}";
                    userInteractionService.ShowNotification(message, LogType.Info, durationInSeconds: 3); // Her bir key-value çiftini ayrı bir bildirim olarak göster
                }
            }
            else
            {
                userInteractionService.ShowNotification("ContextWrapper boş.", LogType.Warning);
            }
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _pluginServiceProvider = serviceProvider;
        }
    }
}
