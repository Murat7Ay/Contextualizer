using Contextualizer.PluginContracts;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Represents an action that prints the current context as a JSON string.
    /// </summary>
    /// <remarks>This action is identified by the name <see cref="Name"/> and is used to display the context
    /// information in a user notification. The context is serialized to a JSON string and shown using the user
    /// interaction service provided by the plugin service provider.</remarks>
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
