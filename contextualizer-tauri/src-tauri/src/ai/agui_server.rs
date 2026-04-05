use axum::{
    extract::State,
    response::sse::{Event, Sse},
    routing::post,
    Json, Router,
};
use std::convert::Infallible;

use super::agui_events::{AgUiEvent, RunAgentInput};

#[derive(Clone)]
pub struct AgUiState {
    // Will hold AI provider config and MCP executor reference
}

impl AgUiState {
    pub fn new() -> Self {
        Self {}
    }
}

pub fn create_agui_router(state: AgUiState) -> Router {
    Router::new()
        .route("/agui/run", post(agui_run))
        .with_state(state)
}

async fn agui_run(
    State(_state): State<AgUiState>,
    Json(input): Json<RunAgentInput>,
) -> Sse<impl tokio_stream::Stream<Item = Result<Event, Infallible>>> {
    let stream = async_stream::stream! {
        let msg_id = format!("msg_{}", uuid_simple());

        yield Ok(sse_event(&AgUiEvent::RunStarted {
            thread_id: input.thread_id.clone(),
            run_id: input.run_id.clone(),
        }));

        yield Ok(sse_event(&AgUiEvent::TextMessageStart {
            message_id: msg_id.clone(),
            role: "assistant".to_string(),
        }));

        yield Ok(sse_event(&AgUiEvent::TextMessageContent {
            message_id: msg_id.clone(),
            delta: "Hello! I'm the Contextualizer AI assistant.".to_string(),
        }));

        yield Ok(sse_event(&AgUiEvent::TextMessageEnd {
            message_id: msg_id,
        }));

        yield Ok(sse_event(&AgUiEvent::RunFinished {
            thread_id: input.thread_id,
            run_id: input.run_id,
        }));
    };

    Sse::new(stream)
}

fn sse_event(event: &AgUiEvent) -> Event {
    Event::default()
        .json_data(event)
        .unwrap_or_else(|_| Event::default().data("error"))
}

fn uuid_simple() -> String {
    use std::time::{SystemTime, UNIX_EPOCH};
    let ts = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_default()
        .as_nanos();
    format!("{:x}", ts)
}

#[cfg(test)]
mod tests {
    use super::*;
    use axum_test::TestServer;

    fn test_state() -> AgUiState {
        AgUiState::new()
    }

    fn run_input() -> serde_json::Value {
        serde_json::json!({
            "thread_id": "t1",
            "run_id": "r1",
            "messages": [{"role": "user", "content": "Hello"}],
            "tools": []
        })
    }

    #[tokio::test]
    async fn test_agui_endpoint_returns_sse() {
        let server = TestServer::new(create_agui_router(test_state())).unwrap();
        let response = server
            .post("/agui/run")
            .content_type("application/json")
            .json(&run_input())
            .await;
        response.assert_status_ok();
        let text = response.text();
        assert!(text.contains("RUN_STARTED"));
        assert!(text.contains("TEXT_MESSAGE_START"));
        assert!(text.contains("TEXT_MESSAGE_CONTENT"));
        assert!(text.contains("TEXT_MESSAGE_END"));
        assert!(text.contains("RUN_FINISHED"));
    }

    #[tokio::test]
    async fn test_agui_events_order() {
        let server = TestServer::new(create_agui_router(test_state())).unwrap();
        let response = server
            .post("/agui/run")
            .content_type("application/json")
            .json(&run_input())
            .await;
        let text = response.text();
        let run_started_pos = text.find("RUN_STARTED").unwrap();
        let msg_start_pos = text.find("TEXT_MESSAGE_START").unwrap();
        let msg_content_pos = text.find("TEXT_MESSAGE_CONTENT").unwrap();
        let msg_end_pos = text.find("TEXT_MESSAGE_END").unwrap();
        let run_finished_pos = text.find("RUN_FINISHED").unwrap();

        assert!(run_started_pos < msg_start_pos);
        assert!(msg_start_pos < msg_content_pos);
        assert!(msg_content_pos < msg_end_pos);
        assert!(msg_end_pos < run_finished_pos);
    }
}
