using Contextualizer.Core;

namespace Contextualizer.PluginContracts
{
    public interface IContentValidator
    {
        string Name { get; }
        bool Validate(ClipboardContent clipboardContent);
    }
} 