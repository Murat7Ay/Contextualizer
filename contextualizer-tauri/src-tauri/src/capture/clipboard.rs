use crate::handlers::context::ClipboardContent;

pub fn create_text_content(text: &str) -> ClipboardContent {
    ClipboardContent::text(text)
}

pub fn create_file_content(files: Vec<String>) -> ClipboardContent {
    ClipboardContent::files(files)
}

pub fn create_empty_content() -> ClipboardContent {
    ClipboardContent::empty()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_clipboard_content_text_constructor() {
        let content = create_text_content("hello");
        assert!(content.success);
        assert!(content.is_text);
        assert!(!content.is_file);
        assert_eq!(content.text, "hello");
    }

    #[test]
    fn test_clipboard_content_files_constructor() {
        let content = create_file_content(vec!["a.txt".into(), "b.txt".into()]);
        assert!(content.success);
        assert!(content.is_file);
        assert!(!content.is_text);
        assert_eq!(content.files.len(), 2);
    }

    #[test]
    fn test_empty_clipboard_returns_failure() {
        let content = create_empty_content();
        assert!(!content.success);
    }
}
