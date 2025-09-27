
namespace Contextualizer.PluginContracts
{
    public interface IContextValidator
    {
        string Name { get; }
        Task<bool> Validate(ClipboardContent clipboardContent);
        
        // Optional: Enhanced validation with configuration support
        Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig? config)
        {
            // Default implementation for backward compatibility
            return Validate(clipboardContent);
        }
    }
} 