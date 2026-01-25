using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp.Services.Mcp.McpModels
{
    internal sealed class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string? JsonRpc { get; set; }

        [JsonPropertyName("id")]
        public JsonElement? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
    }

    internal sealed class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public JsonElement? Id { get; set; }

        [JsonPropertyName("result")]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        public JsonRpcError? Error { get; set; }
    }

    internal sealed class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class McpTool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("inputSchema")]
        public JsonElement InputSchema { get; set; }
    }

    internal sealed class McpToolsListResult
    {
        [JsonPropertyName("tools")]
        public List<McpTool> Tools { get; set; } = new();
    }

    internal sealed class McpToolsCallParams
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public JsonElement? Arguments { get; set; }
    }

    internal sealed class UiUserInputsArgs
    {
        [JsonPropertyName("context")]
        public Dictionary<string, string>? Context { get; set; }

        [JsonPropertyName("user_inputs")]
        public List<UserInputRequest> UserInputs { get; set; } = new();
    }

    internal sealed class UiConfirmArgs
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("details")]
        public ConfirmationDetails? Details { get; set; }
    }

    internal sealed class McpToolsCallResult
    {
        [JsonPropertyName("content")]
        public List<McpContentItem> Content { get; set; } = new();

        [JsonPropertyName("isError")]
        public bool IsError { get; set; } = false;
    }

    internal sealed class McpContentItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
