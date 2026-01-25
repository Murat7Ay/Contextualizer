using System.Collections.Generic;

namespace Contextualizer.Core.Handlers.Api.Models
{
    internal sealed class NormalizedHttpConfig
    {
        public string? UrlTemplate { get; set; }
        public string? Method { get; set; }
        public Dictionary<string, string>? HeaderTemplates { get; set; }
        public Dictionary<string, string>? QueryTemplates { get; set; }
        public string? BodyJsonTemplate { get; set; }
        public string? BodyTextTemplate { get; set; }
        public string? ContentType { get; set; }
        public string? Charset { get; set; }
        public bool AllowBodyForGet { get; set; }
        public bool AllowBodyForDelete { get; set; }

        public string? AuthType { get; set; }
        public string? AuthTokenTemplate { get; set; }
        public string? AuthUsernameTemplate { get; set; }
        public string? AuthPasswordTemplate { get; set; }
        public string? AuthHeaderName { get; set; }
        public string? AuthQueryName { get; set; }
        public string? AuthTokenPrefix { get; set; }

        public string? ProxyUrl { get; set; }
        public string? ProxyUsername { get; set; }
        public string? ProxyPassword { get; set; }
        public List<string> ProxyBypassList { get; set; } = new();
        public bool UseSystemProxy { get; set; }
        public bool ProxyUseDefaultCredentials { get; set; }

        public bool AllowInvalidCerts { get; set; }
        public string? MinTls { get; set; }
        public string? ClientCertPath { get; set; }
        public string? ClientCertPassword { get; set; }

        public int? ConnectTimeoutSeconds { get; set; }
        public int? OverallTimeoutSeconds { get; set; }
        public int? LegacyTimeoutSeconds { get; set; }

        public bool RetryEnabled { get; set; }
        public int RetryMaxAttempts { get; set; } = 3;
        public int RetryBaseDelayMs { get; set; } = 250;
        public int RetryMaxDelayMs { get; set; } = 5000;
        public bool RetryJitter { get; set; } = true;
        public List<int> RetryOnStatus { get; set; } = new();
        public List<string> RetryOnExceptions { get; set; } = new();

        public string? PaginationType { get; set; }
        public string? PaginationCursorPath { get; set; }
        public string? PaginationNextParam { get; set; }
        public string? PaginationLimitParam { get; set; }
        public string? PaginationOffsetParam { get; set; }
        public string? PaginationPageParam { get; set; }
        public int? PaginationPageSize { get; set; }
        public int? PaginationMaxPages { get; set; }
        public int? PaginationStartPage { get; set; }
        public int? PaginationStartOffset { get; set; }

        public string? ResponseExpect { get; set; }
        public bool FlattenJson { get; set; } = true;
        public string? FlattenPrefix { get; set; }
        public int ResponseMaxBytes { get; set; } = 5 * 1024 * 1024;
        public bool IncludeHeaders { get; set; }
        public string? HeaderPrefix { get; set; } = "Header.";

        public Dictionary<string, string> OutputMappings { get; set; } = new();
        public Dictionary<string, string> OutputHeaderMappings { get; set; } = new();
        public bool IncludeRawBody { get; set; } = true;
        public string? RawBodyKey { get; set; } = "RawResponse";
    }
}
