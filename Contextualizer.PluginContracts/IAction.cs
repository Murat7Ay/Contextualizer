using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    internal interface IAction
    {
        string Name { get; }
        void Action(ContextWrapper context);
        void Action(String contextKey, ContextWrapper context);
    }
}
