using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface IHandlerValidator
    {
        string Name { get; }
        bool Validate(ClipboardContent clipboardContent);
        Dictionary<string, string> CreateContext(ClipboardContent clipboardContent);
    }
}
