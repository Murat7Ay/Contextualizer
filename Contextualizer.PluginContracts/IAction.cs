using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface IAction
    {
        string Name { get; }
        void Initialize(IPluginServiceProvider serviceProvider);
        void Action(ConfigAction action, ContextWrapper context);
    }
}
