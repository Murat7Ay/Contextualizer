use std::collections::HashMap;
use std::path::PathBuf;
use std::sync::Mutex;

use crate::commands::{NavigationInputResponse, UserInputResponse};
use crate::config::migration::TauriSettings;
use crate::cron::CronScheduler;
use crate::handlers::types::HandlerConfig;
use crate::mcp::registry::McpToolRegistry;
use crate::plugins::PluginManager;

pub struct AppStateInner {
    pub handlers: Vec<HandlerConfig>,
    pub handlers_path: PathBuf,
    pub settings: TauriSettings,
    pub mcp_registry: McpToolRegistry,
    pub plugin_manager: PluginManager,
    pub cron_scheduler: CronScheduler,
    pub config_values: HashMap<String, String>,
    pub pending_confirms: HashMap<String, tokio::sync::oneshot::Sender<bool>>,
    pub pending_inputs: HashMap<String, tokio::sync::oneshot::Sender<UserInputResponse>>,
    pub pending_nav_inputs: HashMap<String, tokio::sync::oneshot::Sender<NavigationInputResponse>>,
}

pub type AppState = Mutex<AppStateInner>;

impl AppStateInner {
    pub fn new(handlers_path: PathBuf) -> Self {
        Self {
            handlers: Vec::new(),
            handlers_path,
            settings: TauriSettings::default(),
            mcp_registry: McpToolRegistry::new(true),
            plugin_manager: PluginManager::new(),
            cron_scheduler: CronScheduler::new(),
            config_values: HashMap::new(),
            pending_confirms: HashMap::new(),
            pending_inputs: HashMap::new(),
            pending_nav_inputs: HashMap::new(),
        }
    }

    pub fn load_handlers(&mut self) -> Result<usize, String> {
        if !self.handlers_path.exists() {
            return Ok(0);
        }
        let content = std::fs::read_to_string(&self.handlers_path)
            .map_err(|e| format!("Failed to read handlers file: {}", e))?;
        let configs: Vec<HandlerConfig> = serde_json::from_str(&content)
            .map_err(|e| format!("Failed to parse handlers JSON: {}", e))?;
        let count = configs.len();

        for cfg in &configs {
            if cfg.mcp_enabled {
                let tool_name = cfg
                    .mcp_tool_name
                    .clone()
                    .unwrap_or_else(|| slugify(&cfg.name));
                let description = cfg
                    .mcp_description
                    .clone()
                    .or_else(|| cfg.description.clone())
                    .unwrap_or_else(|| format!("{} handler", cfg.type_name));
                let schema = cfg.mcp_input_schema.clone().unwrap_or_else(|| {
                    serde_json::json!({
                        "type": "object",
                        "properties": {
                            "input": { "type": "string", "description": "Input text to process" }
                        },
                        "required": ["input"]
                    })
                });
                self.mcp_registry.register_handler_tool(
                    crate::mcp::registry::McpToolDefinition {
                        name: tool_name,
                        description,
                        input_schema: schema,
                    },
                );
            }
        }

        self.handlers = configs;
        Ok(count)
    }

    pub fn find_handler(&self, name: &str) -> Option<&HandlerConfig> {
        self.handlers
            .iter()
            .find(|h| h.name.eq_ignore_ascii_case(name))
    }

    pub fn find_handler_mut(&mut self, name: &str) -> Option<&mut HandlerConfig> {
        self.handlers
            .iter_mut()
            .find(|h| h.name.eq_ignore_ascii_case(name))
    }

    pub fn save_handlers(&self) -> Result<(), String> {
        let json = serde_json::to_string_pretty(&self.handlers)
            .map_err(|e| format!("Failed to serialize handlers: {}", e))?;
        std::fs::write(&self.handlers_path, json)
            .map_err(|e| format!("Failed to write handlers file: {}", e))?;
        Ok(())
    }

    pub fn add_handler(&mut self, config: HandlerConfig) -> Result<(), String> {
        if self.find_handler(&config.name).is_some() {
            return Err(format!("Handler '{}' already exists", config.name));
        }
        self.handlers.push(config);
        Ok(())
    }

    pub fn delete_handler(&mut self, name: &str) -> Result<HandlerConfig, String> {
        let idx = self
            .handlers
            .iter()
            .position(|h| h.name.eq_ignore_ascii_case(name))
            .ok_or_else(|| format!("Handler '{}' not found", name))?;
        Ok(self.handlers.remove(idx))
    }
}

fn slugify(name: &str) -> String {
    name.to_lowercase()
        .chars()
        .map(|c| if c.is_alphanumeric() { c } else { '_' })
        .collect::<String>()
        .trim_matches('_')
        .to_string()
}

