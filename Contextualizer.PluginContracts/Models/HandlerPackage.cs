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
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("tags")]
        public string[] Tags { get; set; }

        [JsonPropertyName("dependencies")]
        public string[] Dependencies { get; set; }

        [JsonPropertyName("handlerJson")]
        public JsonElement HandlerJson { get; set; }

        [JsonPropertyName("isInstalled")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool IsInstalled { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        [JsonPropertyName("hasUpdate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool HasUpdate { get; set; }

        [JsonPropertyName("installDate")]
        public DateTime InstallDate { get; set; }

        [JsonPropertyName("documentationPath")]
        public string DocumentationPath { get; set; }
    }
} 