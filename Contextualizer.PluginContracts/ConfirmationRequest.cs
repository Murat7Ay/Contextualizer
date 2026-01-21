using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contextualizer.PluginContracts
{
    public sealed class ConfirmationRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "Confirmation";

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public ConfirmationDetails? Details { get; set; }
    }

    public sealed class ConfirmationDetails
    {
        [JsonPropertyName("format")]
        public string Format { get; set; } = "text"; // text | json

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("json")]
        public JsonElement? Json { get; set; }

        // Table format removed; use markdown tables instead.
    }
}

