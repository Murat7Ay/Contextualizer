use std::path::Path;

/// Rhai script plugin representation.
/// Full Rhai engine integration will be added when the `rhai` crate dependency is included.
#[derive(Debug, Clone)]
pub struct RhaiPlugin {
    pub name: String,
    pub type_name: String,
    pub script_path: String,
    pub script_content: String,
}

impl RhaiPlugin {
    pub fn from_file(path: &Path) -> Result<Self, String> {
        let content = std::fs::read_to_string(path)
            .map_err(|e| format!("Failed to read Rhai script: {}", e))?;

        let type_name = extract_type_name(&content)
            .unwrap_or_else(|| path.file_stem().unwrap_or_default().to_string_lossy().to_string());

        let name = path
            .file_stem()
            .unwrap_or_default()
            .to_string_lossy()
            .to_string();

        Ok(Self {
            name,
            type_name,
            script_path: path.to_string_lossy().to_string(),
            script_content: content,
        })
    }

    pub fn from_str(name: &str, content: &str) -> Result<Self, String> {
        if content.trim().is_empty() {
            return Err("Empty script content".to_string());
        }

        let type_name = extract_type_name(content).unwrap_or_else(|| name.to_string());

        Ok(Self {
            name: name.to_string(),
            type_name,
            script_path: String::new(),
            script_content: content.to_string(),
        })
    }
}

fn extract_type_name(content: &str) -> Option<String> {
    for line in content.lines() {
        let trimmed = line.trim();
        if trimmed.starts_with("fn type_name()") {
            if let Some(start) = trimmed.find('"') {
                if let Some(end) = trimmed[start + 1..].find('"') {
                    return Some(trimmed[start + 1..start + 1 + end].to_string());
                }
            }
        }
    }
    None
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    #[test]
    fn test_rhai_plugin_from_str() {
        let script = r#"
fn type_name() { "test_rhai" }
fn can_handle(input) { input.len() > 0 }
fn handle(input) { #{ success: true, output: input } }
"#;
        let plugin = RhaiPlugin::from_str("test", script).unwrap();
        assert_eq!(plugin.type_name, "test_rhai");
        assert_eq!(plugin.name, "test");
    }

    #[test]
    fn test_rhai_plugin_empty_content_error() {
        assert!(RhaiPlugin::from_str("test", "").is_err());
        assert!(RhaiPlugin::from_str("test", "   ").is_err());
    }

    #[test]
    fn test_rhai_plugin_from_file() {
        let mut tmp = tempfile::Builder::new()
            .suffix(".rhai")
            .tempfile()
            .unwrap();
        writeln!(tmp, r#"fn type_name() {{ "file_plugin" }}"#).unwrap();
        tmp.flush().unwrap();
        let plugin = RhaiPlugin::from_file(tmp.path()).unwrap();
        assert_eq!(plugin.type_name, "file_plugin");
    }

    #[test]
    fn test_extract_type_name() {
        assert_eq!(
            extract_type_name(r#"fn type_name() { "my_type" }"#),
            Some("my_type".to_string())
        );
        assert_eq!(extract_type_name("fn handle() {}"), None);
    }

    #[test]
    fn test_rhai_plugin_no_type_name_uses_filename() {
        let script = "fn handle(input) { input }";
        let plugin = RhaiPlugin::from_str("fallback_name", script).unwrap();
        assert_eq!(plugin.type_name, "fallback_name");
    }
}
