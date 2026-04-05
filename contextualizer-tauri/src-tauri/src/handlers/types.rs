use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HandlersFile {
    pub handlers: Vec<HandlerConfig>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HandlerConfig {
    #[serde(default)]
    pub name: String,

    #[serde(default, rename = "type")]
    pub type_name: String,

    #[serde(default)]
    pub description: Option<String>,

    #[serde(default)]
    pub screen_id: Option<String>,

    #[serde(default)]
    pub validator: Option<String>,

    #[serde(default)]
    pub context_provider: Option<String>,

    #[serde(default)]
    pub title: Option<String>,

    #[serde(default)]
    pub regex: Option<String>,

    #[serde(default, rename = "connectionString")]
    pub connection_string: Option<String>,

    #[serde(default)]
    pub query: Option<String>,

    #[serde(default)]
    pub connector: Option<String>,

    #[serde(default)]
    pub groups: Option<Vec<String>>,

    #[serde(default)]
    pub actions: Vec<ConfigAction>,

    #[serde(default)]
    pub path: Option<String>,

    #[serde(default)]
    pub delimiter: Option<String>,

    #[serde(default)]
    pub key_names: Option<Vec<String>>,

    #[serde(default)]
    pub value_names: Option<Vec<String>>,

    #[serde(default)]
    pub output_format: Option<String>,

    #[serde(default)]
    pub seeder: Option<HashMap<String, String>>,

    #[serde(default)]
    pub constant_seeder: Option<HashMap<String, String>>,

    #[serde(default)]
    pub user_inputs: Option<Vec<UserInputRequest>>,

    #[serde(default)]
    pub file_extensions: Option<Vec<String>>,

    #[serde(default)]
    pub requires_confirmation: bool,

    // API handler
    #[serde(default)]
    pub url: Option<String>,

    #[serde(default)]
    pub method: Option<String>,

    #[serde(default)]
    pub headers: Option<HashMap<String, String>>,

    #[serde(default)]
    pub request_body: Option<serde_json::Value>,

    #[serde(default)]
    pub content_type: Option<String>,

    #[serde(default)]
    pub timeout_seconds: Option<u32>,

    #[serde(default)]
    pub http: Option<HttpConfig>,

    // Database handler
    #[serde(default)]
    pub command_timeout_seconds: Option<u32>,

    #[serde(default)]
    pub connection_timeout_seconds: Option<u32>,

    #[serde(default)]
    pub max_pool_size: Option<u32>,

    #[serde(default)]
    pub min_pool_size: Option<u32>,

    #[serde(default)]
    pub disable_pooling: Option<bool>,

    // Synthetic handler
    #[serde(default)]
    pub reference_handler: Option<String>,

    #[serde(default)]
    pub actual_type: Option<String>,

    #[serde(default)]
    pub synthetic_input: Option<UserInputRequest>,

    // Cron handler
    #[serde(default)]
    pub cron_job_id: Option<String>,

    #[serde(default)]
    pub cron_expression: Option<String>,

    #[serde(default)]
    pub cron_timezone: Option<String>,

    #[serde(default = "default_true")]
    pub cron_enabled: bool,

    // UI behavior
    #[serde(default)]
    pub auto_focus_tab: bool,

    #[serde(default)]
    pub bring_window_to_front: bool,

    // State
    #[serde(default = "default_true")]
    pub enabled: bool,

    // MCP
    #[serde(default)]
    pub mcp_enabled: bool,

    #[serde(default)]
    pub mcp_tool_name: Option<String>,

    #[serde(default)]
    pub mcp_description: Option<String>,

    #[serde(default)]
    pub mcp_input_schema: Option<serde_json::Value>,

    #[serde(default)]
    pub mcp_input_template: Option<String>,

    #[serde(default)]
    pub mcp_return_keys: Option<Vec<String>>,

    #[serde(default)]
    pub mcp_headless: bool,

    #[serde(default)]
    pub mcp_seed_overwrite: bool,
}

fn default_true() -> bool {
    true
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct ConfigAction {
    #[serde(default)]
    pub name: String,

    #[serde(default)]
    pub requires_confirmation: bool,

    #[serde(default)]
    pub key: Option<String>,

    #[serde(default)]
    pub conditions: Option<Condition>,

    #[serde(default)]
    pub user_inputs: Option<Vec<UserInputRequest>>,

    #[serde(default)]
    pub seeder: Option<HashMap<String, String>>,

    #[serde(default)]
    pub constant_seeder: Option<HashMap<String, String>>,

    #[serde(default)]
    pub inner_actions: Option<Vec<ConfigAction>>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct Condition {
    #[serde(default = "default_equals")]
    pub operator: String,

    #[serde(default)]
    pub field: Option<String>,

    #[serde(default)]
    pub value: Option<String>,

    #[serde(default)]
    pub conditions: Option<Vec<Condition>>,
}

fn default_equals() -> String {
    "equals".to_string()
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct UserInputRequest {
    #[serde(default)]
    pub key: String,

    #[serde(default)]
    pub title: String,

    #[serde(default)]
    pub message: String,

    #[serde(default)]
    pub validation_regex: Option<String>,

    #[serde(default = "default_true")]
    pub is_required: bool,

    #[serde(default)]
    pub is_selection_list: bool,

    #[serde(default)]
    pub is_password: bool,

    #[serde(default)]
    pub selection_items: Option<Vec<SelectionItem>>,

    #[serde(default)]
    pub is_multi_select: bool,

    #[serde(default)]
    pub is_file_picker: bool,

    #[serde(default)]
    pub file_extensions: Option<Vec<String>>,

    #[serde(default)]
    pub is_folder_picker: bool,

    #[serde(default)]
    pub is_multi_line: bool,

    #[serde(default)]
    pub is_date: bool,

    #[serde(default)]
    pub is_date_picker: bool,

    #[serde(default)]
    pub is_time: bool,

    #[serde(default)]
    pub is_time_picker: bool,

    #[serde(default)]
    pub is_date_time: bool,

    #[serde(default)]
    pub is_datetime_picker: bool,

    #[serde(default)]
    pub default_value: String,

    #[serde(default)]
    pub dependent_key: Option<String>,

    #[serde(default)]
    pub dependent_selection_item_map: Option<HashMap<String, DependentSelectionItemMap>>,

    #[serde(default)]
    pub config_target: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct DependentSelectionItemMap {
    #[serde(default)]
    pub selection_items: Vec<SelectionItem>,

    #[serde(default)]
    pub default_value: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct SelectionItem {
    #[serde(default)]
    pub value: String,

    #[serde(default)]
    pub display: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpConfig {
    pub request: Option<HttpRequestConfig>,
    pub auth: Option<HttpAuthConfig>,
    pub proxy: Option<HttpProxyConfig>,
    pub tls: Option<HttpTlsConfig>,
    pub timeouts: Option<HttpTimeoutsConfig>,
    pub retry: Option<HttpRetryConfig>,
    pub pagination: Option<HttpPaginationConfig>,
    pub response: Option<HttpResponseConfig>,
    pub output: Option<HttpOutputConfig>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpRequestConfig {
    pub url: Option<String>,
    pub method: Option<String>,
    pub headers: Option<HashMap<String, String>>,
    pub query: Option<HashMap<String, String>>,
    pub body: Option<serde_json::Value>,
    pub body_text: Option<String>,
    pub content_type: Option<String>,
    pub charset: Option<String>,
    pub allow_body_for_get: Option<bool>,
    pub allow_body_for_delete: Option<bool>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpAuthConfig {
    #[serde(rename = "type")]
    pub auth_type: Option<String>,
    pub token: Option<String>,
    pub username: Option<String>,
    pub password: Option<String>,
    pub header_name: Option<String>,
    pub query_name: Option<String>,
    pub token_prefix: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpProxyConfig {
    pub url: Option<String>,
    pub username: Option<String>,
    pub password: Option<String>,
    pub bypass: Option<Vec<String>>,
    pub use_system_proxy: Option<bool>,
    pub use_default_credentials: Option<bool>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpTlsConfig {
    pub allow_invalid_cert: Option<bool>,
    pub min_tls: Option<String>,
    pub client_cert_path: Option<String>,
    pub client_cert_password: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpTimeoutsConfig {
    pub connect_seconds: Option<u32>,
    pub read_seconds: Option<u32>,
    pub overall_seconds: Option<u32>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpRetryConfig {
    pub enabled: Option<bool>,
    pub max_attempts: Option<u32>,
    pub base_delay_ms: Option<u32>,
    pub max_delay_ms: Option<u32>,
    pub jitter: Option<bool>,
    pub retry_on_status: Option<Vec<u16>>,
    pub retry_on_exceptions: Option<Vec<String>>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpPaginationConfig {
    #[serde(rename = "type")]
    pub pagination_type: Option<String>,
    pub cursor_path: Option<String>,
    pub next_param: Option<String>,
    pub limit_param: Option<String>,
    pub offset_param: Option<String>,
    pub page_param: Option<String>,
    pub page_size: Option<u32>,
    pub max_pages: Option<u32>,
    pub start_page: Option<u32>,
    pub start_offset: Option<u32>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpResponseConfig {
    pub expect: Option<String>,
    pub flatten_json: Option<bool>,
    pub flatten_prefix: Option<String>,
    pub max_bytes: Option<u64>,
    pub include_headers: Option<bool>,
    pub header_prefix: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct HttpOutputConfig {
    pub mappings: Option<HashMap<String, String>>,
    pub header_mappings: Option<HashMap<String, String>>,
    pub include_raw_body: Option<bool>,
    pub raw_body_key: Option<String>,
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_existing_handlers_json() {
        let json = include_str!("../../test_fixtures/handlers.json");
        let configs: HandlersFile = serde_json::from_str(json).unwrap();
        assert!(!configs.handlers.is_empty());
        assert_eq!(configs.handlers.len(), 6);
    }

    #[test]
    fn test_all_handler_types_recognized() {
        let json = include_str!("../../test_fixtures/handlers.json");
        let configs: HandlersFile = serde_json::from_str(json).unwrap();
        let known_types = [
            "regex", "lookup", "manual", "database", "cron",
        ];
        for handler in &configs.handlers {
            let t = handler.type_name.to_lowercase();
            assert!(
                known_types.contains(&t.as_str()),
                "Type '{}' should be a recognized type",
                t
            );
        }
    }

    #[test]
    fn test_handler_config_roundtrip() {
        let original = HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            regex: Some(r"\d+".to_string()),
            enabled: true,
            mcp_enabled: true,
            ..Default::default()
        };
        let json = serde_json::to_string(&original).unwrap();
        let parsed: HandlerConfig = serde_json::from_str(&json).unwrap();
        assert_eq!(original.name, parsed.name);
        assert_eq!(original.mcp_enabled, parsed.mcp_enabled);
    }

    #[test]
    fn test_optional_fields_default_to_none() {
        let json = r#"{"name":"minimal","type":"regex","regex":".*"}"#;
        let config: HandlerConfig = serde_json::from_str(json).unwrap();
        assert!(config.connection_string.is_none());
        assert!(config.url.is_none());
        assert!(config.cron_expression.is_none());
    }

    #[test]
    fn test_config_action_deserialization() {
        let json = r#"{
            "name": "copytoclipboard",
            "requires_confirmation": false,
            "key": "_formatted_output",
            "conditions": {
                "operator": "equals",
                "field": "_selector_key",
                "value": "drivercode"
            }
        }"#;
        let action: ConfigAction = serde_json::from_str(json).unwrap();
        assert_eq!(action.name, "copytoclipboard");
        assert!(!action.requires_confirmation);
        let cond = action.conditions.unwrap();
        assert_eq!(cond.operator, "equals");
        assert_eq!(cond.field.unwrap(), "_selector_key");
    }

    #[test]
    fn test_handler_config_with_user_inputs() {
        let json = include_str!("../../test_fixtures/handlers.json");
        let configs: HandlersFile = serde_json::from_str(json).unwrap();
        let manual = configs.handlers.iter().find(|h| h.type_name == "manual").unwrap();
        let inputs = manual.user_inputs.as_ref().unwrap();
        assert_eq!(inputs.len(), 1);
        assert_eq!(inputs[0].key, "input_text");
        assert!(inputs[0].is_required);
    }

    #[test]
    fn test_default_values_from_json() {
        let json = r#"{"name":"test","type":"regex"}"#;
        let config: HandlerConfig = serde_json::from_str(json).unwrap();
        assert!(config.enabled, "enabled should default to true via serde");
        assert!(config.cron_enabled, "cron_enabled should default to true via serde");
        assert!(!config.mcp_enabled);
        assert!(!config.requires_confirmation);
        assert!(!config.mcp_headless);
    }
}
