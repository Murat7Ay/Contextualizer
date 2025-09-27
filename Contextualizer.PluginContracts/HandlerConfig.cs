using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
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
        [JsonPropertyName("context_provider")]
        public string ContextProvider { get; set; }
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
        [JsonPropertyName("constant_seeder")]
        public Dictionary<string, string> ConstantSeeder { get; set; }
        [JsonPropertyName("user_inputs")]
        public List<UserInputRequest> UserInputs { get; set; }
        [JsonPropertyName("file_extensions")]
        public List<string> FileExtensions { get; set; }
        [JsonPropertyName("requires_confirmation")]
        public bool RequiresConfirmation { get; set; }

        // API Handler Properties
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("request_body")]
        public JsonElement? RequestBody { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("timeout_seconds")]
        public int? TimeoutSeconds { get; set; }

        // Synthetic handler property
        [JsonPropertyName("reference_handler")]
        public string? ReferenceHandler { get; set; }
        [JsonPropertyName("actual_type")]
        public string? ActualType { get; set; }
        [JsonPropertyName("synthetic_input")]
        public UserInputRequest? SyntheticInput { get; set; }

        // Cron handler properties
        [JsonPropertyName("cron_job_id")]
        public string? CronJobId { get; set; }
        [JsonPropertyName("cron_expression")]
        public string? CronExpression { get; set; }
        [JsonPropertyName("cron_timezone")]
        public string? CronTimezone { get; set; }
        [JsonPropertyName("cron_enabled")]
        public bool CronEnabled { get; set; } = true;
    }

}
