
namespace Contextualizer.PluginContracts
{
    public interface IContentValidator
    {
        string Name { get; }
        Task<bool> Validate(ClipboardContent clipboardContent);
    }
} 