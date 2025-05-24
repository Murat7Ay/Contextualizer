
namespace Contextualizer.PluginContracts
{
    public interface IClipboardService
    {
        void SetText(string text);
        Task SetTextAsync(string text, CancellationToken cancellation);
    }
}