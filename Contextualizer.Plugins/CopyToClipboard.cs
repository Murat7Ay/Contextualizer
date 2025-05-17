using Contextualizer.Core;
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

        public void Action(ConfigAction action, ContextWrapper context)
        {
            string value = context[action.Key].ToString();

            serviceProvider.GetService<IClipboardService>().SetText(value);

            serviceProvider.GetService<IUserInteractionService>().ShowNotification($"{action.Key.ToUpper()} : {value} Clipboard kopyalandı. ", LogType.Info, "Kurum Bilgisi", 5, null);

        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
    }
}
