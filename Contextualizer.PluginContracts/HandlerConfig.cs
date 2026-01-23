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

        // Advanced HTTP configuration (optional; overrides legacy API fields when provided)
        [JsonPropertyName("http")]
        public HttpConfig? Http { get; set; }

        // Database Handler Properties
        [JsonPropertyName("command_timeout_seconds")]
        public int? CommandTimeoutSeconds { get; set; }

        [JsonPropertyName("connection_timeout_seconds")]
        public int? ConnectionTimeoutSeconds { get; set; }

        [JsonPropertyName("max_pool_size")]
        public int? MaxPoolSize { get; set; }

        [JsonPropertyName("min_pool_size")]
        public int? MinPoolSize { get; set; }

        [JsonPropertyName("disable_pooling")]
        public bool? DisablePooling { get; set; }

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

        // UI behavior properties
        [JsonPropertyName("auto_focus_tab")]
        public bool AutoFocusTab { get; set; } = false;
        [JsonPropertyName("bring_window_to_front")]
        public bool BringWindowToFront { get; set; } = false;

        // Handler state properties
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        // MCP (Model Context Protocol) properties
        [JsonPropertyName("mcp_enabled")]
        public bool McpEnabled { get; set; } = false;

        [JsonPropertyName("mcp_tool_name")]
        public string? McpToolName { get; set; }

        [JsonPropertyName("mcp_description")]
        public string? McpDescription { get; set; }

        /// <summary>
        /// MCP tool input schema (JSON Schema object). If null, MCP server will use a default schema (e.g. { text: string }).
        /// </summary>
        [JsonPropertyName("mcp_input_schema")]
        public JsonElement? McpInputSchema { get; set; }

        /// <summary>
        /// If provided, MCP server will build ClipboardContent.Text using this template and the tool arguments as context.
        /// Supports $(key) placeholders and $config: / $file: / $func: processing via HandlerContextProcessor.
        /// </summary>
        [JsonPropertyName("mcp_input_template")]
        public string? McpInputTemplate { get; set; }

        /// <summary>
        /// If provided, MCP server will return only these keys from the handler execution context.
        /// If null/empty, MCP server returns { _formatted_output: ... } by default.
        /// </summary>
        [JsonPropertyName("mcp_return_keys")]
        public List<string>? McpReturnKeys { get; set; }

        /// <summary>
        /// If true, MCP tool calls will run in "headless" mode (no interactive dialogs).
        /// - requires_confirmation: will not show a dialog (execution will be cancelled)
        /// - user_inputs: will not prompt; values must be provided via MCP arguments (or defaults)
        /// </summary>
        [JsonPropertyName("mcp_headless")]
        public bool McpHeadless { get; set; } = false;

        /// <summary>
        /// If true, MCP seed context values are allowed to overwrite existing context keys produced by the handler.
        /// This can be useful when calling regex/group-based handlers but supplying group values directly via MCP.
        /// </summary>
        [JsonPropertyName("mcp_seed_overwrite")]
        public bool McpSeedOverwrite { get; set; } = false;
    }

    public sealed class HttpConfig
    {
        [JsonPropertyName("request")]
        public HttpRequestConfig? Request { get; set; }

        [JsonPropertyName("auth")]
        public HttpAuthConfig? Auth { get; set; }

        [JsonPropertyName("proxy")]
        public HttpProxyConfig? Proxy { get; set; }

        [JsonPropertyName("tls")]
        public HttpTlsConfig? Tls { get; set; }

        [JsonPropertyName("timeouts")]
        public HttpTimeoutsConfig? Timeouts { get; set; }

        [JsonPropertyName("retry")]
        public HttpRetryConfig? Retry { get; set; }

        [JsonPropertyName("pagination")]
        public HttpPaginationConfig? Pagination { get; set; }

        [JsonPropertyName("response")]
        public HttpResponseConfig? Response { get; set; }

        [JsonPropertyName("output")]
        public HttpOutputConfig? Output { get; set; }
    }

    public sealed class HttpRequestConfig
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("query")]
        public Dictionary<string, string>? Query { get; set; }

        [JsonPropertyName("body")]
        public JsonElement? Body { get; set; }

        [JsonPropertyName("body_text")]
        public string? BodyText { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("charset")]
        public string? Charset { get; set; }

        [JsonPropertyName("allow_body_for_get")]
        public bool? AllowBodyForGet { get; set; }

        [JsonPropertyName("allow_body_for_delete")]
        public bool? AllowBodyForDelete { get; set; }
    }

    public sealed class HttpAuthConfig
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; } // basic | bearer | oauth2 | api_key | custom

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("header_name")]
        public string? HeaderName { get; set; }

        [JsonPropertyName("query_name")]
        public string? QueryName { get; set; }

        [JsonPropertyName("token_prefix")]
        public string? TokenPrefix { get; set; } // e.g. "Bearer"
    }

    public sealed class HttpProxyConfig
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }

        [JsonPropertyName("bypass")]
        public List<string>? Bypass { get; set; }

        [JsonPropertyName("use_system_proxy")]
        public bool? UseSystemProxy { get; set; }

        [JsonPropertyName("use_default_credentials")]
        public bool? UseDefaultCredentials { get; set; }
    }

    public sealed class HttpTlsConfig
    {
        [JsonPropertyName("allow_invalid_cert")]
        public bool? AllowInvalidCert { get; set; }

        [JsonPropertyName("min_tls")]
        public string? MinTls { get; set; } // e.g. "1.2"

        [JsonPropertyName("client_cert_path")]
        public string? ClientCertPath { get; set; }

        [JsonPropertyName("client_cert_password")]
        public string? ClientCertPassword { get; set; }
    }

    public sealed class HttpTimeoutsConfig
    {
        [JsonPropertyName("connect_seconds")]
        public int? ConnectSeconds { get; set; }

        [JsonPropertyName("read_seconds")]
        public int? ReadSeconds { get; set; }

        [JsonPropertyName("overall_seconds")]
        public int? OverallSeconds { get; set; }
    }

    public sealed class HttpRetryConfig
    {
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

        [JsonPropertyName("max_attempts")]
        public int? MaxAttempts { get; set; }

        [JsonPropertyName("base_delay_ms")]
        public int? BaseDelayMs { get; set; }

        [JsonPropertyName("max_delay_ms")]
        public int? MaxDelayMs { get; set; }

        [JsonPropertyName("jitter")]
        public bool? Jitter { get; set; }

        [JsonPropertyName("retry_on_status")]
        public List<int>? RetryOnStatus { get; set; }

        [JsonPropertyName("retry_on_exceptions")]
        public List<string>? RetryOnExceptions { get; set; }
    }

    public sealed class HttpPaginationConfig
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; } // cursor | offset | page

        [JsonPropertyName("cursor_path")]
        public string? CursorPath { get; set; }

        [JsonPropertyName("next_param")]
        public string? NextParam { get; set; }

        [JsonPropertyName("limit_param")]
        public string? LimitParam { get; set; }

        [JsonPropertyName("offset_param")]
        public string? OffsetParam { get; set; }

        [JsonPropertyName("page_param")]
        public string? PageParam { get; set; }

        [JsonPropertyName("page_size")]
        public int? PageSize { get; set; }

        [JsonPropertyName("max_pages")]
        public int? MaxPages { get; set; }

        [JsonPropertyName("start_page")]
        public int? StartPage { get; set; }

        [JsonPropertyName("start_offset")]
        public int? StartOffset { get; set; }
    }

    public sealed class HttpResponseConfig
    {
        [JsonPropertyName("expect")]
        public string? Expect { get; set; } // json | text | binary

        [JsonPropertyName("flatten_json")]
        public bool? FlattenJson { get; set; }

        [JsonPropertyName("flatten_prefix")]
        public string? FlattenPrefix { get; set; }

        [JsonPropertyName("max_bytes")]
        public int? MaxBytes { get; set; }

        [JsonPropertyName("include_headers")]
        public bool? IncludeHeaders { get; set; }

        [JsonPropertyName("header_prefix")]
        public string? HeaderPrefix { get; set; }
    }

    public sealed class HttpOutputConfig
    {
        [JsonPropertyName("mappings")]
        public Dictionary<string, string>? Mappings { get; set; } // context_key -> json_path

        [JsonPropertyName("header_mappings")]
        public Dictionary<string, string>? HeaderMappings { get; set; } // context_key -> header_name

        [JsonPropertyName("include_raw_body")]
        public bool? IncludeRawBody { get; set; }

        [JsonPropertyName("raw_body_key")]
        public string? RawBodyKey { get; set; }
    }

}
