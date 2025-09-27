using System.Collections.Generic;

namespace Contextualizer.PluginContracts
{
    public interface IContextProvider
    {
        string Name { get; }
        Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent);
        
        // Optional: Enhanced context creation with configuration support
        Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent, HandlerConfig? config)
        {
            // Default implementation for backward compatibility
            return CreateContext(clipboardContent);
        }
    }
} 