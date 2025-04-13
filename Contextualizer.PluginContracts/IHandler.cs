using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public interface IHandler
    {
        string Name { get; }
        bool CanHandle(ClipboardContent clipboardContent);
        void Execute(ClipboardContent clipboardContent);
    }
}
