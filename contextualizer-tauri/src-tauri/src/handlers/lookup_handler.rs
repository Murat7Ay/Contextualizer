use super::context::{ClipboardContent, ContextKey};
use super::traits::Handler;
use super::types::HandlerConfig;
use anyhow::Result;
use std::collections::HashMap;
use std::fs;

pub struct LookupHandler {
    config: HandlerConfig,
    data: HashMap<String, HashMap<String, String>>,
}

impl LookupHandler {
    pub fn new(config: HandlerConfig) -> Result<Self> {
        let data = Self::load_data(&config);
        Ok(Self { config, data })
    }

    fn load_data(config: &HandlerConfig) -> HashMap<String, HashMap<String, String>> {
        let mut data: HashMap<String, HashMap<String, String>> = HashMap::new();

        let path = match &config.path {
            Some(p) => p.clone(),
            None => return data,
        };

        let delimiter = config.delimiter.as_deref().unwrap_or("|");
        let value_names = match &config.value_names {
            Some(vn) => vn,
            None => return data,
        };
        let key_names = match &config.key_names {
            Some(kn) => kn,
            None => return data,
        };

        let file_content = match fs::read_to_string(&path) {
            Ok(c) => c,
            Err(_) => return data,
        };

        for line in file_content.lines() {
            let trimmed = line.trim();
            if trimmed.is_empty() || trimmed.starts_with('#') {
                continue;
            }

            let parts: Vec<&str> = trimmed.split(delimiter).collect();
            if parts.len() != value_names.len() {
                continue;
            }

            let mut values = HashMap::new();
            for (i, name) in value_names.iter().enumerate() {
                let val = parts[i].replace("{{NEWLINE}}", "\n");
                values.insert(name.clone(), val);
            }

            for key_name in key_names {
                if let Some(key_value) = values.get(key_name) {
                    if !key_value.is_empty() {
                        data.insert(key_value.clone(), values.clone());
                    }
                }
            }
        }

        data
    }

    pub fn entry_count(&self) -> usize {
        self.data.len()
    }
}

impl Handler for LookupHandler {
    fn type_name(&self) -> &str {
        "lookup"
    }

    fn config(&self) -> &HandlerConfig {
        &self.config
    }

    fn can_handle(&self, content: &ClipboardContent) -> bool {
        if !content.is_text || content.text.is_empty() {
            return false;
        }
        self.data.contains_key(&content.text)
    }

    fn create_context(&self, content: &ClipboardContent) -> Result<HashMap<String, String>> {
        let mut context = HashMap::new();

        if !content.is_text {
            context.insert(ContextKey::ERROR.to_string(), "Invalid clipboard content".to_string());
            return Ok(context);
        }

        let input = &content.text;
        context.insert(ContextKey::INPUT.to_string(), input.clone());

        match self.data.get(input) {
            Some(lookup_data) => {
                for (k, v) in lookup_data {
                    context.insert(k.clone(), v.clone());
                }
            }
            None => {
                context.insert(
                    ContextKey::ERROR.to_string(),
                    format!("Lookup key not found: {}", input),
                );
            }
        }

        Ok(context)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    fn create_temp_lookup(content: &str) -> tempfile::NamedTempFile {
        let mut tmp = tempfile::NamedTempFile::new().unwrap();
        tmp.write_all(content.as_bytes()).unwrap();
        tmp.flush().unwrap();
        tmp
    }

    fn lookup_config(path: &std::path::Path) -> HandlerConfig {
        HandlerConfig {
            name: "test_lookup".to_string(),
            type_name: "lookup".to_string(),
            path: Some(path.to_str().unwrap().to_string()),
            delimiter: Some("|".to_string()),
            key_names: Some(vec!["col1".to_string()]),
            value_names: Some(vec!["col1".to_string(), "col2".to_string(), "col3".to_string()]),
            ..Default::default()
        }
    }

    #[test]
    fn test_load_delimited_file() {
        let tmp = create_temp_lookup("key1|val1|val2\nkey2|val3|val4\n");
        let config = lookup_config(tmp.path());
        let handler = LookupHandler::new(config).unwrap();
        let content = ClipboardContent::text("key1");
        assert!(handler.can_handle(&content));
    }

    #[test]
    fn test_missing_file_returns_empty_data() {
        let config = HandlerConfig {
            path: Some("/nonexistent/file.txt".to_string()),
            delimiter: Some("|".to_string()),
            key_names: Some(vec!["col1".to_string()]),
            value_names: Some(vec!["col1".to_string()]),
            ..Default::default()
        };
        let handler = LookupHandler::new(config).unwrap();
        let content = ClipboardContent::text("anything");
        assert!(!handler.can_handle(&content));
    }

    #[test]
    fn test_comments_and_empty_lines_skipped() {
        let tmp = create_temp_lookup("# comment\n\nkey1|val1|val2\n");
        let config = lookup_config(tmp.path());
        let handler = LookupHandler::new(config).unwrap();
        assert_eq!(handler.entry_count(), 1);
    }

    #[test]
    fn test_lookup_miss_sets_error() {
        let tmp = create_temp_lookup("key1|val1|val2\n");
        let config = lookup_config(tmp.path());
        let handler = LookupHandler::new(config).unwrap();
        let content = ClipboardContent::text("nonexistent_key");
        let ctx = handler.create_context(&content).unwrap();
        assert!(ctx.contains_key(ContextKey::ERROR));
    }

    #[test]
    fn test_newline_replacement() {
        let tmp = create_temp_lookup("key1|line1{{NEWLINE}}line2|val2\n");
        let config = lookup_config(tmp.path());
        let handler = LookupHandler::new(config).unwrap();
        let content = ClipboardContent::text("key1");
        let ctx = handler.create_context(&content).unwrap();
        assert!(ctx.get("col2").unwrap().contains('\n'));
    }

    #[test]
    fn test_lookup_creates_context_with_values() {
        let tmp = create_temp_lookup("ABC|Acme|V8\n");
        let config = lookup_config(tmp.path());
        let handler = LookupHandler::new(config).unwrap();
        let content = ClipboardContent::text("ABC");
        let ctx = handler.create_context(&content).unwrap();
        assert_eq!(ctx.get("col1"), Some(&"ABC".to_string()));
        assert_eq!(ctx.get("col2"), Some(&"Acme".to_string()));
        assert_eq!(ctx.get("col3"), Some(&"V8".to_string()));
    }
}
