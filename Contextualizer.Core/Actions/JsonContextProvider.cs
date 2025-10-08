using Contextualizer.PluginContracts;
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
            context[ContextKey._input] = clipboardContent.Text;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(clipboardContent.Text))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
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

