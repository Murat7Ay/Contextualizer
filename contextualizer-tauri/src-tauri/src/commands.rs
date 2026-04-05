use serde::{Deserialize, Serialize};
use tauri::State;

use crate::error::{AppError, AppResult};
use crate::handlers::types::HandlerConfig;
use crate::state::AppState;

// ── Response types ──────────────────────────────────────────────────

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct HandlerSummary {
    pub name: String,
    pub handler_type: String,
    pub enabled: bool,
    pub mcp_enabled: bool,
    pub description: Option<String>,
}

impl From<&HandlerConfig> for HandlerSummary {
    fn from(cfg: &HandlerConfig) -> Self {
        Self {
            name: cfg.name.clone(),
            handler_type: cfg.type_name.clone(),
            enabled: cfg.enabled,
            mcp_enabled: cfg.mcp_enabled,
            description: cfg.description.clone(),
        }
    }
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct AppSettings {
    pub handlers_file_path: Option<String>,
    pub plugins_directory: Option<String>,
    pub exchange_directory: Option<String>,
    pub keyboard_shortcut: Option<KeyboardShortcut>,
    pub clipboard_wait_timeout: Option<u32>,
    pub window_activation_delay: Option<u32>,
    pub clipboard_clear_delay: Option<u32>,
    pub config_system: Option<ConfigSystemSettings>,
    pub ui_settings: Option<UiSettings>,
    pub logging_settings: Option<LoggingSettings>,
    pub mcp_settings: Option<McpSettings>,
    pub ai_skills_hub: Option<AiSkillsHubSettings>,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct KeyboardShortcut {
    pub modifier_keys: Vec<String>,
    pub key: String,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct ConfigSystemSettings {
    pub enabled: bool,
    pub config_file_path: Option<String>,
    pub secrets_file_path: Option<String>,
    pub auto_create_files: bool,
    pub file_format: Option<String>,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct UiSettings {
    pub toast_position_x: Option<f64>,
    pub toast_position_y: Option<f64>,
    pub theme: Option<String>,
    pub skipped_update_version: Option<String>,
    pub last_update_check: Option<String>,
    pub network_update_settings: Option<NetworkUpdateSettings>,
    pub initial_deployment_settings: Option<InitialDeploymentSettings>,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct NetworkUpdateSettings {
    pub enable_network_updates: bool,
    pub network_update_path: Option<String>,
    pub update_script_path: Option<String>,
    pub check_interval_hours: Option<u32>,
    pub auto_install_non_mandatory: bool,
    pub auto_install_mandatory: bool,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct InitialDeploymentSettings {
    pub enabled: bool,
    pub source_path: Option<String>,
    pub is_completed: bool,
    pub copy_exchange_handlers: bool,
    pub copy_installed_handlers: bool,
    pub copy_plugins: bool,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct LoggingSettings {
    pub enable_local_logging: bool,
    pub enable_usage_tracking: bool,
    pub local_log_path: Option<String>,
    pub usage_endpoint_url: Option<String>,
    pub minimum_log_level: Option<String>,
    #[serde(rename = "maxLogFileSizeMB")]
    pub max_log_file_size_mb: Option<u32>,
    pub max_log_file_count: Option<u32>,
    pub enable_debug_mode: bool,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct McpSettings {
    pub enabled: bool,
    pub port: Option<u16>,
    pub use_native_ui: bool,
    pub management_tools_enabled: bool,
}

#[derive(Debug, Serialize, Deserialize, Clone, Default)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillsHubSettings {
    pub sources: Vec<AiSkillsSource>,
    pub cursor_skills_path: Option<String>,
    pub copilot_skills_path: Option<String>,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillsSource {
    pub id: String,
    pub path: String,
    pub label: Option<String>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PongResponse {
    pub pong: bool,
    pub timestamp: u64,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DispatchResult {
    pub handler_name: String,
    pub status: String,
    pub output: Option<String>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ToolSummary {
    pub name: String,
    pub description: String,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PluginSummary {
    pub name: String,
    pub source: String,
    pub type_name: String,
}

// ── Commands ────────────────────────────────────────────────────────

#[tauri::command]
pub fn greet(name: &str) -> String {
    format!("Hello, {}! Welcome to Contextualizer.", name)
}

#[tauri::command]
pub fn ping() -> PongResponse {
    use std::time::{SystemTime, UNIX_EPOCH};
    PongResponse {
        pong: true,
        timestamp: SystemTime::now()
            .duration_since(UNIX_EPOCH)
            .unwrap_or_default()
            .as_millis() as u64,
    }
}

#[tauri::command]
pub async fn handlers_list(state: State<'_, AppState>) -> AppResult<Vec<HandlerSummary>> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    Ok(state.handlers.iter().map(HandlerSummary::from).collect())
}

#[tauri::command]
pub async fn handler_get(
    name: String,
    state: State<'_, AppState>,
) -> AppResult<HandlerConfig> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    state
        .find_handler(&name)
        .cloned()
        .ok_or_else(|| AppError::HandlerNotFound(name))
}

#[tauri::command]
pub async fn handler_create(
    config: HandlerConfig,
    state: State<'_, AppState>,
) -> AppResult<HandlerSummary> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    if config.name.trim().is_empty() {
        return Err(AppError::HandlerValidation("Handler name cannot be empty".to_string()));
    }
    if config.type_name.trim().is_empty() {
        return Err(AppError::HandlerValidation("Handler type cannot be empty".to_string()));
    }
    let summary = HandlerSummary::from(&config);
    state.add_handler(config).map_err(AppError::General)?;
    state.save_handlers().map_err(AppError::General)?;
    Ok(summary)
}

#[tauri::command]
pub async fn handler_delete(
    name: String,
    state: State<'_, AppState>,
) -> AppResult<HandlerSummary> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let deleted = state.delete_handler(&name).map_err(|e| AppError::HandlerNotFound(e))?;
    state.save_handlers().map_err(AppError::General)?;
    Ok(HandlerSummary::from(&deleted))
}

#[tauri::command]
pub async fn handler_toggle(
    name: String,
    enabled: bool,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let handler = state
        .find_handler_mut(&name)
        .ok_or_else(|| AppError::HandlerNotFound(name))?;
    handler.enabled = enabled;
    state.save_handlers().map_err(AppError::General)?;
    Ok(enabled)
}

#[tauri::command]
pub async fn handler_reload(state: State<'_, AppState>) -> AppResult<usize> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    state
        .load_handlers()
        .map_err(AppError::General)
}

#[tauri::command]
pub async fn get_app_settings(state: State<'_, AppState>) -> AppResult<AppSettings> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    Ok(AppSettings {
        handlers_file_path: Some(state.handlers_path.to_string_lossy().to_string()),
        keyboard_shortcut: Some(KeyboardShortcut {
            modifier_keys: vec!["CommandOrControl".to_string(), "Alt".to_string()],
            key: state.settings.shortcut.split('+').last().unwrap_or("W").to_string(),
        }),
        mcp_settings: Some(McpSettings {
            enabled: true,
            port: Some(state.settings.mcp_port),
            use_native_ui: false,
            management_tools_enabled: true,
        }),
        logging_settings: Some(LoggingSettings {
            enable_local_logging: true,
            minimum_log_level: Some(state.settings.log_level.clone()),
            ..Default::default()
        }),
        ..Default::default()
    })
}

#[tauri::command]
pub async fn save_app_settings(
    settings: AppSettings,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    if let Some(ks) = &settings.keyboard_shortcut {
        let mods = ks.modifier_keys.join("+");
        state.settings.shortcut = if mods.is_empty() {
            ks.key.clone()
        } else {
            format!("{}+{}", mods, ks.key)
        };
    }
    if let Some(mcp) = &settings.mcp_settings {
        if let Some(port) = mcp.port {
            state.settings.mcp_port = port;
        }
    }
    if let Some(logging) = &settings.logging_settings {
        if let Some(level) = &logging.minimum_log_level {
            state.settings.log_level = level.clone();
        }
    }
    Ok(true)
}

#[tauri::command]
pub async fn dispatch_clipboard(
    input: String,
    state: State<'_, AppState>,
) -> AppResult<DispatchResult> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;

    for handler in &state.handlers {
        if !handler.enabled {
            continue;
        }
        if handler.type_name == "regex" {
            if let Some(pattern) = &handler.regex {
                if let Ok(re) = regex::Regex::new(pattern) {
                    if re.is_match(&input) {
                        return Ok(DispatchResult {
                            handler_name: handler.name.clone(),
                            status: "processed".to_string(),
                            output: Some(format!("Matched by {}", handler.name)),
                        });
                    }
                }
            }
        }
    }

    Ok(DispatchResult {
        handler_name: String::new(),
        status: "not_processed".to_string(),
        output: None,
    })
}

#[tauri::command]
pub async fn mcp_tools_list(state: State<'_, AppState>) -> AppResult<Vec<ToolSummary>> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    Ok(state
        .mcp_registry
        .list_tools()
        .iter()
        .map(|t| ToolSummary {
            name: t.name.clone(),
            description: t.description.clone(),
        })
        .collect())
}

#[tauri::command]
pub async fn plugin_list(state: State<'_, AppState>) -> AppResult<Vec<PluginSummary>> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    Ok(state
        .plugin_manager
        .list_plugins()
        .iter()
        .map(|p| PluginSummary {
            name: p.name.clone(),
            source: format!("{:?}", p.source),
            type_name: p.type_name.clone(),
        })
        .collect())
}

#[tauri::command]
pub async fn config_get(
    key: String,
    state: State<'_, AppState>,
) -> AppResult<Option<String>> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    Ok(state.config_values.get(&key).cloned())
}

#[tauri::command]
pub async fn config_set(
    key: String,
    value: String,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    state.config_values.insert(key, value);
    Ok(true)
}

// ── Cron commands ───────────────────────────────────────────────────

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct CronJobSummary {
    pub job_id: String,
    pub handler_name: String,
    pub cron_expression: String,
    pub enabled: bool,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct CronListResponse {
    pub is_running: bool,
    pub jobs: Vec<CronJobSummary>,
}

#[tauri::command]
pub async fn cron_list(state: State<'_, AppState>) -> AppResult<CronListResponse> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let jobs = state
        .cron_scheduler
        .list_jobs()
        .iter()
        .map(|j| CronJobSummary {
            job_id: j.id.clone(),
            handler_name: j.handler_name.clone(),
            cron_expression: j.cron_expression.clone(),
            enabled: j.enabled,
        })
        .collect();
    Ok(CronListResponse {
        is_running: state.cron_scheduler.is_running(),
        jobs,
    })
}

#[tauri::command]
pub async fn cron_set_enabled(
    job_id: String,
    enabled: bool,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let job = state
        .cron_scheduler
        .get_job_mut(&job_id)
        .ok_or_else(|| AppError::General(format!("Cron job '{}' not found", job_id)))?;
    job.enabled = enabled;
    Ok(enabled)
}

#[tauri::command]
pub async fn cron_trigger(
    job_id: String,
    state: State<'_, AppState>,
) -> AppResult<String> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let job = state
        .cron_scheduler
        .get_job_mut(&job_id)
        .ok_or_else(|| AppError::General(format!("Cron job '{}' not found", job_id)))?;
    job.mark_run();
    Ok(job.handler_name.clone())
}

#[tauri::command]
pub async fn cron_update(
    job_id: String,
    cron_expression: String,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let job = state
        .cron_scheduler
        .get_job_mut(&job_id)
        .ok_or_else(|| AppError::General(format!("Cron job '{}' not found", job_id)))?;
    job.update_expression(&cron_expression);
    Ok(true)
}

// ── Handler update/mcp/manual ───────────────────────────────────────

#[tauri::command]
pub async fn handler_update(
    handler_name: String,
    updates: serde_json::Value,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let handler = state
        .find_handler_mut(&handler_name)
        .ok_or_else(|| AppError::HandlerNotFound(handler_name.clone()))?;

    if let Some(desc) = updates.get("description").and_then(|v| v.as_str()) {
        handler.description = Some(desc.to_string());
    }
    if let Some(enabled) = updates.get("enabled").and_then(|v| v.as_bool()) {
        handler.enabled = enabled;
    }
    if let Some(mcp) = updates.get("mcpEnabled").and_then(|v| v.as_bool()) {
        handler.mcp_enabled = mcp;
    }
    if let Some(regex) = updates.get("regex").and_then(|v| v.as_str()) {
        handler.regex = Some(regex.to_string());
    }
    if let Some(output) = updates.get("outputFormat").and_then(|v| v.as_str()) {
        handler.output_format = Some(output.to_string());
    }
    if let Some(title) = updates.get("title").and_then(|v| v.as_str()) {
        handler.title = Some(title.to_string());
    }

    state.save_handlers().map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn handler_set_mcp(
    name: String,
    mcp_enabled: bool,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let handler = state
        .find_handler_mut(&name)
        .ok_or_else(|| AppError::HandlerNotFound(name))?;
    handler.mcp_enabled = mcp_enabled;
    state.save_handlers().map_err(AppError::General)?;
    Ok(mcp_enabled)
}

#[tauri::command]
pub async fn manual_handler_execute(
    name: String,
    state: State<'_, AppState>,
) -> AppResult<DispatchResult> {
    let state = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let handler = state
        .find_handler(&name)
        .ok_or_else(|| AppError::HandlerNotFound(name.clone()))?;

    if handler.type_name != "manual" {
        return Err(AppError::HandlerValidation(format!(
            "Handler '{}' is not a manual handler (type: {})",
            name, handler.type_name
        )));
    }

    Ok(DispatchResult {
        handler_name: name,
        status: "executed".to_string(),
        output: handler.output_format.clone(),
    })
}

// ── UI dialogs ──────────────────────────────────────────────────────

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ConfirmResult {
    pub confirmed: bool,
}

#[tauri::command]
pub async fn ui_confirm(
    title: String,
    message: String,
    app: tauri::AppHandle,
) -> AppResult<ConfirmResult> {
    use tauri_plugin_dialog::{DialogExt, MessageDialogButtons};
    let confirmed = app
        .dialog()
        .message(&message)
        .title(&title)
        .buttons(MessageDialogButtons::OkCancelCustom("OK".into(), "Cancel".into()))
        .blocking_show();
    Ok(ConfirmResult { confirmed })
}

#[tauri::command]
pub async fn ui_notify(
    message: String,
    title: Option<String>,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use tauri_plugin_notification::NotificationExt;
    let mut builder = app.notification().builder();
    if let Some(t) = title {
        builder = builder.title(t);
    }
    builder.body(message).show().map_err(|e| AppError::General(e.to_string()))?;
    Ok(true)
}

#[tauri::command]
pub async fn open_external(url: String) -> AppResult<bool> {
    open::that(&url).map_err(|e| AppError::General(format!("Failed to open URL: {}", e)))?;
    Ok(true)
}

// ── Exchange (placeholder) ──────────────────────────────────────────

#[derive(Debug, Serialize, Deserialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct ExchangePackage {
    pub handler_id: String,
    pub name: String,
    pub description: Option<String>,
    pub version: Option<String>,
    pub tags: Vec<String>,
}

#[tauri::command]
pub async fn exchange_list(
    search_term: Option<String>,
    tags: Option<Vec<String>>,
) -> AppResult<Vec<ExchangePackage>> {
    let _ = (search_term, tags);
    Ok(vec![])
}

#[tauri::command]
pub async fn exchange_install(handler_id: String) -> AppResult<bool> {
    let _ = handler_id;
    Err(AppError::General("Exchange not yet implemented".to_string()))
}

#[tauri::command]
pub async fn exchange_remove(handler_id: String) -> AppResult<bool> {
    let _ = handler_id;
    Err(AppError::General("Exchange not yet implemented".to_string()))
}

// ── Theme ───────────────────────────────────────────────────────────

#[tauri::command]
pub async fn set_theme(
    theme: String,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use tauri::Emitter;
    app.emit("theme_changed", &theme)
        .map_err(|e| AppError::General(e.to_string()))?;
    Ok(true)
}

// ── Log / Toast / Tab ───────────────────────────────────────────────

#[tauri::command]
pub async fn emit_log(
    level: String,
    message: String,
    details: Option<String>,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, LogEvent, EVENT_LOG};
    let event = LogEvent::new(&level, &message, details);
    emit_event(&app, EVENT_LOG, &event).map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn emit_toast(
    level: String,
    message: String,
    title: Option<String>,
    duration_seconds: Option<u32>,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, ToastEvent, EVENT_TOAST};
    let mut event = ToastEvent::info(&message, duration_seconds.unwrap_or(5));
    event.level = level;
    event.title = title;
    emit_event(&app, EVENT_TOAST, &event).map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn emit_open_tab(
    screen_id: String,
    title: String,
    context: serde_json::Value,
    auto_focus: Option<bool>,
    bring_to_front: Option<bool>,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, OpenTabEvent, EVENT_OPEN_TAB};
    let mut event = OpenTabEvent::new(&screen_id, &title, context);
    event.auto_focus = auto_focus.unwrap_or(true);
    event.bring_to_front = bring_to_front.unwrap_or(true);
    emit_event(&app, EVENT_OPEN_TAB, &event).map_err(AppError::General)?;
    Ok(true)
}

// ── File / Folder dialogs ───────────────────────────────────────────

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct FileDialogResult {
    pub cancelled: bool,
    pub path: Option<String>,
    pub paths: Option<Vec<String>>,
}

#[tauri::command]
pub async fn open_file_dialog(
    title: Option<String>,
    filters: Option<Vec<String>>,
    multiple: Option<bool>,
    app: tauri::AppHandle,
) -> AppResult<FileDialogResult> {
    use tauri_plugin_dialog::DialogExt;
    let mut builder = app.dialog().file();
    if let Some(t) = title {
        builder = builder.set_title(t);
    }
    if let Some(ref filter_list) = filters {
        let extensions: Vec<&str> = filter_list.iter().map(|s| s.as_str()).collect();
        builder = builder.add_filter("Files", &extensions);
    }
    if multiple.unwrap_or(false) {
        let result = builder.blocking_pick_files();
        match result {
            Some(paths) => Ok(FileDialogResult {
                cancelled: false,
                path: None,
                paths: Some(paths.iter().map(|p| p.to_string()).collect()),
            }),
            None => Ok(FileDialogResult {
                cancelled: true,
                path: None,
                paths: None,
            }),
        }
    } else {
        let result = builder.blocking_pick_file();
        match result {
            Some(path) => Ok(FileDialogResult {
                cancelled: false,
                path: Some(path.to_string()),
                paths: None,
            }),
            None => Ok(FileDialogResult {
                cancelled: true,
                path: None,
                paths: None,
            }),
        }
    }
}

#[tauri::command]
pub async fn open_folder_dialog(
    title: Option<String>,
    app: tauri::AppHandle,
) -> AppResult<FileDialogResult> {
    use tauri_plugin_dialog::DialogExt;
    let mut builder = app.dialog().file();
    if let Some(t) = title {
        builder = builder.set_title(t);
    }
    let result = builder.blocking_pick_folder();
    match result {
        Some(path) => Ok(FileDialogResult {
            cancelled: false,
            path: Some(path.to_string()),
            paths: None,
        }),
        None => Ok(FileDialogResult {
            cancelled: true,
            path: None,
            paths: None,
        }),
    }
}

// ── Prompt system (Phase 2) ─────────────────────────────────────────

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UserInputResponse {
    pub request_id: String,
    pub cancelled: bool,
    pub value: Option<String>,
    pub selected_values: Option<Vec<String>>,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct NavigationInputResponse {
    pub request_id: String,
    pub action: String,
    pub value: Option<String>,
    pub selected_values: Option<Vec<String>>,
}

#[tauri::command]
pub async fn request_confirm(
    request_id: String,
    title: Option<String>,
    message: String,
    details: Option<serde_json::Value>,
    app: tauri::AppHandle,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    use crate::events::{emit_event, UiConfirmRequestEvent, EVENT_UI_CONFIRM_REQUEST};
    let (tx, rx) = tokio::sync::oneshot::channel::<bool>();
    {
        let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
        s.pending_confirms.insert(request_id.clone(), tx);
    }
    let event = UiConfirmRequestEvent {
        request_id: request_id.clone(),
        title,
        message,
        details,
    };
    emit_event(&app, EVENT_UI_CONFIRM_REQUEST, &event).map_err(AppError::General)?;
    match tokio::time::timeout(std::time::Duration::from_secs(300), rx).await {
        Ok(Ok(confirmed)) => Ok(confirmed),
        _ => {
            let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
            s.pending_confirms.remove(&request_id);
            Ok(false)
        }
    }
}

#[tauri::command]
pub async fn submit_confirm_response(
    request_id: String,
    confirmed: bool,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    if let Some(tx) = s.pending_confirms.remove(&request_id) {
        let _ = tx.send(confirmed);
        Ok(true)
    } else {
        Ok(false)
    }
}

#[tauri::command]
pub async fn request_user_input(
    request_id: String,
    request: serde_json::Value,
    context: Option<serde_json::Value>,
    app: tauri::AppHandle,
    state: State<'_, AppState>,
) -> AppResult<UserInputResponse> {
    use crate::events::{emit_event, UiUserInputRequestEvent, EVENT_UI_USER_INPUT_REQUEST};
    let (tx, rx) = tokio::sync::oneshot::channel::<UserInputResponse>();
    {
        let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
        s.pending_inputs.insert(request_id.clone(), tx);
    }
    let event = UiUserInputRequestEvent {
        request_id: request_id.clone(),
        request,
        context,
    };
    emit_event(&app, EVENT_UI_USER_INPUT_REQUEST, &event).map_err(AppError::General)?;
    match tokio::time::timeout(std::time::Duration::from_secs(300), rx).await {
        Ok(Ok(response)) => Ok(response),
        _ => {
            let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
            s.pending_inputs.remove(&request_id);
            Ok(UserInputResponse {
                request_id,
                cancelled: true,
                value: None,
                selected_values: None,
            })
        }
    }
}

#[tauri::command]
pub async fn submit_user_input_response(
    response: UserInputResponse,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    if let Some(tx) = s.pending_inputs.remove(&response.request_id) {
        let _ = tx.send(response);
        Ok(true)
    } else {
        Ok(false)
    }
}

#[tauri::command]
pub async fn request_user_input_navigation(
    request_id: String,
    request: serde_json::Value,
    context: serde_json::Value,
    can_go_back: bool,
    current_step: u32,
    total_steps: u32,
    app: tauri::AppHandle,
    state: State<'_, AppState>,
) -> AppResult<NavigationInputResponse> {
    use crate::events::{emit_event, UiUserInputNavRequestEvent, EVENT_UI_USER_INPUT_NAV_REQUEST};
    let (tx, rx) = tokio::sync::oneshot::channel::<NavigationInputResponse>();
    {
        let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
        s.pending_nav_inputs.insert(request_id.clone(), tx);
    }
    let event = UiUserInputNavRequestEvent {
        request_id: request_id.clone(),
        request,
        context,
        can_go_back,
        current_step,
        total_steps,
    };
    emit_event(&app, EVENT_UI_USER_INPUT_NAV_REQUEST, &event).map_err(AppError::General)?;
    match tokio::time::timeout(std::time::Duration::from_secs(300), rx).await {
        Ok(Ok(response)) => Ok(response),
        _ => {
            let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
            s.pending_nav_inputs.remove(&request_id);
            Ok(NavigationInputResponse {
                request_id,
                action: "cancel".to_string(),
                value: None,
                selected_values: None,
            })
        }
    }
}

#[tauri::command]
pub async fn submit_navigation_response(
    response: NavigationInputResponse,
    state: State<'_, AppState>,
) -> AppResult<bool> {
    let mut s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    if let Some(tx) = s.pending_nav_inputs.remove(&response.request_id) {
        let _ = tx.send(response);
        Ok(true)
    } else {
        Ok(false)
    }
}

// ── Tab/Toast lifecycle (Phase 3) ───────────────────────────────────

#[tauri::command]
pub async fn tab_action_execute(
    tab_id: String,
    action_id: String,
    context: Option<serde_json::Value>,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, LogEvent, EVENT_LOG};
    let event = LogEvent::new(
        "debug",
        &format!("Tab action executed: tab={}, action={}", tab_id, action_id),
        context.map(|c| c.to_string()),
    );
    emit_event(&app, EVENT_LOG, &event).map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn tab_closed(
    tab_id: String,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, LogEvent, EVENT_LOG};
    let event = LogEvent::new("debug", &format!("Tab closed: {}", tab_id), None);
    emit_event(&app, EVENT_LOG, &event).map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn toast_action_execute(
    toast_id: String,
    action_id: String,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, LogEvent, EVENT_LOG};
    let event = LogEvent::new(
        "debug",
        &format!("Toast action executed: toast={}, action={}", toast_id, action_id),
        None,
    );
    emit_event(&app, EVENT_LOG, &event).map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn toast_closed(
    toast_id: String,
    app: tauri::AppHandle,
) -> AppResult<bool> {
    use crate::events::{emit_event, LogEvent, EVENT_LOG};
    let event = LogEvent::new("debug", &format!("Toast closed: {}", toast_id), None);
    emit_event(&app, EVENT_LOG, &event).map_err(AppError::General)?;
    Ok(true)
}

// ── Logging / Usage / Log Clear (Phase 4) ───────────────────────────

#[tauri::command]
pub async fn logging_test(app: tauri::AppHandle) -> AppResult<bool> {
    use crate::events::{emit_event, LogEvent, ToastEvent, EVENT_LOG, EVENT_TOAST};
    for level in &["debug", "info", "warning", "error", "critical"] {
        let event = LogEvent::new(level, &format!("Test {} log entry", level), None);
        emit_event(&app, EVENT_LOG, &event).map_err(AppError::General)?;
    }
    let toast = ToastEvent::info("Logging test completed - check log entries", 5);
    emit_event(&app, EVENT_TOAST, &toast).map_err(AppError::General)?;
    Ok(true)
}

#[tauri::command]
pub async fn usage_test(app: tauri::AppHandle) -> AppResult<bool> {
    use crate::events::{emit_event, ToastEvent, EVENT_TOAST};
    let toast = ToastEvent::info("Usage tracking test completed (placeholder)", 5);
    emit_event(&app, EVENT_TOAST, &toast).map_err(AppError::General)?;
    Ok(true)
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct LogClearResult {
    pub deleted_count: usize,
}

#[tauri::command]
pub async fn log_clear(
    path: Option<String>,
    state: State<'_, AppState>,
) -> AppResult<LogClearResult> {
    let log_path = match path {
        Some(p) => std::path::PathBuf::from(p),
        None => {
            let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
            s.handlers_path.parent().unwrap_or(std::path::Path::new(".")).to_path_buf()
        }
    };
    let mut deleted = 0;
    if log_path.exists() && log_path.is_dir() {
        if let Ok(entries) = std::fs::read_dir(&log_path) {
            for entry in entries.flatten() {
                let p = entry.path();
                if p.extension().and_then(|e| e.to_str()) == Some("log") {
                    if std::fs::remove_file(&p).is_ok() {
                        deleted += 1;
                    }
                }
            }
        }
    }
    Ok(LogClearResult { deleted_count: deleted })
}

// ── Exchange full (Phase 5) ─────────────────────────────────────────

#[tauri::command]
pub async fn exchange_tags(state: State<'_, AppState>) -> AppResult<Vec<String>> {
    let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let exchange_dir = s.settings.handlers_path
        .replace("handlers.json", "exchange");
    let dir = std::path::Path::new(&exchange_dir);
    if !dir.exists() {
        return Ok(vec![]);
    }
    let mut tags = std::collections::HashSet::new();
    if let Ok(entries) = std::fs::read_dir(dir) {
        for entry in entries.flatten() {
            if entry.path().extension().and_then(|e| e.to_str()) == Some("json") {
                if let Ok(content) = std::fs::read_to_string(entry.path()) {
                    if let Ok(pkg) = serde_json::from_str::<ExchangePackage>(&content) {
                        for tag in &pkg.tags {
                            tags.insert(tag.clone());
                        }
                    }
                }
            }
        }
    }
    let mut result: Vec<String> = tags.into_iter().collect();
    result.sort();
    Ok(result)
}

#[tauri::command]
pub async fn exchange_details(
    handler_id: String,
    state: State<'_, AppState>,
) -> AppResult<Option<ExchangePackage>> {
    let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let exchange_dir = s.settings.handlers_path
        .replace("handlers.json", "exchange");
    let dir = std::path::Path::new(&exchange_dir);
    if !dir.exists() {
        return Ok(None);
    }
    if let Ok(entries) = std::fs::read_dir(dir) {
        for entry in entries.flatten() {
            if entry.path().extension().and_then(|e| e.to_str()) == Some("json") {
                if let Ok(content) = std::fs::read_to_string(entry.path()) {
                    if let Ok(pkg) = serde_json::from_str::<ExchangePackage>(&content) {
                        if pkg.handler_id == handler_id {
                            return Ok(Some(pkg));
                        }
                    }
                }
            }
        }
    }
    Ok(None)
}

#[tauri::command]
pub async fn exchange_update(handler_id: String) -> AppResult<bool> {
    let _ = handler_id;
    Err(AppError::General("Exchange update not yet implemented".to_string()))
}

#[tauri::command]
pub async fn exchange_publish(package: ExchangePackage) -> AppResult<bool> {
    let _ = package;
    Err(AppError::General("Exchange publish not yet implemented".to_string()))
}

// ── AI Skills Hub (Phase 6) ─────────────────────────────────────────

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillsHubListResult {
    pub cursor_skills_root: Option<String>,
    pub copilot_skills_root: Option<String>,
    pub sources: Vec<AiSkillsSource>,
    pub skills: Vec<AiSkillRow>,
    pub global_only_skills: Vec<GlobalOnlySkill>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillRow {
    pub name: String,
    pub source_id: String,
    pub source_path: String,
    pub cursor_state: String,
    pub copilot_state: String,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct GlobalOnlySkill {
    pub name: String,
    pub target: String,
    pub path: String,
}

#[tauri::command]
pub async fn ai_skills_hub_list(
    state: State<'_, AppState>,
) -> AppResult<AiSkillsHubListResult> {
    let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let settings = &s.settings;

    let cursor_root = dirs::home_dir().map(|h| h.join(".cursor").join("skills"));
    let copilot_root = dirs::home_dir().map(|h| h.join(".copilot").join("skills"));

    let mut skills = Vec::new();
    let mut global_only = Vec::new();

    let sources: Vec<AiSkillsSource> = s.config_values
        .get("ai_skills_hub_sources")
        .and_then(|v| serde_json::from_str(v).ok())
        .unwrap_or_default();

    for source in &sources {
        let source_dir = std::path::Path::new(&source.path);
        if !source_dir.exists() { continue; }
        if let Ok(entries) = std::fs::read_dir(source_dir) {
            for entry in entries.flatten() {
                if entry.path().is_dir() {
                    let name = entry.file_name().to_string_lossy().to_string();
                    let cursor_state = match &cursor_root {
                        Some(r) if r.join(&name).exists() => "synced",
                        _ => "needs_deploy",
                    };
                    let copilot_state = match &copilot_root {
                        Some(r) if r.join(&name).exists() => "synced",
                        _ => "needs_deploy",
                    };
                    skills.push(AiSkillRow {
                        name,
                        source_id: source.id.clone(),
                        source_path: entry.path().to_string_lossy().to_string(),
                        cursor_state: cursor_state.to_string(),
                        copilot_state: copilot_state.to_string(),
                    });
                }
            }
        }
    }

    for (target, root) in [("cursor", &cursor_root), ("copilot", &copilot_root)] {
        if let Some(root_path) = root {
            if root_path.exists() {
                if let Ok(entries) = std::fs::read_dir(root_path) {
                    for entry in entries.flatten() {
                        if entry.path().is_dir() {
                            let name = entry.file_name().to_string_lossy().to_string();
                            if !skills.iter().any(|s| s.name == name) {
                                global_only.push(GlobalOnlySkill {
                                    name,
                                    target: target.to_string(),
                                    path: entry.path().to_string_lossy().to_string(),
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    let _ = settings;
    Ok(AiSkillsHubListResult {
        cursor_skills_root: cursor_root.map(|p| p.to_string_lossy().to_string()),
        copilot_skills_root: copilot_root.map(|p| p.to_string_lossy().to_string()),
        sources,
        skills,
        global_only_skills: global_only,
    })
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillDeployment {
    pub skill_name: String,
    pub source_id: String,
    pub targets: Vec<String>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillOpResult {
    pub ok: bool,
    pub results: Vec<AiSkillOpItem>,
}

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AiSkillOpItem {
    pub skill_name: String,
    pub ok: bool,
    pub error: Option<String>,
}

#[tauri::command]
pub async fn ai_skills_hub_deploy(
    deployments: Vec<AiSkillDeployment>,
    custom_destination_root: Option<String>,
    state: State<'_, AppState>,
) -> AppResult<AiSkillOpResult> {
    let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let sources: Vec<AiSkillsSource> = s.config_values
        .get("ai_skills_hub_sources")
        .and_then(|v| serde_json::from_str(v).ok())
        .unwrap_or_default();
    drop(s);

    let cursor_root = dirs::home_dir().map(|h| h.join(".cursor").join("skills"));
    let copilot_root = dirs::home_dir().map(|h| h.join(".copilot").join("skills"));
    let custom_root = custom_destination_root.map(std::path::PathBuf::from);

    let mut results = Vec::new();
    for dep in &deployments {
        let source = sources.iter().find(|s| s.id == dep.source_id);
        let source_path = match source {
            Some(s) => std::path::PathBuf::from(&s.path).join(&dep.skill_name),
            None => {
                results.push(AiSkillOpItem {
                    skill_name: dep.skill_name.clone(),
                    ok: false,
                    error: Some(format!("Source '{}' not found", dep.source_id)),
                });
                continue;
            }
        };
        if !source_path.exists() {
            results.push(AiSkillOpItem {
                skill_name: dep.skill_name.clone(),
                ok: false,
                error: Some("Source skill directory not found".to_string()),
            });
            continue;
        }
        let mut ok = true;
        let mut errors = Vec::new();
        for target in &dep.targets {
            let dest = match target.as_str() {
                "cursor" => cursor_root.as_ref().map(|r| r.join(&dep.skill_name)),
                "copilot" => copilot_root.as_ref().map(|r| r.join(&dep.skill_name)),
                "custom" => custom_root.as_ref().map(|r| r.join(&dep.skill_name)),
                _ => None,
            };
            if let Some(dest_path) = dest {
                if let Err(e) = copy_dir_recursive(&source_path, &dest_path) {
                    ok = false;
                    errors.push(format!("{}: {}", target, e));
                }
            }
        }
        results.push(AiSkillOpItem {
            skill_name: dep.skill_name.clone(),
            ok,
            error: if errors.is_empty() { None } else { Some(errors.join("; ")) },
        });
    }
    Ok(AiSkillOpResult {
        ok: results.iter().all(|r| r.ok),
        results,
    })
}

#[tauri::command]
pub async fn ai_skills_hub_remove(
    skill_names: Vec<String>,
    targets: Vec<String>,
) -> AppResult<AiSkillOpResult> {
    let cursor_root = dirs::home_dir().map(|h| h.join(".cursor").join("skills"));
    let copilot_root = dirs::home_dir().map(|h| h.join(".copilot").join("skills"));
    let mut results = Vec::new();
    for name in &skill_names {
        let mut ok = true;
        let mut errors = Vec::new();
        for target in &targets {
            let root = match target.as_str() {
                "cursor" => &cursor_root,
                "copilot" => &copilot_root,
                _ => &None,
            };
            if let Some(root_path) = root {
                let skill_path = root_path.join(name);
                if skill_path.exists() {
                    if let Err(e) = std::fs::remove_dir_all(&skill_path) {
                        ok = false;
                        errors.push(format!("{}: {}", target, e));
                    }
                }
            }
        }
        results.push(AiSkillOpItem {
            skill_name: name.clone(),
            ok,
            error: if errors.is_empty() { None } else { Some(errors.join("; ")) },
        });
    }
    Ok(AiSkillOpResult {
        ok: results.iter().all(|r| r.ok),
        results,
    })
}

#[tauri::command]
pub async fn ai_skills_hub_pull(
    skill_names: Vec<String>,
    from_target: String,
    to_source_id: String,
    state: State<'_, AppState>,
) -> AppResult<AiSkillOpResult> {
    let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let sources: Vec<AiSkillsSource> = s.config_values
        .get("ai_skills_hub_sources")
        .and_then(|v| serde_json::from_str(v).ok())
        .unwrap_or_default();
    drop(s);

    let from_root = match from_target.as_str() {
        "cursor" => dirs::home_dir().map(|h| h.join(".cursor").join("skills")),
        "copilot" => dirs::home_dir().map(|h| h.join(".copilot").join("skills")),
        _ => None,
    };
    let to_source = sources.iter().find(|s| s.id == to_source_id);

    let mut results = Vec::new();
    for name in &skill_names {
        let from_path = from_root.as_ref().map(|r| r.join(name));
        let to_path = to_source.map(|s| std::path::PathBuf::from(&s.path).join(name));
        match (from_path, to_path) {
            (Some(from), Some(to)) if from.exists() => {
                match copy_dir_recursive(&from, &to) {
                    Ok(_) => results.push(AiSkillOpItem {
                        skill_name: name.clone(), ok: true, error: None,
                    }),
                    Err(e) => results.push(AiSkillOpItem {
                        skill_name: name.clone(), ok: false, error: Some(e),
                    }),
                }
            }
            _ => results.push(AiSkillOpItem {
                skill_name: name.clone(),
                ok: false,
                error: Some("Source or destination not found".to_string()),
            }),
        }
    }
    Ok(AiSkillOpResult {
        ok: results.iter().all(|r| r.ok),
        results,
    })
}

fn copy_dir_recursive(src: &std::path::Path, dst: &std::path::Path) -> Result<(), String> {
    std::fs::create_dir_all(dst).map_err(|e| e.to_string())?;
    for entry in std::fs::read_dir(src).map_err(|e| e.to_string())? {
        let entry = entry.map_err(|e| e.to_string())?;
        let src_path = entry.path();
        let dst_path = dst.join(entry.file_name());
        if src_path.is_dir() {
            copy_dir_recursive(&src_path, &dst_path)?;
        } else {
            std::fs::copy(&src_path, &dst_path).map_err(|e| e.to_string())?;
        }
    }
    Ok(())
}

// ── Plugin list WPF-compatible shape (Phase 7) ─────────────────────

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PluginsListResponse {
    pub handler_types: Vec<String>,
    pub actions: Vec<String>,
    pub validators: Vec<String>,
    pub context_providers: Vec<String>,
}

#[tauri::command]
pub async fn plugins_list_full(state: State<'_, AppState>) -> AppResult<PluginsListResponse> {
    let s = state.lock().map_err(|e| AppError::General(e.to_string()))?;
    let handler_types: Vec<String> = s.plugin_manager
        .list_plugins()
        .iter()
        .map(|p| p.type_name.clone())
        .collect();
    Ok(PluginsListResponse {
        handler_types,
        actions: vec![],
        validators: vec![],
        context_providers: vec![],
    })
}

// ── Tests ───────────────────────────────────────────────────────────

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_greet_returns_message() {
        let result = greet("Murat");
        assert!(result.contains("Murat"));
        assert!(result.contains("Contextualizer"));
    }

    #[test]
    fn test_ping_returns_pong() {
        let result = ping();
        assert!(result.pong);
        assert!(result.timestamp > 0);
    }

    #[test]
    fn test_handler_summary_from_config() {
        let config = HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            enabled: true,
            mcp_enabled: true,
            description: Some("Test handler".to_string()),
            ..Default::default()
        };
        let summary = HandlerSummary::from(&config);
        assert_eq!(summary.name, "test");
        assert_eq!(summary.handler_type, "regex");
        assert!(summary.enabled);
        assert!(summary.mcp_enabled);
    }

    #[test]
    fn test_handler_summary_serialization_camel_case() {
        let summary = HandlerSummary {
            name: "test".to_string(),
            handler_type: "regex".to_string(),
            enabled: true,
            mcp_enabled: false,
            description: Some("Test handler".to_string()),
        };
        let json = serde_json::to_string(&summary).unwrap();
        assert!(json.contains("\"handlerType\""));
        assert!(json.contains("\"mcpEnabled\""));
    }

    #[test]
    fn test_app_settings_default() {
        let settings = AppSettings::default();
        assert!(settings.handlers_file_path.is_none());
        assert!(settings.mcp_settings.is_none());
    }

    #[test]
    fn test_app_settings_roundtrip() {
        let original = AppSettings {
            handlers_file_path: Some("C:\\handlers.json".to_string()),
            plugins_directory: Some("C:\\plugins".to_string()),
            keyboard_shortcut: Some(KeyboardShortcut {
                modifier_keys: vec!["Ctrl".to_string(), "Alt".to_string()],
                key: "W".to_string(),
            }),
            mcp_settings: Some(McpSettings {
                enabled: true,
                port: Some(3000),
                use_native_ui: false,
                management_tools_enabled: true,
            }),
            ui_settings: Some(UiSettings {
                theme: Some("dark".to_string()),
                ..Default::default()
            }),
            ..Default::default()
        };
        let json = serde_json::to_string(&original).unwrap();
        let parsed: AppSettings = serde_json::from_str(&json).unwrap();
        assert_eq!(original.handlers_file_path, parsed.handlers_file_path);
        assert_eq!(parsed.mcp_settings.unwrap().port, Some(3000));
    }

    #[test]
    fn test_app_settings_full_serialization() {
        let settings = AppSettings {
            handlers_file_path: Some("handlers.json".to_string()),
            logging_settings: Some(LoggingSettings {
                enable_local_logging: true,
                minimum_log_level: Some("Info".to_string()),
                max_log_file_size_mb: Some(10),
                ..Default::default()
            }),
            ai_skills_hub: Some(AiSkillsHubSettings {
                sources: vec![AiSkillsSource {
                    id: "default".to_string(),
                    path: "C:\\skills".to_string(),
                    label: Some("Default".to_string()),
                }],
                ..Default::default()
            }),
            ..Default::default()
        };
        let json = serde_json::to_string(&settings).unwrap();
        assert!(json.contains("\"handlersFilePath\""));
        assert!(json.contains("\"loggingSettings\""));
        assert!(json.contains("\"enableLocalLogging\""));
        assert!(json.contains("\"aiSkillsHub\""));
        assert!(json.contains("\"maxLogFileSizeMB\""));
    }

    #[test]
    fn test_dispatch_result_serialization() {
        let result = DispatchResult {
            handler_name: "test".to_string(),
            status: "processed".to_string(),
            output: Some("matched".to_string()),
        };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"handlerName\""));
        assert!(json.contains("\"status\""));
    }

    #[test]
    fn test_cron_job_summary_serialization() {
        let summary = CronJobSummary {
            job_id: "j1".to_string(),
            handler_name: "handler1".to_string(),
            cron_expression: "every 30s".to_string(),
            enabled: true,
        };
        let json = serde_json::to_string(&summary).unwrap();
        assert!(json.contains("\"jobId\""));
        assert!(json.contains("\"handlerName\""));
        assert!(json.contains("\"cronExpression\""));
    }

    #[test]
    fn test_cron_list_response_serialization() {
        let resp = CronListResponse {
            is_running: true,
            jobs: vec![CronJobSummary {
                job_id: "j1".to_string(),
                handler_name: "h1".to_string(),
                cron_expression: "every 5m".to_string(),
                enabled: false,
            }],
        };
        let json = serde_json::to_string(&resp).unwrap();
        assert!(json.contains("\"isRunning\":true"));
        assert!(json.contains("\"jobs\""));
    }

    #[test]
    fn test_confirm_result_serialization() {
        let result = ConfirmResult { confirmed: true };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"confirmed\":true"));
    }

    #[test]
    fn test_exchange_package_serialization() {
        let pkg = ExchangePackage {
            handler_id: "pkg-1".to_string(),
            name: "Test Package".to_string(),
            description: Some("A test".to_string()),
            version: Some("1.0.0".to_string()),
            tags: vec!["test".to_string(), "demo".to_string()],
        };
        let json = serde_json::to_string(&pkg).unwrap();
        assert!(json.contains("\"handlerId\""));
        assert!(json.contains("\"tags\""));
    }

    #[test]
    fn test_exchange_package_roundtrip() {
        let pkg = ExchangePackage {
            handler_id: "pkg-1".to_string(),
            name: "Test".to_string(),
            description: None,
            version: None,
            tags: vec![],
        };
        let json = serde_json::to_string(&pkg).unwrap();
        let parsed: ExchangePackage = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed.handler_id, "pkg-1");
        assert!(parsed.description.is_none());
    }

    #[test]
    fn test_file_dialog_result_serialization() {
        let result = FileDialogResult {
            cancelled: false,
            path: Some("C:\\test.txt".to_string()),
            paths: None,
        };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"cancelled\":false"));
        assert!(json.contains("\"path\":\"C:\\\\test.txt\""));
    }

    #[test]
    fn test_file_dialog_result_cancelled() {
        let result = FileDialogResult {
            cancelled: true,
            path: None,
            paths: None,
        };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"cancelled\":true"));
    }

    #[test]
    fn test_user_input_response_roundtrip() {
        let resp = UserInputResponse {
            request_id: "req-1".to_string(),
            cancelled: false,
            value: Some("hello".to_string()),
            selected_values: Some(vec!["a".to_string(), "b".to_string()]),
        };
        let json = serde_json::to_string(&resp).unwrap();
        let parsed: UserInputResponse = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed.request_id, "req-1");
        assert!(!parsed.cancelled);
        assert_eq!(parsed.value.unwrap(), "hello");
    }

    #[test]
    fn test_navigation_input_response_roundtrip() {
        let resp = NavigationInputResponse {
            request_id: "nav-1".to_string(),
            action: "next".to_string(),
            value: Some("step1".to_string()),
            selected_values: None,
        };
        let json = serde_json::to_string(&resp).unwrap();
        let parsed: NavigationInputResponse = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed.action, "next");
    }

    #[test]
    fn test_log_clear_result_serialization() {
        let result = LogClearResult { deleted_count: 3 };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"deletedCount\":3"));
    }

    #[test]
    fn test_ai_skills_hub_list_result_serialization() {
        let result = AiSkillsHubListResult {
            cursor_skills_root: Some("C:\\skills".to_string()),
            copilot_skills_root: None,
            sources: vec![],
            skills: vec![AiSkillRow {
                name: "test-skill".to_string(),
                source_id: "default".to_string(),
                source_path: "C:\\src\\test-skill".to_string(),
                cursor_state: "synced".to_string(),
                copilot_state: "needs_deploy".to_string(),
            }],
            global_only_skills: vec![],
        };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"cursorSkillsRoot\""));
        assert!(json.contains("\"cursorState\":\"synced\""));
    }

    #[test]
    fn test_ai_skill_op_result_serialization() {
        let result = AiSkillOpResult {
            ok: true,
            results: vec![AiSkillOpItem {
                skill_name: "skill1".to_string(),
                ok: true,
                error: None,
            }],
        };
        let json = serde_json::to_string(&result).unwrap();
        assert!(json.contains("\"ok\":true"));
        assert!(json.contains("\"skillName\":\"skill1\""));
    }

    #[test]
    fn test_plugins_list_response_serialization() {
        let resp = PluginsListResponse {
            handler_types: vec!["regex".to_string(), "lookup".to_string()],
            actions: vec![],
            validators: vec![],
            context_providers: vec![],
        };
        let json = serde_json::to_string(&resp).unwrap();
        assert!(json.contains("\"handlerTypes\""));
        assert!(json.contains("\"actions\""));
        assert!(json.contains("\"validators\""));
        assert!(json.contains("\"contextProviders\""));
    }
}
