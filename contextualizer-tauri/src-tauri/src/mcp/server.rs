use axum::{
    extract::State,
    http::StatusCode,
    response::IntoResponse,
    routing::{get, post},
    Json, Router,
};
use std::sync::Arc;
use tokio::sync::RwLock;

use super::json_rpc::{handle_initialize, handle_unknown_method, JsonRpcRequest, JsonRpcResponse};
use super::registry::McpToolRegistry;

#[derive(Clone)]
pub struct McpState {
    pub registry: Arc<RwLock<McpToolRegistry>>,
}

impl McpState {
    pub fn new(management_tools_enabled: bool) -> Self {
        Self {
            registry: Arc::new(RwLock::new(McpToolRegistry::new(management_tools_enabled))),
        }
    }
}

pub fn create_mcp_router(state: McpState) -> Router {
    Router::new()
        .route("/mcp/health", get(health_check))
        .route("/mcp", post(mcp_handler))
        .with_state(state)
}

async fn health_check() -> impl IntoResponse {
    Json(serde_json::json!({
        "ok": true,
        "service": "contextualizer-mcp",
        "version": super::json_rpc::SERVER_VERSION
    }))
}

async fn mcp_handler(
    State(state): State<McpState>,
    Json(request): Json<JsonRpcRequest>,
) -> impl IntoResponse {
    if request.id.is_none() {
        return (StatusCode::NO_CONTENT, Json(serde_json::json!(null))).into_response();
    }

    let response = match request.method.as_str() {
        "initialize" => handle_initialize(request.id),
        "tools/list" => {
            let registry = state.registry.read().await;
            let tools: Vec<serde_json::Value> = registry
                .list_tools()
                .iter()
                .map(|t| {
                    serde_json::json!({
                        "name": t.name,
                        "description": t.description,
                        "inputSchema": t.input_schema
                    })
                })
                .collect();
            JsonRpcResponse::success(request.id, serde_json::json!({ "tools": tools }))
        }
        "tools/call" => {
            let tool_name = request.params.get("name").and_then(|n| n.as_str()).unwrap_or("");
            let registry = state.registry.read().await;
            if registry.find_tool(tool_name).is_none() {
                JsonRpcResponse::success(
                    request.id,
                    serde_json::json!({
                        "isError": true,
                        "content": [{"type": "text", "text": format!("Tool not found: {}", tool_name)}]
                    }),
                )
            } else {
                JsonRpcResponse::success(
                    request.id,
                    serde_json::json!({
                        "isError": false,
                        "content": [{"type": "text", "text": "Tool executed successfully"}]
                    }),
                )
            }
        }
        _ => handle_unknown_method(request.id, &request.method),
    };

    (StatusCode::OK, Json(serde_json::to_value(response).unwrap())).into_response()
}

#[cfg(test)]
mod tests {
    use super::*;
    use axum_test::TestServer;

    fn test_state() -> McpState {
        McpState::new(false)
    }

    fn json_rpc(method: &str, params: serde_json::Value) -> serde_json::Value {
        serde_json::json!({
            "jsonrpc": "2.0",
            "id": 1,
            "method": method,
            "params": params
        })
    }

    #[tokio::test]
    async fn test_health_endpoint() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.get("/mcp/health").await;
        response.assert_status_ok();
        let body: serde_json::Value = response.json();
        assert_eq!(body["ok"], true);
        assert_eq!(body["service"], "contextualizer-mcp");
    }

    #[tokio::test]
    async fn test_initialize_method() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.post("/mcp").json(&json_rpc("initialize", serde_json::json!({}))).await;
        response.assert_status_ok();
        let body: serde_json::Value = response.json();
        assert_eq!(body["result"]["protocolVersion"], "2024-11-05");
        assert_eq!(body["result"]["serverInfo"]["name"], "Contextualizer");
    }

    #[tokio::test]
    async fn test_tools_list_includes_ui_tools() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.post("/mcp").json(&json_rpc("tools/list", serde_json::json!({}))).await;
        let body: serde_json::Value = response.json();
        let tools = body["result"]["tools"].as_array().unwrap();
        let names: Vec<String> = tools.iter().map(|t| t["name"].as_str().unwrap().to_string()).collect();
        assert!(names.contains(&"ui_confirm".to_string()));
        assert!(names.contains(&"ui_notify".to_string()));
    }

    #[tokio::test]
    async fn test_tools_list_includes_handler_tools() {
        let state = McpState::new(false);
        {
            let mut reg = state.registry.write().await;
            reg.register_handler_tool(super::super::registry::McpToolDefinition {
                name: "my_tool".to_string(),
                description: "test".to_string(),
                input_schema: serde_json::json!({"type": "object"}),
            });
        }
        let server = TestServer::new(create_mcp_router(state)).unwrap();
        let response = server.post("/mcp").json(&json_rpc("tools/list", serde_json::json!({}))).await;
        let body: serde_json::Value = response.json();
        let tools = body["result"]["tools"].as_array().unwrap();
        let names: Vec<String> = tools.iter().map(|t| t["name"].as_str().unwrap().to_string()).collect();
        assert!(names.contains(&"my_tool".to_string()));
    }

    #[tokio::test]
    async fn test_tools_call_existing_tool() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.post("/mcp").json(&json_rpc("tools/call", serde_json::json!({
            "name": "ui_confirm",
            "arguments": { "title": "Test", "message": "Hello" }
        }))).await;
        let body: serde_json::Value = response.json();
        assert_eq!(body["result"]["isError"], false);
    }

    #[tokio::test]
    async fn test_unknown_method_returns_error() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.post("/mcp").json(&json_rpc("unknown/method", serde_json::json!({}))).await;
        let body: serde_json::Value = response.json();
        assert_eq!(body["error"]["code"], -32601);
    }

    #[tokio::test]
    async fn test_notification_returns_204() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.post("/mcp").json(&serde_json::json!({
            "jsonrpc": "2.0",
            "method": "notifications/initialized"
        })).await;
        response.assert_status(StatusCode::NO_CONTENT);
    }

    #[tokio::test]
    async fn test_call_nonexistent_tool() {
        let server = TestServer::new(create_mcp_router(test_state())).unwrap();
        let response = server.post("/mcp").json(&json_rpc("tools/call", serde_json::json!({
            "name": "nonexistent_tool",
            "arguments": {}
        }))).await;
        let body: serde_json::Value = response.json();
        assert_eq!(body["result"]["isError"], true);
    }

    #[tokio::test]
    async fn test_management_tools_gated() {
        let disabled = McpState::new(false);
        let enabled = McpState::new(true);

        let server_d = TestServer::new(create_mcp_router(disabled)).unwrap();
        let resp_d = server_d.post("/mcp").json(&json_rpc("tools/list", serde_json::json!({}))).await;
        let tools_d: Vec<String> = resp_d.json::<serde_json::Value>()["result"]["tools"]
            .as_array().unwrap().iter()
            .map(|t| t["name"].as_str().unwrap().to_string()).collect();
        assert!(!tools_d.contains(&"handlers_list".to_string()));

        let server_e = TestServer::new(create_mcp_router(enabled)).unwrap();
        let resp_e = server_e.post("/mcp").json(&json_rpc("tools/list", serde_json::json!({}))).await;
        let tools_e: Vec<String> = resp_e.json::<serde_json::Value>()["result"]["tools"]
            .as_array().unwrap().iter()
            .map(|t| t["name"].as_str().unwrap().to_string()).collect();
        assert!(tools_e.contains(&"handlers_list".to_string()));
    }
}
