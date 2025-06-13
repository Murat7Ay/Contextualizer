using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Represents an action that opens a file using the system's default application.
    /// </summary>
    /// <remarks>This action retrieves the file path from the provided context and attempts to open the file.
    /// It also logs the last access time of the file, if available, using the user interaction service.</remarks>
    public class OpenFile : IAction
    {
        private IPluginServiceProvider pluginServiceProvider;
        public string Name => "open_file";

        public Task Action(ConfigAction action, ContextWrapper context)
        {
            pluginServiceProvider.GetService<IUserInteractionService>().Log(LogType.Info, context[FileInfoKeys.LastAccessUtc]);
            Process.Start(context[FileInfoKeys.FullPath]);
            return Task.CompletedTask;
        }

        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            pluginServiceProvider = serviceProvider;
        }
    }
}
