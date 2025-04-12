using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public interface IActionService
    {
        Task Action(string name, ContextWrapper context);
        void Action(string name, string contextKey, ContextWrapper context);
    }
}
