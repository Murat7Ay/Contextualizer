use serde::Serialize;

pub const EVENT_HANDLER_PROCESSED: &str = "handler-processed";
pub const EVENT_CLIPBOARD_CAPTURED: &str = "clipboard-captured";
pub const EVENT_HANDLERS_RELOADED: &str = "handlers-reloaded";
pub const EVENT_MCP_REQUEST: &str = "mcp-request";
pub const EVENT_SETTINGS_CHANGED: &str = "settings-changed";
pub const EVENT_CRON_TRIGGERED: &str = "cron-triggered";
pub const EVENT_THEME_CHANGED: &str = "theme_changed";
pub const EVENT_LOG: &str = "log";
pub const EVENT_TOAST: &str = "toast";
pub const EVENT_OPEN_TAB: &str = "open_tab";
pub const EVENT_HOST_READY: &str = "host_ready";
pub const EVENT_UI_CONFIRM_REQUEST: &str = "ui_confirm_request";
pub const EVENT_UI_USER_INPUT_REQUEST: &str = "ui_user_input_request";
pub const EVENT_UI_USER_INPUT_NAV_REQUEST: &str = "ui_user_input_navigation_request";

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct HandlerProcessedEvent {
    pub handler_name: String,
    pub handler_type: String,
    pub status: String,
    pub output: Option<String>,
    pub timestamp: u64,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ClipboardCapturedEvent {
    pub content_type: String,
    pub content_length: usize,
    pub preview: String,
    pub timestamp: u64,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct HandlersReloadedEvent {
    pub count: usize,
    pub timestamp: u64,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct McpRequestEvent {
    pub method: String,
    pub tool_name: Option<String>,
    pub timestamp: u64,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SettingsChangedEvent {
    pub changed_keys: Vec<String>,
    pub timestamp: u64,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct CronTriggeredEvent {
    pub job_id: String,
    pub handler_name: String,
    pub timestamp: u64,
}

fn now_millis() -> u64 {
    use std::time::{SystemTime, UNIX_EPOCH};
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_millis() as u64
}

impl HandlerProcessedEvent {
    pub fn new(handler_name: &str, handler_type: &str, status: &str, output: Option<String>) -> Self {
        Self {
            handler_name: handler_name.to_string(),
            handler_type: handler_type.to_string(),
            status: status.to_string(),
            output,
            timestamp: now_millis(),
        }
    }
}

impl ClipboardCapturedEvent {
    pub fn new(content_type: &str, content: &str) -> Self {
        let preview = if content.len() > 100 {
            format!("{}...", &content[..100])
        } else {
            content.to_string()
        };
        Self {
            content_type: content_type.to_string(),
            content_length: content.len(),
            preview,
            timestamp: now_millis(),
        }
    }
}

impl HandlersReloadedEvent {
    pub fn new(count: usize) -> Self {
        Self {
            count,
            timestamp: now_millis(),
        }
    }
}

impl McpRequestEvent {
    pub fn new(method: &str, tool_name: Option<&str>) -> Self {
        Self {
            method: method.to_string(),
            tool_name: tool_name.map(|s| s.to_string()),
            timestamp: now_millis(),
        }
    }
}

impl CronTriggeredEvent {
    pub fn new(job_id: &str, handler_name: &str) -> Self {
        Self {
            job_id: job_id.to_string(),
            handler_name: handler_name.to_string(),
            timestamp: now_millis(),
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct LogEvent {
    pub level: String,
    pub message: String,
    pub details: Option<String>,
}

impl LogEvent {
    pub fn new(level: &str, message: &str, details: Option<String>) -> Self {
        Self {
            level: level.to_string(),
            message: message.to_string(),
            details,
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ToastEvent {
    pub toast_id: Option<String>,
    pub level: String,
    pub title: Option<String>,
    pub message: String,
    pub details: Option<String>,
    pub duration_seconds: u32,
    pub actions: Vec<ToastAction>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ToastAction {
    pub id: String,
    pub label: String,
}

impl ToastEvent {
    pub fn info(message: &str, duration: u32) -> Self {
        Self {
            toast_id: None,
            level: "info".to_string(),
            title: None,
            message: message.to_string(),
            details: None,
            duration_seconds: duration,
            actions: vec![],
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct OpenTabEvent {
    pub screen_id: String,
    pub title: String,
    pub context: serde_json::Value,
    pub actions: Vec<TabAction>,
    pub auto_focus: bool,
    pub bring_to_front: bool,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct TabAction {
    pub id: String,
    pub label: String,
}

impl OpenTabEvent {
    pub fn new(screen_id: &str, title: &str, context: serde_json::Value) -> Self {
        Self {
            screen_id: screen_id.to_string(),
            title: title.to_string(),
            context,
            actions: vec![],
            auto_focus: true,
            bring_to_front: true,
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct HostReadyEvent {
    pub app_version: String,
    pub theme: String,
    pub mcp_url: Option<String>,
    pub protocol_version: u32,
}

impl HostReadyEvent {
    pub fn new(mcp_port: u16, theme: &str) -> Self {
        Self {
            app_version: env!("CARGO_PKG_VERSION").to_string(),
            theme: theme.to_string(),
            mcp_url: Some(format!("http://127.0.0.1:{}/mcp", mcp_port)),
            protocol_version: 1,
        }
    }
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct UiConfirmRequestEvent {
    pub request_id: String,
    pub title: Option<String>,
    pub message: String,
    pub details: Option<serde_json::Value>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct UiUserInputRequestEvent {
    pub request_id: String,
    pub request: serde_json::Value,
    pub context: Option<serde_json::Value>,
}

#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct UiUserInputNavRequestEvent {
    pub request_id: String,
    pub request: serde_json::Value,
    pub context: serde_json::Value,
    pub can_go_back: bool,
    pub current_step: u32,
    pub total_steps: u32,
}

/// Emit a global event via Tauri's Emitter trait.
/// This is a convenience wrapper for use in non-command contexts.
pub fn emit_event<S: Serialize + Clone>(
    app: &tauri::AppHandle,
    event_name: &str,
    payload: &S,
) -> Result<(), String> {
    use tauri::Emitter;
    app.emit(event_name, payload)
        .map_err(|e| format!("Failed to emit event '{}': {}", event_name, e))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_handler_processed_event_serialization() {
        let event = HandlerProcessedEvent::new("test", "regex", "processed", Some("output".to_string()));
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"handlerName\":\"test\""));
        assert!(json.contains("\"handlerType\":\"regex\""));
        assert!(json.contains("\"status\":\"processed\""));
        assert!(event.timestamp > 0);
    }

    #[test]
    fn test_clipboard_captured_event_truncation() {
        let long_content = "a".repeat(200);
        let event = ClipboardCapturedEvent::new("text", &long_content);
        assert_eq!(event.content_length, 200);
        assert!(event.preview.ends_with("..."));
        assert_eq!(event.preview.len(), 103); // 100 + "..."
    }

    #[test]
    fn test_clipboard_captured_event_short_content() {
        let event = ClipboardCapturedEvent::new("text", "hello");
        assert_eq!(event.preview, "hello");
        assert_eq!(event.content_length, 5);
    }

    #[test]
    fn test_handlers_reloaded_event() {
        let event = HandlersReloadedEvent::new(5);
        assert_eq!(event.count, 5);
        assert!(event.timestamp > 0);
    }

    #[test]
    fn test_mcp_request_event() {
        let event = McpRequestEvent::new("tools/call", Some("read_file"));
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"method\":\"tools/call\""));
        assert!(json.contains("\"toolName\":\"read_file\""));
    }

    #[test]
    fn test_mcp_request_event_no_tool() {
        let event = McpRequestEvent::new("initialize", None);
        assert!(event.tool_name.is_none());
    }

    #[test]
    fn test_cron_triggered_event() {
        let event = CronTriggeredEvent::new("job1", "handler1");
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"jobId\":\"job1\""));
        assert!(json.contains("\"handlerName\":\"handler1\""));
    }

    #[test]
    fn test_settings_changed_event() {
        let event = SettingsChangedEvent {
            changed_keys: vec!["shortcut".to_string(), "theme".to_string()],
            timestamp: now_millis(),
        };
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"changedKeys\""));
        assert!(json.contains("shortcut"));
    }

    #[test]
    fn test_event_constants() {
        assert_eq!(EVENT_HANDLER_PROCESSED, "handler-processed");
        assert_eq!(EVENT_CLIPBOARD_CAPTURED, "clipboard-captured");
        assert_eq!(EVENT_HANDLERS_RELOADED, "handlers-reloaded");
        assert_eq!(EVENT_MCP_REQUEST, "mcp-request");
        assert_eq!(EVENT_SETTINGS_CHANGED, "settings-changed");
        assert_eq!(EVENT_CRON_TRIGGERED, "cron-triggered");
        assert_eq!(EVENT_THEME_CHANGED, "theme_changed");
        assert_eq!(EVENT_LOG, "log");
        assert_eq!(EVENT_TOAST, "toast");
        assert_eq!(EVENT_OPEN_TAB, "open_tab");
    }

    #[test]
    fn test_log_event_serialization() {
        let event = LogEvent::new("info", "Test message", Some("details".to_string()));
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"level\":\"info\""));
        assert!(json.contains("\"message\":\"Test message\""));
        assert!(json.contains("\"details\":\"details\""));
    }

    #[test]
    fn test_toast_event_info() {
        let event = ToastEvent::info("Hello!", 5);
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"level\":\"info\""));
        assert!(json.contains("\"durationSeconds\":5"));
        assert!(json.contains("\"actions\":[]"));
    }

    #[test]
    fn test_open_tab_event() {
        let event = OpenTabEvent::new("editor", "My Tab", serde_json::json!({"key": "value"}));
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"screenId\":\"editor\""));
        assert!(json.contains("\"title\":\"My Tab\""));
        assert!(json.contains("\"autoFocus\":true"));
        assert!(json.contains("\"bringToFront\":true"));
    }

    #[test]
    fn test_toast_action_serialization() {
        let action = ToastAction {
            id: "ok".to_string(),
            label: "OK".to_string(),
        };
        let json = serde_json::to_string(&action).unwrap();
        assert!(json.contains("\"id\":\"ok\""));
        assert!(json.contains("\"label\":\"OK\""));
    }

    #[test]
    fn test_host_ready_event() {
        let event = HostReadyEvent::new(3000, "dark");
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"appVersion\""));
        assert!(json.contains("\"theme\":\"dark\""));
        assert!(json.contains("\"mcpUrl\""));
        assert!(json.contains("\"protocolVersion\":1"));
    }

    #[test]
    fn test_ui_confirm_request_event() {
        let event = UiConfirmRequestEvent {
            request_id: "req-1".to_string(),
            title: Some("Confirm".to_string()),
            message: "Are you sure?".to_string(),
            details: None,
        };
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"requestId\":\"req-1\""));
        assert!(json.contains("\"message\":\"Are you sure?\""));
    }

    #[test]
    fn test_ui_user_input_request_event() {
        let event = UiUserInputRequestEvent {
            request_id: "input-1".to_string(),
            request: serde_json::json!({"key": "name", "title": "Enter name"}),
            context: None,
        };
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"requestId\":\"input-1\""));
    }

    #[test]
    fn test_ui_nav_request_event() {
        let event = UiUserInputNavRequestEvent {
            request_id: "nav-1".to_string(),
            request: serde_json::json!({}),
            context: serde_json::json!({}),
            can_go_back: true,
            current_step: 2,
            total_steps: 5,
        };
        let json = serde_json::to_string(&event).unwrap();
        assert!(json.contains("\"canGoBack\":true"));
        assert!(json.contains("\"currentStep\":2"));
        assert!(json.contains("\"totalSteps\":5"));
    }

    #[test]
    fn test_new_event_constants() {
        assert_eq!(EVENT_HOST_READY, "host_ready");
        assert_eq!(EVENT_UI_CONFIRM_REQUEST, "ui_confirm_request");
        assert_eq!(EVENT_UI_USER_INPUT_REQUEST, "ui_user_input_request");
        assert_eq!(EVENT_UI_USER_INPUT_NAV_REQUEST, "ui_user_input_navigation_request");
    }
}
