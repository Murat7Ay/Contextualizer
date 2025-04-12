using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    internal class OpenFile : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "open_file";

        public void Action(ConfigAction action, ContextWrapper context)
        {
            pluginServiceProvider.GetService<IUserInteractionService>().Log(NotificationType.Info, context[FileInfoKeys.LastAccessUtc]);
            Process.Start(context[FileInfoKeys.FullPath]);
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
