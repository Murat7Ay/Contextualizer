use super::context::{ClipboardContent, ContextKey};
use super::traits::Handler;
use super::types::HandlerConfig;
use anyhow::Result;
use regex::Regex;
use std::collections::HashMap;

pub struct RegexHandler {
    config: HandlerConfig,
    compiled_regex: Regex,
}

impl RegexHandler {
    pub fn new(config: HandlerConfig) -> Result<Self> {
        let pattern = config
            .regex
            .as_deref()
            .unwrap_or(".*");

        let compiled_regex = Regex::new(pattern)
            .map_err(|e| anyhow::anyhow!("Invalid regex pattern in handler '{}': {}", config.name, e))?;

        Ok(Self {
            config,
            compiled_regex,
        })
    }
}

impl Handler for RegexHandler {
    fn type_name(&self) -> &str {
        "regex"
    }

    fn config(&self) -> &HandlerConfig {
        &self.config
    }

    fn can_handle(&self, content: &ClipboardContent) -> bool {
        if !content.is_text || content.text.is_empty() {
            return false;
        }
        self.compiled_regex.is_match(&content.text)
    }

    fn create_context(&self, content: &ClipboardContent) -> Result<HashMap<String, String>> {
        let mut context = HashMap::new();

        if !content.is_text {
            context.insert(ContextKey::ERROR.to_string(), "Invalid clipboard content".to_string());
            return Ok(context);
        }

        let input = &content.text;
        context.insert(ContextKey::INPUT.to_string(), input.clone());

        if let Some(captures) = self.compiled_regex.captures(input) {
            context.insert(
                ContextKey::MATCH.to_string(),
                captures.get(0).map(|m| m.as_str().to_string()).unwrap_or_default(),
            );

            if let Some(ref groups) = self.config.groups {
                for (i, group_name) in groups.iter().enumerate() {
                    let value = captures
                        .name(group_name)
                        .map(|m| m.as_str().to_string())
                        .or_else(|| {
                            captures.get(i + 1).map(|m| m.as_str().to_string())
                        })
                        .unwrap_or_default();
                    context.insert(group_name.clone(), value);
                }
            } else {
                for i in 1..captures.len() {
                    if let Some(m) = captures.get(i) {
                        context.insert(format!("group_{}", i), m.as_str().to_string());
                    }
                }
            }
        } else {
            context.insert(
                ContextKey::ERROR.to_string(),
                "Regex pattern did not match the input".to_string(),
            );
        }

        Ok(context)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn regex_config(pattern: &str) -> HandlerConfig {
        HandlerConfig {
            name: "test".to_string(),
            type_name: "regex".to_string(),
            regex: Some(pattern.to_string()),
            ..Default::default()
        }
    }

    #[test]
    fn test_can_handle_matching_text() {
        let config = regex_config(r"^\d{3}-\d{4}$");
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("123-4567");
        assert!(handler.can_handle(&content));
    }

    #[test]
    fn test_can_handle_non_matching_text() {
        let config = regex_config(r"^\d{3}-\d{4}$");
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("hello");
        assert!(!handler.can_handle(&content));
    }

    #[test]
    fn test_can_handle_empty_content() {
        let config = regex_config(r".*");
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::empty();
        assert!(!handler.can_handle(&content));
    }

    #[test]
    fn test_named_groups_extraction() {
        let config = HandlerConfig {
            regex: Some(r"(?P<area>\d{3})-(?P<number>\d{4})".to_string()),
            groups: Some(vec!["area".to_string(), "number".to_string()]),
            ..regex_config("")
        };
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("123-4567");
        let ctx = handler.create_context(&content).unwrap();
        assert_eq!(ctx.get("area"), Some(&"123".to_string()));
        assert_eq!(ctx.get("number"), Some(&"4567".to_string()));
    }

    #[test]
    fn test_invalid_regex_returns_error() {
        let config = regex_config(r"[invalid");
        assert!(RegexHandler::new(config).is_err());
    }

    #[test]
    fn test_unnamed_groups_use_numeric_keys() {
        let config = HandlerConfig {
            regex: Some(r"(\d{3})-(\d{4})".to_string()),
            groups: None,
            ..regex_config("")
        };
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("123-4567");
        let ctx = handler.create_context(&content).unwrap();
        assert_eq!(ctx.get("group_1"), Some(&"123".to_string()));
        assert_eq!(ctx.get("group_2"), Some(&"4567".to_string()));
    }

    #[test]
    fn test_non_matching_creates_error_context() {
        let config = regex_config(r"^\d+$");
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("hello");
        let ctx = handler.create_context(&content).unwrap();
        assert!(ctx.contains_key(ContextKey::ERROR));
    }

    #[test]
    fn test_match_value_in_context() {
        let config = regex_config(r"\d+");
        let handler = RegexHandler::new(config).unwrap();
        let content = ClipboardContent::text("abc123def");
        let ctx = handler.create_context(&content).unwrap();
        assert_eq!(ctx.get(ContextKey::MATCH), Some(&"123".to_string()));
    }
}
