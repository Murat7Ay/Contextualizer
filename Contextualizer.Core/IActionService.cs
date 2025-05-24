using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public interface IActionService
    {
        Task Action(ConfigAction action, ContextWrapper context);
        IContentValidator? GetContentValidator(string name);
        IContextProvider? GetContextProvider(string name);
    }
}
