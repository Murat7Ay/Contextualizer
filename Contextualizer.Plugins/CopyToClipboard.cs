using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class CopyToClipboard : IAction
    {
        private IPluginServiceProvider serviceProvider;

        public string Name => "copytoclipboard";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            string value = context[action.Key].ToString();

            serviceProvider.GetService<IClipboardService>().SetText(value);

            serviceProvider.GetService<IUserInteractionService>().ShowNotification($"{action.Key.ToUpper()} : {value} Clipboard kopyalandı. ", LogType.Info, "Clipboard", 5, null);

            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
    }
}
