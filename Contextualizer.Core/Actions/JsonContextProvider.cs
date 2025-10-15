using Contextualizer.PluginContracts;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Contextualizer.Core.Actions
{
    public class JsonContextProvider : IContextProvider
    {
        public string Name => "jsonvalidator";

        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            Dictionary<string, string> context = new Dictionary<string, string>();
            
            // ✅ Trim input to match validator behavior and handle whitespace/newlines
            var input = clipboardContent.Text?.Trim() ?? string.Empty;
            context[ContextKey._input] = input;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(input))
                {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string formattedJson = JsonSerializer.Serialize(doc.RootElement, options);
                    context[ContextKey._formatted_output] = formattedJson;
                }
            }
            catch (JsonException ex)
            {
                context[ContextKey._error] = $"Invalid JSON: {ex.Message}";
            }

            return Task.FromResult(context);
        }
    }
}

