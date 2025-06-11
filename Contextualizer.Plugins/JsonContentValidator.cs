using Contextualizer.PluginContracts;
using System.Text.Json;

namespace Contextualizer.Plugins
{
    public class JsonContentValidator : IContentValidator
    {
        public string Name => "jsonvalidator";

        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            if (string.IsNullOrEmpty(clipboardContent.Text))
            {
                return Task.FromResult(false);
            }

            var input = clipboardContent.Text.Trim();

            if (!(input.StartsWith("{") && input.EndsWith("}")) &&
                !(input.StartsWith("[") && input.EndsWith("]")))
                return Task.FromResult(false);

            try
            {
                var jsonDocument = JsonDocument.Parse(clipboardContent.Text);
                return Task.FromResult(true);
            }
            catch (JsonException)
            {
                return Task.FromResult(false);
            }
        }
    }
} 