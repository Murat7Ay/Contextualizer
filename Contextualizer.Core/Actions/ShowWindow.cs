using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core.Actions
{
    /// <summary>
    /// Represents an action that displays a window with specified configuration and context.
    /// </summary>
    /// <remarks>This class implements the <see cref="IAction"/> interface and provides functionality to
    /// display a window using the provided context and configuration. The window's screen ID and title are determined
    /// by the handler configuration in the context.</remarks>
    public class ShowWindow : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "show_window";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            context[ContextKey._body] = context[action.Key];

            pluginServiceProvider.GetService<IUserInteractionService>().ShowWindow(
                context._handlerConfig.ScreenId, 
                context._handlerConfig.Title, 
                context, 
                new(), 
                context._handlerConfig.AutoFocusTab,
                context._handlerConfig.BringWindowToFront);
            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}

