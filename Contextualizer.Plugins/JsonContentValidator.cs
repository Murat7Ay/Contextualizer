using Contextualizer.PluginContracts;
using System.Text.Json;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Provides functionality to validate whether clipboard content contains valid JSON.
    /// </summary>
    /// <remarks>This validator checks if the text content of a <see cref="ClipboardContent"/> instance is a
    /// syntactically valid JSON object or array. The validation is performed by attempting to parse the text as JSON.
    /// If the text is null, empty, or not valid JSON, the validation fails.</remarks>
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