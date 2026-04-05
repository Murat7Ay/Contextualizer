pub mod commands;
pub mod ai;
pub mod capture;
pub mod config;
pub mod cron;
pub mod database;
pub mod error;
pub mod events;
pub mod handlers;
pub mod mcp;
pub mod plugins;
pub mod state;

use std::path::PathBuf;
use std::sync::Mutex;
use tauri::Manager;

use state::AppStateInner;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_clipboard_manager::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_http::init())
        .plugin(tauri_plugin_os::init())
        .plugin(tauri_plugin_process::init())
        .invoke_handler(tauri::generate_handler![
            commands::greet,
            commands::ping,
            // Handlers
            commands::handlers_list,
            commands::handler_get,
            commands::handler_create,
            commands::handler_update,
            commands::handler_delete,
            commands::handler_toggle,
            commands::handler_set_mcp,
            commands::handler_reload,
            commands::manual_handler_execute,
            commands::dispatch_clipboard,
            // Cron
            commands::cron_list,
            commands::cron_set_enabled,
            commands::cron_trigger,
            commands::cron_update,
            // Settings & Config
            commands::get_app_settings,
            commands::save_app_settings,
            commands::config_get,
            commands::config_set,
            commands::set_theme,
            // MCP & Plugins
            commands::mcp_tools_list,
            commands::plugin_list,
            // UI
            commands::ui_confirm,
            commands::ui_notify,
            commands::open_external,
            // Exchange
            commands::exchange_list,
            commands::exchange_install,
            commands::exchange_remove,
            // Log / Toast / Tab
            commands::emit_log,
            commands::emit_toast,
            commands::emit_open_tab,
            // File / Folder dialogs
            commands::open_file_dialog,
            commands::open_folder_dialog,
            // Prompt system
            commands::request_confirm,
            commands::submit_confirm_response,
            commands::request_user_input,
            commands::submit_user_input_response,
            commands::request_user_input_navigation,
            commands::submit_navigation_response,
            // Tab/Toast lifecycle
            commands::tab_action_execute,
            commands::tab_closed,
            commands::toast_action_execute,
            commands::toast_closed,
            // Logging / Usage
            commands::logging_test,
            commands::usage_test,
            commands::log_clear,
            // Exchange full
            commands::exchange_tags,
            commands::exchange_details,
            commands::exchange_update,
            commands::exchange_publish,
            // AI Skills Hub
            commands::ai_skills_hub_list,
            commands::ai_skills_hub_deploy,
            commands::ai_skills_hub_remove,
            commands::ai_skills_hub_pull,
            // Plugin list (WPF shape)
            commands::plugins_list_full,
        ])
        .setup(|app| {
            let app_dir = app
                .path()
                .app_data_dir()
                .unwrap_or_else(|_| PathBuf::from("."));

            let handlers_path = app_dir.join("handlers.json");
            let mut inner = AppStateInner::new(handlers_path);

            if let Err(e) = inner.load_handlers() {
                eprintln!("Warning: failed to load handlers: {}", e);
            }

            let mcp_port = inner.settings.mcp_port;
            let agui_port = inner.settings.agui_port;

            app.manage(Mutex::new(inner));

            start_mcp_server(mcp_port);
            start_agui_server(agui_port);

            {
                use events::{emit_event, HostReadyEvent, EVENT_HOST_READY};
                let handle = app.handle().clone();
                let ready_event = HostReadyEvent::new(mcp_port, "light");
                tauri::async_runtime::spawn(async move {
                    tokio::time::sleep(std::time::Duration::from_millis(500)).await;
                    let _ = emit_event(&handle, EVENT_HOST_READY, &ready_event);
                });
            }

            let window = app.get_webview_window("main").unwrap();
            #[cfg(debug_assertions)]
            window.open_devtools();
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

fn start_mcp_server(port: u16) {
    use mcp::server::{create_mcp_router, McpState};
    let state = McpState::new(true);
    let router = create_mcp_router(state);
    tauri::async_runtime::spawn(async move {
        let addr = format!("127.0.0.1:{}", port);
        let listener = match tokio::net::TcpListener::bind(&addr).await {
            Ok(l) => l,
            Err(e) => {
                eprintln!("MCP server failed to bind on {}: {}", addr, e);
                return;
            }
        };
        eprintln!("MCP server listening on {}", addr);
        if let Err(e) = axum::serve(listener, router).await {
            eprintln!("MCP server error: {}", e);
        }
    });
}

fn start_agui_server(port: u16) {
    use ai::agui_server::{create_agui_router, AgUiState};
    let state = AgUiState::new();
    let router = create_agui_router(state);
    tauri::async_runtime::spawn(async move {
        let addr = format!("127.0.0.1:{}", port);
        let listener = match tokio::net::TcpListener::bind(&addr).await {
            Ok(l) => l,
            Err(e) => {
                eprintln!("AG-UI server failed to bind on {}: {}", addr, e);
                return;
            }
        };
        eprintln!("AG-UI server listening on {}", addr);
        if let Err(e) = axum::serve(listener, router).await {
            eprintln!("AG-UI server error: {}", e);
        }
    });
}

#[cfg(test)]
mod tests {
    #[test]
    fn test_capabilities_json_is_valid() {
        let caps_str = include_str!("../capabilities/default.json");
        let caps: serde_json::Value = serde_json::from_str(caps_str).unwrap();
        assert!(
            caps["permissions"].is_array(),
            "capabilities/default.json must have a 'permissions' array"
        );
        let perms = caps["permissions"].as_array().unwrap();
        assert!(
            !perms.is_empty(),
            "permissions array must not be empty"
        );
    }

    #[test]
    fn test_tauri_conf_json_is_valid() {
        let conf_str = include_str!("../tauri.conf.json");
        let conf: serde_json::Value = serde_json::from_str(conf_str).unwrap();
        assert_eq!(conf["productName"], "Contextualizer");
        assert!(conf["app"]["windows"].is_array());
    }
}
