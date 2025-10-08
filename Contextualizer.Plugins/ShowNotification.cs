using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// This plugin shows a notification with a specified message and title.
    /// </summary>
    /// <remarks>This action retrieves the notification title from the context and displays 
    /// a notification using the user interaction service. If no title is provided in the context,
    /// it defaults to "Notification".</remarks>
    public class ShowNotification : IAction
    {
        private IPluginServiceProvider _pluginServiceProvider;

        public string Name => "show_notification";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            var titleNotification = context.TryGetValue(ContextKey._notification_title, out var title)
                ? title
                : "Notification";

            var titleDuration = context.TryGetValue(ContextKey._duration, out var duration) && int.TryParse(duration, out var parsedDuration)
     ? parsedDuration
     : 5;


            var userInteractionService = _pluginServiceProvider.GetService<IUserInteractionService>();
            userInteractionService.ShowNotification(
                context[action.Key],
                LogType.Info,
                titleNotification,
                durationInSeconds: titleDuration,
                null);

            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _pluginServiceProvider = serviceProvider;
        }
    }
}
