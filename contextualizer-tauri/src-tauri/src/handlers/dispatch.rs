use super::context::{ClipboardContent, ContextKey, DispatchResult};
use super::template::{context_resolve, replace_dynamic_values};
use super::traits::Handler;
use super::types::HandlerConfig;
use std::collections::HashMap;

pub fn execute_handler(
    handler: &dyn Handler,
    content: &ClipboardContent,
    seed_context: Option<HashMap<String, String>>,
) -> DispatchResult {
    let config = handler.config();

    let is_mcp_call = seed_context
        .as_ref()
        .and_then(|sc| sc.get(ContextKey::TRIGGER))
        .map(|t| t.eq_ignore_ascii_case("mcp"))
        .unwrap_or(false);

    let is_headless_mcp = is_mcp_call && config.mcp_headless;

    let can_handle = handler.can_handle(content);
    if !can_handle && !is_mcp_call {
        return DispatchResult {
            can_handle: false,
            processed: false,
            cancelled: false,
            context: None,
            formatted_output: None,
        };
    }

    if config.requires_confirmation && is_headless_mcp {
        let msg = format!(
            "Handler '{}' requires confirmation and cannot run in MCP headless mode.",
            config.name
        );
        return DispatchResult {
            can_handle: true,
            processed: false,
            cancelled: true,
            context: None,
            formatted_output: Some(msg),
        };
    }

    let mut context = match handler.create_context(content) {
        Ok(ctx) => ctx,
        Err(e) => {
            let mut ctx = HashMap::new();
            ctx.insert(ContextKey::ERROR.to_string(), e.to_string());
            ctx
        }
    };

    if !context.contains_key(ContextKey::TRIGGER) {
        context.insert(ContextKey::TRIGGER.to_string(), "app".to_string());
    }

    if let Some(seed) = &seed_context {
        for (key, value) in seed {
            if key.is_empty() {
                continue;
            }
            if key == ContextKey::TRIGGER {
                context.insert(ContextKey::TRIGGER.to_string(), value.clone());
            } else if is_mcp_call && config.mcp_seed_overwrite {
                context.insert(key.clone(), value.clone());
            } else if !context.contains_key(key) {
                context.insert(key.clone(), value.clone());
            }
        }
    }

    find_selector_key(content, &mut context);

    if is_headless_mcp {
        if let Some(ref user_inputs) = config.user_inputs {
            let mut missing: Vec<String> = Vec::new();
            for input in user_inputs {
                if input.key.is_empty() {
                    continue;
                }
                if context.get(&input.key).map(|v| !v.is_empty()).unwrap_or(false) {
                    continue;
                }
                if !input.default_value.is_empty() {
                    let resolved = replace_dynamic_values(&input.default_value, &context);
                    if !resolved.is_empty() {
                        context.insert(input.key.clone(), resolved);
                    }
                }
                if input.is_required
                    && context.get(&input.key).map(|v| v.is_empty()).unwrap_or(true)
                {
                    missing.push(input.key.clone());
                }
            }
            if !missing.is_empty() {
                let msg = format!(
                    "Handler '{}' is missing required inputs for MCP headless execution: {}",
                    config.name,
                    missing.join(", ")
                );
                context.insert(ContextKey::ERROR.to_string(), msg.clone());
                context.insert(ContextKey::FORMATTED_OUTPUT.to_string(), msg.clone());
                return DispatchResult {
                    can_handle: true,
                    processed: false,
                    cancelled: true,
                    context: Some(context),
                    formatted_output: Some(msg),
                };
            }
        }
    }

    context_resolve(
        config.constant_seeder.as_ref(),
        config.seeder.as_ref(),
        &mut context,
    );

    context_default_seed(config, &mut context);

    let formatted_output = context.get(ContextKey::FORMATTED_OUTPUT).cloned();

    DispatchResult {
        can_handle: true,
        processed: true,
        cancelled: false,
        context: Some(context),
        formatted_output,
    }
}

fn find_selector_key(content: &ClipboardContent, context: &mut HashMap<String, String>) {
    if !content.is_text {
        return;
    }
    for (key, value) in context.iter() {
        if value == &content.text {
            let sel_key = key.clone();
            context.insert(ContextKey::SELECTOR_KEY.to_string(), sel_key);
            return;
        }
    }
}

