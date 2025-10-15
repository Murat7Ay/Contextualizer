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

            var input = clipboardContent.Text.Trim();
            
            // ✅ Aggressive validation: Must start with { or [ and end with } or ]
            // This ensures we only accept JSON objects or arrays, not primitives (numbers, strings, booleans, null)
            bool startsCorrectly = input.StartsWith("{") || input.StartsWith("[");
            bool endsCorrectly = input.EndsWith("}") || input.EndsWith("]");
            
            if (!startsCorrectly || !endsCorrectly)
            {
                return Task.FromResult(false);
            }

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(input))
                {
                    // Additional check: Root element must be an object or array
                    var rootKind = doc.RootElement.ValueKind;
                    if (rootKind == JsonValueKind.Object || rootKind == JsonValueKind.Array)
                    {
                        return Task.FromResult(true);
                    }
                    
                    return Task.FromResult(false);
                }
            }
            catch (JsonException)
            {
                return Task.FromResult(false);
            }
        }
    }
}