#[cfg(test)]
mod tests {
    use super::*;

    fn temp_handlers_path() -> (tempfile::TempDir, PathBuf) {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("handlers.json");
        (tmp, path)
    }

    #[test]
    fn test_app_state_inner_new() {
        let state = AppStateInner::new(PathBuf::from("handlers.json"));
        assert!(state.handlers.is_empty());
        assert_eq!(state.settings.mcp_port, 3000);
    }

    #[test]
    fn test_load_handlers_from_file() {
        let (_tmp, path) = temp_handlers_path();
        std::fs::write(
            &path,
            r#"[{"name":"test","type":"regex","pattern":".*","enabled":true}]"#,
        )
        .unwrap();
        let mut state = AppStateInner::new(path);
        let count = state.load_handlers().unwrap();
        assert_eq!(count, 1);
        assert_eq!(state.handlers[0].name, "test");
    }

    #[test]
    fn test_load_handlers_nonexistent_file() {
        let mut state = AppStateInner::new(PathBuf::from("/nonexistent/handlers.json"));
        let count = state.load_handlers().unwrap();
        assert_eq!(count, 0);
    }

    #[test]
    fn test_load_handlers_with_mcp_enabled() {
        let (_tmp, path) = temp_handlers_path();
        std::fs::write(
            &path,
            r#"[{"name":"MCP Handler","type":"regex","pattern":".*","mcp_enabled":true,"mcp_tool_name":"my_tool","mcp_description":"A test tool"}]"#,
        )
        .unwrap();
        let mut state = AppStateInner::new(path);
        state.load_handlers().unwrap();
        assert!(state.mcp_registry.find_tool("my_tool").is_some());
    }

    #[test]
    fn test_find_handler_case_insensitive() {
        let (_tmp, path) = temp_handlers_path();
        let mut state = AppStateInner::new(path);
        state.handlers.push(HandlerConfig {
            name: "MyHandler".to_string(),
            type_name: "regex".to_string(),
            ..Default::default()
        });
        assert!(state.find_handler("myhandler").is_some());
        assert!(state.find_handler("MYHANDLER").is_some());
        assert!(state.find_handler("nonexistent").is_none());
    }

    #[test]
    fn test_add_handler() {
        let (_tmp, path) = temp_handlers_path();
        let mut state = AppStateInner::new(path);
        let config = HandlerConfig {
            name: "new_handler".to_string(),
            type_name: "regex".to_string(),
            ..Default::default()
        };
        state.add_handler(config).unwrap();
        assert_eq!(state.handlers.len(), 1);
    }

    #[test]
    fn test_add_duplicate_handler_fails() {
        let (_tmp, path) = temp_handlers_path();
        let mut state = AppStateInner::new(path);
        let config = HandlerConfig {
            name: "handler1".to_string(),
            type_name: "regex".to_string(),
            ..Default::default()
        };
        state.add_handler(config.clone()).unwrap();
        assert!(state.add_handler(config).is_err());
    }

    #[test]
    fn test_delete_handler() {
        let (_tmp, path) = temp_handlers_path();
        let mut state = AppStateInner::new(path);
        state.handlers.push(HandlerConfig {
            name: "to_delete".to_string(),
            type_name: "regex".to_string(),
            ..Default::default()
        });
        let deleted = state.delete_handler("to_delete").unwrap();
        assert_eq!(deleted.name, "to_delete");
        assert!(state.handlers.is_empty());
    }

    #[test]
    fn test_delete_nonexistent_handler() {
        let (_tmp, path) = temp_handlers_path();
        let mut state = AppStateInner::new(path);
        assert!(state.delete_handler("nonexistent").is_err());
    }

    #[test]
    fn test_save_and_reload_handlers() {
        let (_tmp, path) = temp_handlers_path();
        let mut state = AppStateInner::new(path.clone());
        state.handlers.push(HandlerConfig {
            name: "saved_handler".to_string(),
            type_name: "lookup".to_string(),
            ..Default::default()
        });
        state.save_handlers().unwrap();

        let mut state2 = AppStateInner::new(path);
        state2.load_handlers().unwrap();
        assert_eq!(state2.handlers.len(), 1);
        assert_eq!(state2.handlers[0].name, "saved_handler");
    }

    #[test]
    fn test_slugify() {
        assert_eq!(slugify("My Handler Name"), "my_handler_name");
        assert_eq!(slugify("URL-Parser"), "url_parser");
        assert_eq!(slugify("test123"), "test123");
    }
}
