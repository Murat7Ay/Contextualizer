use serde::{Deserialize, Serialize};

#[derive(Debug, Clone)]
pub enum AiChunk {
    Text(String),
    ToolCall {
        id: String,
        name: String,
        args: String,
    },
    Done,
    Error(String),
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AiProviderConfig {
    pub base_url: String,
    pub api_key: String,
    pub model: String,
}

impl AiProviderConfig {
    pub fn new(base_url: &str, api_key: &str, model: &str) -> Self {
        Self {
            base_url: base_url.to_string(),
            api_key: api_key.to_string(),
            model: model.to_string(),
        }
    }
}

pub fn mcp_tool_to_openai_function(
    mcp_tool: &crate::mcp::registry::McpToolDefinition,
) -> serde_json::Value {
    serde_json::json!({
        "type": "function",
        "function": {
            "name": mcp_tool.name,
            "description": mcp_tool.description,
            "parameters": mcp_tool.input_schema
        }
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::mcp::registry::McpToolDefinition;

    #[test]
    fn test_ai_provider_config_creation() {
        let config = AiProviderConfig::new("https://api.openai.com", "sk-test", "gpt-4o");
        assert_eq!(config.base_url, "https://api.openai.com");
        assert_eq!(config.model, "gpt-4o");
    }

    #[test]
    fn test_mcp_to_openai_tool_conversion() {
        let mcp_tool = McpToolDefinition {
            name: "my_tool".to_string(),
            description: "desc".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {"text": {"type": "string"}}
            }),
        };
        let openai_tool = mcp_tool_to_openai_function(&mcp_tool);
        assert_eq!(openai_tool["function"]["name"], "my_tool");
        assert_eq!(openai_tool["type"], "function");
    }

    #[test]
    fn test_ai_chunk_variants() {
        let text = AiChunk::Text("hello".to_string());
        let tool = AiChunk::ToolCall {
            id: "tc1".to_string(),
            name: "tool".to_string(),
            args: "{}".to_string(),
        };
        let done = AiChunk::Done;
        let err = AiChunk::Error("fail".to_string());

        match text {
            AiChunk::Text(t) => assert_eq!(t, "hello"),
            _ => panic!("Expected Text"),
        }
        match tool {
            AiChunk::ToolCall { name, .. } => assert_eq!(name, "tool"),
            _ => panic!("Expected ToolCall"),
        }
        assert!(matches!(done, AiChunk::Done));
        assert!(matches!(err, AiChunk::Error(_)));
    }

    #[test]
    fn test_ai_provider_config_roundtrip() {
        let config = AiProviderConfig::new("http://localhost:11434", "key", "llama3");
        let json = serde_json::to_string(&config).unwrap();
        let parsed: AiProviderConfig = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed.base_url, config.base_url);
        assert_eq!(parsed.model, config.model);
    }
}
