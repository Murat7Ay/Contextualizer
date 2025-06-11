using Contextualizer.PluginContracts;

namespace Contextualizer.Plugins
{
    public class PrintContextJson : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "print_context_json";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            pluginServiceProvider.GetService<IUserInteractionService>().ShowNotification(context.ToString());
            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
