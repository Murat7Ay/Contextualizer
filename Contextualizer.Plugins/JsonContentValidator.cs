using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System.Text.Json;

namespace Contextualizer.Plugins
{
    public class JsonContentValidator : IContentValidator
    {
        public string Name => "jsonvalidator";

        public bool Validate(ClipboardContent clipboardContent)
        {
            if (string.IsNullOrEmpty(clipboardContent.Text))
            {
                return false;
            }

            var input = clipboardContent.Text.Trim();

            if (!(input.StartsWith("{") && input.EndsWith("}")) &&
                !(input.StartsWith("[") && input.EndsWith("]")))
                return false;

            try
            {
                var jsonDocument = JsonDocument.Parse(clipboardContent.Text);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
} 