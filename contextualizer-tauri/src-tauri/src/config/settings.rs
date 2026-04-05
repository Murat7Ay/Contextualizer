use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub struct AppSettings {
    #[serde(default)]
    pub handlers_path: String,

    #[serde(default)]
    pub plugins_directory: Option<String>,

    #[serde(default)]
    pub shortcut_key: Option<String>,

    #[serde(default)]
    pub modifier_keys: Option<Vec<String>>,

    #[serde(default)]
    pub theme: Option<String>,
}

pub fn parse_ini(content: &str) -> Result<IniConfig, String> {
    let mut config = IniConfig::new();
    let mut current_section = String::new();

    for line in content.lines() {
        let trimmed = line.trim();
        if trimmed.is_empty() || trimmed.starts_with(';') || trimmed.starts_with('#') {
            continue;
        }

        if trimmed.starts_with('[') && trimmed.ends_with(']') {
            current_section = trimmed[1..trimmed.len() - 1].to_string();
            continue;
        }

        if let Some(eq_pos) = trimmed.find('=') {
            let key = trimmed[..eq_pos].trim().to_string();
            let value = trimmed[eq_pos + 1..].trim().to_string();
            config.set(&current_section, &key, &value);
        }
    }

    Ok(config)
}

#[derive(Debug, Clone, Default)]
pub struct IniConfig {
    sections: HashMap<String, HashMap<String, String>>,
}

impl IniConfig {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn get(&self, section: &str, key: &str) -> Option<&str> {
        self.sections
            .get(section)
            .and_then(|s| s.get(key))
            .map(|s| s.as_str())
    }

    pub fn set(&mut self, section: &str, key: &str, value: &str) {
        self.sections
            .entry(section.to_string())
            .or_default()
            .insert(key.to_string(), value.to_string());
    }

    pub fn merge(&mut self, other: &IniConfig) {
        for (section, values) in &other.sections {
            for (key, value) in values {
                self.set(section, key, value);
            }
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_ini() {
        let content = "[database]\nhost=localhost\nport=5432\n[auth]\nuser=admin\n";
        let config = parse_ini(content).unwrap();
        assert_eq!(config.get("database", "host"), Some("localhost"));
        assert_eq!(config.get("database", "port"), Some("5432"));
        assert_eq!(config.get("auth", "user"), Some("admin"));
    }

    #[test]
    fn test_ini_comments_and_empty_lines() {
        let content = "; comment\n# another comment\n\n[section]\nkey=value\n";
        let config = parse_ini(content).unwrap();
        assert_eq!(config.get("section", "key"), Some("value"));
    }

    #[test]
    fn test_ini_missing_key() {
        let content = "[section]\nkey=value\n";
        let config = parse_ini(content).unwrap();
        assert_eq!(config.get("section", "missing"), None);
        assert_eq!(config.get("missing_section", "key"), None);
    }

    #[test]
    fn test_ini_merge_overrides() {
        let config_ini = "[db]\npassword=plain\nhost=localhost\n";
        let secrets_ini = "[db]\npassword=secret\n";
        let mut config = parse_ini(config_ini).unwrap();
        let secrets = parse_ini(secrets_ini).unwrap();
        config.merge(&secrets);
        assert_eq!(config.get("db", "password"), Some("secret"));
        assert_eq!(config.get("db", "host"), Some("localhost"));
    }

    #[test]
    fn test_app_settings_roundtrip() {
        let settings = AppSettings {
            handlers_path: "C:\\handlers.json".to_string(),
            theme: Some("dark".to_string()),
            ..Default::default()
        };
        let json = serde_json::to_string(&settings).unwrap();
        let parsed: AppSettings = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed.handlers_path, "C:\\handlers.json");
        assert_eq!(parsed.theme, Some("dark".to_string()));
    }
}
