use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct McpToolDefinition {
    pub name: String,
    pub description: String,
    #[serde(rename = "inputSchema")]
    pub input_schema: serde_json::Value,
}

#[derive(Debug, Default)]
pub struct McpToolRegistry {
    tools: Vec<McpToolDefinition>,
    management_tools_enabled: bool,
}

impl McpToolRegistry {
    pub fn new(management_tools_enabled: bool) -> Self {
        let mut registry = Self {
            tools: Vec::new(),
            management_tools_enabled,
        };
        registry.register_ui_tools();
        if management_tools_enabled {
            registry.register_management_tools();
        }
        registry
    }

    fn register_ui_tools(&mut self) {
        self.tools.push(McpToolDefinition {
            name: "ui_confirm".to_string(),
            description: "Show a confirmation dialog to the user".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "title": { "type": "string" },
                    "message": { "type": "string" }
                },
                "required": ["title", "message"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "ui_notify".to_string(),
            description: "Show a notification to the user".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "message": { "type": "string" },
                    "level": { "type": "string", "enum": ["info", "warning", "error", "success"] }
                },
                "required": ["message"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "ui_user_inputs".to_string(),
            description: "Prompt the user for inputs sequentially. Each input has key, title, message (required) plus optional fields: validation_regex, is_required, default_value, is_password, is_multi_line, is_file_picker, is_folder_picker, file_extensions, is_selection_list, selection_items, is_multi_select, is_date_picker, is_time_picker, is_datetime_picker, dependent_key, dependent_selection_item_map, config_target. Returns { cancelled, values }.".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "user_inputs": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "key": { "type": "string" },
                                "title": { "type": "string" },
                                "message": { "type": "string" },
                                "validation_regex": { "type": "string" },
                                "is_required": { "type": "boolean" },
                                "default_value": { "type": "string" },
                                "is_password": { "type": "boolean" },
                                "is_multi_line": { "type": "boolean" },
                                "is_file_picker": { "type": "boolean" },
                                "is_folder_picker": { "type": "boolean" },
                                "file_extensions": { "type": "array", "items": { "type": "string" } },
                                "is_selection_list": { "type": "boolean" },
                                "selection_items": { "type": "array", "items": { "type": "object", "properties": { "value": { "type": "string" }, "display": { "type": "string" } }, "required": ["value", "display"] } },
                                "is_multi_select": { "type": "boolean" },
                                "is_date_picker": { "type": "boolean" },
                                "is_time_picker": { "type": "boolean" },
                                "is_datetime_picker": { "type": "boolean" },
                                "dependent_key": { "type": "string" },
                                "dependent_selection_item_map": { "type": "object" },
                                "config_target": { "type": "string" }
                            },
                            "required": ["key", "title", "message"]
                        }
                    },
                    "context": { "type": "object", "description": "Initial context values for dependent selections" }
                },
                "required": ["user_inputs"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "ui_show_markdown".to_string(),
            description: "Open a new tab displaying rendered markdown content".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "title": { "type": "string" },
                    "markdown": { "type": "string" }
                },
                "required": ["title", "markdown"]
            }),
        });
    }

    fn register_management_tools(&mut self) {
        self.tools.push(McpToolDefinition {
            name: "handlers_list".to_string(),
            description: "List all configured handlers".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {}
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handler_reload".to_string(),
            description: "Reload handler configuration from disk".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {}
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handlers_get".to_string(),
            description: "Get a single handler config by name".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "name": { "type": "string" }
                },
                "required": ["name"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handler_delete".to_string(),
            description: "Delete an existing handler by name and optionally reload handlers".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "reload": { "type": "boolean" }
                },
                "required": ["name"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "plugins_list".to_string(),
            description: "List loaded plugin names (actions/validators/context_providers) and registered handler types".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {}
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "database_tool_create".to_string(),
            description: "Create a new database handler with connection string, query, and optional MCP configuration".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "description": { "type": "string" },
                    "connectionString": { "type": "string" },
                    "query": { "type": "string" },
                    "databaseType": { "type": "string", "enum": ["sqlite", "mssql", "postgres", "mysql"] },
                    "mcpEnabled": { "type": "boolean" },
                    "mcpToolName": { "type": "string" },
                    "mcpDescription": { "type": "string" },
                    "mcpInputSchema": { "type": "object" }
                },
                "required": ["name", "connectionString", "query"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handler_create_api".to_string(),
            description: "Create a new API handler with URL, method, headers, and body template".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "name": { "type": "string" },
                    "description": { "type": "string" },
                    "url": { "type": "string" },
                    "method": { "type": "string", "enum": ["GET", "POST", "PUT", "DELETE", "PATCH"] },
                    "headers": { "type": "object" },
                    "bodyTemplate": { "type": "string" },
                    "mcpEnabled": { "type": "boolean" },
                    "mcpToolName": { "type": "string" },
                    "mcpDescription": { "type": "string" }
                },
                "required": ["name", "url"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handler_update_api".to_string(),
            description: "Update an existing API handler's configuration".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "handlerName": { "type": "string" },
                    "url": { "type": "string" },
                    "method": { "type": "string" },
                    "headers": { "type": "object" },
                    "bodyTemplate": { "type": "string" },
                    "description": { "type": "string" }
                },
                "required": ["handlerName"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handler_update_database".to_string(),
            description: "Update an existing database handler's configuration".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "handlerName": { "type": "string" },
                    "connectionString": { "type": "string" },
                    "query": { "type": "string" },
                    "description": { "type": "string" }
                },
                "required": ["handlerName"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "handler_docs".to_string(),
            description: "Return the handler authoring guide with examples for all handler types".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "handlerType": { "type": "string", "description": "Optional filter for a specific handler type" }
                }
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "config_get_keys".to_string(),
            description: "List all configuration keys".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "section": { "type": "string", "description": "Optional section prefix to filter keys" }
                }
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "config_get_section".to_string(),
            description: "Get all key-value pairs in a configuration section".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "section": { "type": "string" }
                },
                "required": ["section"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "config_set_value".to_string(),
            description: "Set a configuration value by key".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "key": { "type": "string" },
                    "value": { "type": "string" }
                },
                "required": ["key", "value"]
            }),
        });
        self.tools.push(McpToolDefinition {
            name: "config_reload".to_string(),
            description: "Reload configuration from disk".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {}
            }),
        });
    }

    pub fn register_handler_tool(&mut self, tool: McpToolDefinition) {
        self.tools.push(tool);
    }

    pub fn list_tools(&self) -> &[McpToolDefinition] {
        &self.tools
    }

    pub fn find_tool(&self, name: &str) -> Option<&McpToolDefinition> {
        self.tools.iter().find(|t| t.name == name)
    }

    pub fn tool_names(&self) -> Vec<String> {
        self.tools.iter().map(|t| t.name.clone()).collect()
    }

    pub fn is_management_enabled(&self) -> bool {
        self.management_tools_enabled
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_registry_includes_ui_tools() {
        let registry = McpToolRegistry::new(false);
        let names = registry.tool_names();
        assert!(names.contains(&"ui_confirm".to_string()));
        assert!(names.contains(&"ui_notify".to_string()));
        assert!(names.contains(&"ui_user_inputs".to_string()));
        assert!(names.contains(&"ui_show_markdown".to_string()));
    }

    #[test]
    fn test_management_tools_gated() {
        let disabled = McpToolRegistry::new(false);
        let enabled = McpToolRegistry::new(true);
        assert!(!disabled.tool_names().contains(&"handlers_list".to_string()));
        assert!(enabled.tool_names().contains(&"handlers_list".to_string()));
        assert!(enabled.tool_names().contains(&"handlers_get".to_string()));
        assert!(enabled.tool_names().contains(&"handler_delete".to_string()));
        assert!(enabled.tool_names().contains(&"plugins_list".to_string()));
        assert!(enabled.tool_names().contains(&"database_tool_create".to_string()));
        assert!(enabled.tool_names().contains(&"handler_create_api".to_string()));
        assert!(enabled.tool_names().contains(&"handler_update_api".to_string()));
        assert!(enabled.tool_names().contains(&"handler_update_database".to_string()));
        assert!(enabled.tool_names().contains(&"handler_docs".to_string()));
        assert!(enabled.tool_names().contains(&"config_get_keys".to_string()));
        assert!(enabled.tool_names().contains(&"config_get_section".to_string()));
        assert!(enabled.tool_names().contains(&"config_set_value".to_string()));
        assert!(enabled.tool_names().contains(&"config_reload".to_string()));
    }

    #[test]
    fn test_register_handler_tool() {
        let mut registry = McpToolRegistry::new(false);
        registry.register_handler_tool(McpToolDefinition {
            name: "my_tool".to_string(),
            description: "test".to_string(),
            input_schema: serde_json::json!({"type": "object"}),
        });
        assert!(registry.tool_names().contains(&"my_tool".to_string()));
    }

    #[test]
    fn test_find_tool() {
        let registry = McpToolRegistry::new(false);
        assert!(registry.find_tool("ui_confirm").is_some());
        assert!(registry.find_tool("nonexistent").is_none());
    }
}
