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

        public void Action(ConfigAction action, ContextWrapper context)
        {
            List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = new List<KeyValuePair<string, Action<Dictionary<string, string>>>>();
            actions.Add( new KeyValuePair<string, Action<Dictionary<string, string>>>("Print", (context) =>
            {
                string key = context[ContextKey._self].ToString();
                pluginServiceProvider.GetService<IUserInteractionService>().ShowNotification($"Key: {key}", LogType.Info, "Key-Value Pair", 5, null);
            }));

            context[ContextKey._body] = context[action.Key];

            pluginServiceProvider.GetService<IUserInteractionService>().ShowWindow(context._handlerConfig.ScreenId, context._handlerConfig.Title, context, actions);
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
