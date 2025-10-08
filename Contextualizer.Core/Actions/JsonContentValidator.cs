using Contextualizer.PluginContracts;
using System.Text.Json;

namespace Contextualizer.Core.Actions
{
    public class JsonContentValidator : IContextValidator
    {
        public string Name => "jsonvalidator";

        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
            {
                return Task.FromResult(false);
            }

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(clipboardContent.Text))
                {
                    return Task.FromResult(true);
                }
            }
            catch (JsonException)
            {
                return Task.FromResult(false);
            }
        }
    }
}

