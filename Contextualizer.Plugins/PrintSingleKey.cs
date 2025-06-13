using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Represents an action that displays a single key-value pair in a user interface.
    /// </summary>
    /// <remarks>This class implements the <see cref="IAction"/> interface and provides functionality to
    /// display a key-value pair using a user interaction service. The action is identified by the name <see
    /// cref="Name"/> and can be executed with the <see cref="Action"/> method.</remarks>
    public class PrintSingleKey : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "simple_print_key";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = new List<KeyValuePair<string, Action<Dictionary<string, string>>>>();
            actions.Add( new KeyValuePair<string, Action<Dictionary<string, string>>>("Print", (context) =>
            {
                string key = context[ContextKey._self].ToString();
                pluginServiceProvider.GetService<IUserInteractionService>().ShowNotification($"Key: {key}", LogType.Info, "Key-Value Pair", 5, null);
            }));

            context[ContextKey._body] = context[action.Key];

            pluginServiceProvider.GetService<IUserInteractionService>().ShowWindow(context._handlerConfig.ScreenId, context._handlerConfig.Title, context, actions);
            return Task.CompletedTask;  
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
