use serde::Serialize;

#[derive(Debug, Clone, Serialize)]
#[serde(tag = "type")]
pub enum AgUiEvent {
    #[serde(rename = "RUN_STARTED")]
    RunStarted { thread_id: String, run_id: String },

    #[serde(rename = "TEXT_MESSAGE_START")]
    TextMessageStart { message_id: String, role: String },

    #[serde(rename = "TEXT_MESSAGE_CONTENT")]
    TextMessageContent { message_id: String, delta: String },

    #[serde(rename = "TEXT_MESSAGE_END")]
    TextMessageEnd { message_id: String },

    #[serde(rename = "TOOL_CALL_START")]
    ToolCallStart {
        tool_call_id: String,
        tool_call_name: String,
    },

    #[serde(rename = "TOOL_CALL_ARGS")]
    ToolCallArgs {
        tool_call_id: String,
        delta: String,
    },

    #[serde(rename = "TOOL_CALL_END")]
    ToolCallEnd { tool_call_id: String },

    #[serde(rename = "TOOL_CALL_RESULT")]
    ToolCallResult {
        tool_call_id: String,
        content: String,
    },

    #[serde(rename = "STATE_SNAPSHOT")]
    StateSnapshot { snapshot: serde_json::Value },

    #[serde(rename = "RUN_FINISHED")]
    RunFinished { thread_id: String, run_id: String },

    #[serde(rename = "RUN_ERROR")]
    RunError { message: String },
}

#[derive(Debug, Clone, serde::Deserialize)]
pub struct RunAgentInput {
    pub thread_id: String,
    pub run_id: String,
    pub messages: Vec<serde_json::Value>,
    #[serde(default)]
    pub tools: Vec<serde_json::Value>,
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_run_started_serialization() {
        let event = AgUiEvent::RunStarted {
            thread_id: "t1".to_string(),
            run_id: "r1".to_string(),
        };
        let json = serde_json::to_value(&event).unwrap();
        assert_eq!(json["type"], "RUN_STARTED");
        assert_eq!(json["thread_id"], "t1");
    }

    #[test]
    fn test_text_message_content_serialization() {
        let event = AgUiEvent::TextMessageContent {
            message_id: "m1".to_string(),
            delta: "Hello".to_string(),
        };
        let json = serde_json::to_value(&event).unwrap();
        assert_eq!(json["type"], "TEXT_MESSAGE_CONTENT");
        assert_eq!(json["delta"], "Hello");
    }

    #[test]
    fn test_tool_call_events_serialization() {
        let start = AgUiEvent::ToolCallStart {
            tool_call_id: "tc1".to_string(),
            tool_call_name: "my_tool".to_string(),
        };
        let json = serde_json::to_value(&start).unwrap();
        assert_eq!(json["type"], "TOOL_CALL_START");
        assert_eq!(json["tool_call_name"], "my_tool");
    }

    #[test]
    fn test_run_error_serialization() {
        let event = AgUiEvent::RunError {
            message: "Something went wrong".to_string(),
        };
        let json = serde_json::to_value(&event).unwrap();
        assert_eq!(json["type"], "RUN_ERROR");
    }

    #[test]
    fn test_state_snapshot_serialization() {
        let event = AgUiEvent::StateSnapshot {
            snapshot: serde_json::json!({"handlers": []}),
        };
        let json = serde_json::to_value(&event).unwrap();
        assert_eq!(json["type"], "STATE_SNAPSHOT");
        assert!(json["snapshot"]["handlers"].is_array());
    }

    #[test]
    fn test_run_agent_input_deserialization() {
        let json = r#"{
            "thread_id": "t1",
            "run_id": "r1",
            "messages": [{"role": "user", "content": "Hello"}],
            "tools": []
        }"#;
        let input: RunAgentInput = serde_json::from_str(json).unwrap();
        assert_eq!(input.thread_id, "t1");
        assert_eq!(input.messages.len(), 1);
    }

    #[test]
    fn test_all_event_types_serialize() {
        let events = vec![
            AgUiEvent::RunStarted { thread_id: "t".into(), run_id: "r".into() },
            AgUiEvent::TextMessageStart { message_id: "m".into(), role: "assistant".into() },
            AgUiEvent::TextMessageContent { message_id: "m".into(), delta: "hi".into() },
            AgUiEvent::TextMessageEnd { message_id: "m".into() },
            AgUiEvent::ToolCallStart { tool_call_id: "tc".into(), tool_call_name: "t".into() },
            AgUiEvent::ToolCallArgs { tool_call_id: "tc".into(), delta: "{}".into() },
            AgUiEvent::ToolCallEnd { tool_call_id: "tc".into() },
            AgUiEvent::ToolCallResult { tool_call_id: "tc".into(), content: "ok".into() },
            AgUiEvent::StateSnapshot { snapshot: serde_json::json!({}) },
            AgUiEvent::RunFinished { thread_id: "t".into(), run_id: "r".into() },
            AgUiEvent::RunError { message: "err".into() },
        ];
        for event in events {
            let json = serde_json::to_string(&event);
            assert!(json.is_ok(), "Failed to serialize {:?}", event);
        }
    }
}