fn context_default_seed(config: &HandlerConfig, context: &mut HashMap<String, String>) {
    if !context.contains_key(ContextKey::SELF) {
        let serialized = serde_json::to_string_pretty(context).unwrap_or_default();
        context.insert(ContextKey::SELF.to_string(), serialized);
    }

    if context.contains_key(ContextKey::FORMATTED_OUTPUT) {
        return;
    }

    match &config.output_format {
        Some(fmt) if !fmt.is_empty() => {
            let formatted = replace_dynamic_values(fmt, context);
            context.insert(ContextKey::FORMATTED_OUTPUT.to_string(), formatted);
        }
        _ => {
            let self_val = context.get(ContextKey::SELF).cloned().unwrap_or_default();
            context.insert(ContextKey::FORMATTED_OUTPUT.to_string(), self_val);
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::handlers::regex_handler::RegexHandler;

    fn create_regex_handler(pattern: &str) -> Box<dyn Handler> {
        let config = HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            regex: Some(pattern.to_string()),
            ..Default::default()
        };
        Box::new(RegexHandler::new(config).unwrap())
    }

    #[test]
    fn test_successful_execution_returns_processed() {
        let handler = create_regex_handler(r"\d+");
        let content = ClipboardContent::text("12345");
        let result = execute_handler(handler.as_ref(), &content, None);
        assert!(result.processed);
        assert!(result.can_handle);
        assert!(!result.cancelled);
    }

    #[test]
    fn test_no_match_returns_not_processed() {
        let handler = create_regex_handler(r"^\d+$");
        let content = ClipboardContent::text("hello");
        let result = execute_handler(handler.as_ref(), &content, None);
        assert!(!result.processed);
        assert!(!result.can_handle);
    }

    #[test]
    fn test_mcp_trigger_bypasses_can_handle() {
        let handler = create_regex_handler(r"^\d+$");
        let content = ClipboardContent::text("hello");
        let seed = HashMap::from([("_trigger".to_string(), "mcp".to_string())]);
        let result = execute_handler(handler.as_ref(), &content, Some(seed));
        assert!(result.can_handle || result.processed);
    }

    #[test]
    fn test_mcp_headless_with_confirmation_is_cancelled() {
        let config = HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            regex: Some(r".*".to_string()),
            requires_confirmation: true,
            mcp_headless: true,
            ..Default::default()
        };
        let handler = RegexHandler::new(config).unwrap();
        let seed = HashMap::from([("_trigger".to_string(), "mcp".to_string())]);
        let result = execute_handler(&handler, &ClipboardContent::text("test"), Some(seed));
        assert!(result.cancelled);
    }

    #[test]
    fn test_seed_context_merges_into_context() {
        let handler = create_regex_handler(r".*");
        let content = ClipboardContent::text("test");
        let seed = HashMap::from([
            ("custom_key".to_string(), "custom_value".to_string()),
            ("_trigger".to_string(), "mcp".to_string()),
        ]);
        let result = execute_handler(handler.as_ref(), &content, Some(seed));
        let ctx = result.context.unwrap();
        assert_eq!(ctx.get("custom_key"), Some(&"custom_value".to_string()));
    }

    #[test]
    fn test_output_format_template_resolution() {
        let config = HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            regex: Some(r"(?P<name>\w+)".to_string()),
            groups: Some(vec!["name".to_string()]),
            output_format: Some("Hello $(name)!".to_string()),
            ..Default::default()
        };
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("World");
        let result = execute_handler(&handler, &content, None);
        assert_eq!(result.formatted_output, Some("Hello World!".to_string()));
    }

    #[test]
    fn test_no_output_format_defaults_to_self_json() {
        let handler = create_regex_handler(r"(\d+)");
        let content = ClipboardContent::text("42");
        let result = execute_handler(handler.as_ref(), &content, None);
        let output = result.formatted_output.unwrap();
        assert!(serde_json::from_str::<serde_json::Value>(&output).is_ok());
    }

    #[test]
    fn test_headless_mcp_missing_required_inputs_cancelled() {
        let config = HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            regex: Some(r".*".to_string()),
            mcp_headless: true,
            user_inputs: Some(vec![super::super::types::UserInputRequest {
                key: "required_field".to_string(),
                is_required: true,
                ..Default::default()
            }]),
            ..Default::default()
        };
        let handler = RegexHandler::new(config).unwrap();
        let seed = HashMap::from([("_trigger".to_string(), "mcp".to_string())]);
        let result = execute_handler(&handler, &ClipboardContent::text("test"), Some(seed));
        assert!(result.cancelled);
        assert!(result.formatted_output.unwrap().contains("missing required inputs"));
    }
}
