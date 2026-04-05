use std::collections::HashMap;

pub struct ContextKey;

impl ContextKey {
    pub const INPUT: &'static str = "_input";
    pub const MATCH: &'static str = "_match";
    pub const ERROR: &'static str = "_error";
    pub const SELF: &'static str = "_self";
    pub const FORMATTED_OUTPUT: &'static str = "_formatted_output";
    pub const TRIGGER: &'static str = "_trigger";
    pub const SELECTOR_KEY: &'static str = "_selector_key";
}

#[derive(Debug, Clone, Default)]
pub struct ClipboardContent {
    pub success: bool,
    pub is_text: bool,
    pub is_file: bool,
    pub text: String,
    pub files: Vec<String>,
    pub seed_context: Option<HashMap<String, String>>,
}

impl ClipboardContent {
    pub fn text(text: &str) -> Self {
        Self {
            success: !text.is_empty(),
            is_text: true,
            is_file: false,
            text: text.to_string(),
            files: vec![],
            seed_context: None,
        }
    }

    pub fn files(files: Vec<String>) -> Self {
        Self {
            success: !files.is_empty(),
            is_text: false,
            is_file: true,
            text: String::new(),
            files,
            seed_context: None,
        }
    }

    pub fn empty() -> Self {
        Self {
            success: false,
            is_text: false,
            is_file: false,
            text: String::new(),
            files: vec![],
            seed_context: None,
        }
    }
}

#[derive(Debug, Clone, Default)]
pub struct DispatchResult {
    pub can_handle: bool,
    pub processed: bool,
    pub cancelled: bool,
    pub context: Option<HashMap<String, String>>,
    pub formatted_output: Option<String>,
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_clipboard_content_text() {
        let content = ClipboardContent::text("hello");
        assert!(content.success);
        assert!(content.is_text);
        assert!(!content.is_file);
        assert_eq!(content.text, "hello");
    }

    #[test]
    fn test_clipboard_content_files() {
        let content = ClipboardContent::files(vec!["a.txt".into(), "b.txt".into()]);
        assert!(content.success);
        assert!(content.is_file);
        assert!(!content.is_text);
        assert_eq!(content.files.len(), 2);
    }

    #[test]
    fn test_clipboard_content_empty() {
        let content = ClipboardContent::empty();
        assert!(!content.success);
        assert!(!content.is_text);
        assert!(!content.is_file);
    }

    #[test]
    fn test_clipboard_content_empty_string_is_not_success() {
        let content = ClipboardContent::text("");
        assert!(!content.success);
    }
}
