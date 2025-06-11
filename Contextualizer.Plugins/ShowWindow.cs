using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class ShowWindow : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "show_window";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            context[ContextKey._body] = context[action.Key];

            pluginServiceProvider.GetService<IUserInteractionService>().ShowWindow(context._handlerConfig.ScreenId, context._handlerConfig.Title, context, new());
            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
