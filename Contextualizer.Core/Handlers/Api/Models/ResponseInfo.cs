using System.Text.Json;

namespace Contextualizer.Core.Handlers.Api.Models
{
    internal sealed class ResponseInfo
    {
        public ResponseInfo(string? rawBody, JsonDocument? jsonDocument)
        {
            RawBody = rawBody;
            JsonDocument = jsonDocument;
        }

        public string? RawBody { get; }
        public JsonDocument? JsonDocument { get; }
    }
}
