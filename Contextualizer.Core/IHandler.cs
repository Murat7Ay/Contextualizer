using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    internal interface IHandler
    {
        string Name { get; }
        bool CanHandle(string input);
        void Execute(string input);
    }
}
