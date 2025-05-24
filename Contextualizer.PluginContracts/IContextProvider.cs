using System.Collections.Generic;

namespace Contextualizer.PluginContracts
{
    public interface IContextProvider
    {
        string Name { get; }
        Dictionary<string, string> CreateContext(ClipboardContent clipboardContent);
    }
} 