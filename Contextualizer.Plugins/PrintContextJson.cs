using Contextualizer.PluginContracts;

namespace Contextualizer.Plugins
{
    public class PrintContextJson : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "print_context_json";

        public void Action(ConfigAction action, ContextWrapper context)
        {
            pluginServiceProvider.GetService<IUserInteractionService>().ShowNotification(context.ToString());
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
