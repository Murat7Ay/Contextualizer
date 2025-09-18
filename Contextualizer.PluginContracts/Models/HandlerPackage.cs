using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contextualizer.PluginContracts.Models
{
    public class HandlerPackage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("tags")]
        public string[] Tags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("dependencies")]
        public string[] Dependencies { get; set; } = Array.Empty<string>();

        [JsonPropertyName("handlerJson")]
        public JsonElement HandlerJson { get; set; }

        [JsonPropertyName("isInstalled")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool IsInstalled { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = [];

        [JsonPropertyName("hasUpdate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool HasUpdate { get; set; }

        [JsonPropertyName("template_user_inputs")]
        public List<UserInputRequest> TemplateUserInputs { get; set; } = new List<UserInputRequest>();
    }
} 