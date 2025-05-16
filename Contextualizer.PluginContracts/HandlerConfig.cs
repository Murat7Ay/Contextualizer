using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class HandlerConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("screen_id")]
        public string ScreenId { get; set; }
        [JsonPropertyName("validator")]
        public string Validator { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("regex")]
        public string Regex { get; set; }
        [JsonPropertyName("connectionString")]
        public string ConnectionString { get; set; }
        [JsonPropertyName("query")]
        public string Query { get; set; }
        [JsonPropertyName("connector")]
        public string Connector { get; set; }

        [JsonPropertyName("groups")]
        public List<string> Groups { get; set; }

        [JsonPropertyName("actions")]
        public List<ConfigAction> Actions { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("delimiter")]
        public string Delimiter { get; set; }

        [JsonPropertyName("key_names")]
        public List<string> KeyNames { get; set; }

        [JsonPropertyName("value_names")]
        public List<string> ValueNames { get; set; }

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; }
        [JsonPropertyName("seeder")]
        public Dictionary<string, string> Seeder { get; set; }
        [JsonPropertyName("user_inputs")]
        public List<UserInputRequest> UserInputs { get; set; }
        [JsonPropertyName("file_extensions")]
        public List<string> FileExtensions { get; set; }
        [JsonPropertyName("requires_confirmation")]
        public bool RequiresConfirmation { get; set; }
    }

}
