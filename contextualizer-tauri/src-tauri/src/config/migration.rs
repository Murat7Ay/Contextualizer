use serde::{Deserialize, Serialize};
use std::path::Path;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MigrationResult {
    pub success: bool,
    pub handlers_migrated: usize,
    pub settings_migrated: bool,
    pub warnings: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LegacySettings {
    #[serde(default)]
    pub shortcut: Option<String>,
    #[serde(default)]
    pub handlers_path: Option<String>,
    #[serde(default)]
    pub log_level: Option<String>,
    #[serde(default)]
    pub auto_start: Option<bool>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct TauriSettings {
    pub shortcut: String,
    pub handlers_path: String,
    pub log_level: String,
    pub auto_start: bool,
    pub mcp_port: u16,
    pub agui_port: u16,
}

impl Default for TauriSettings {
    fn default() -> Self {
        Self {
            shortcut: "CommandOrControl+Alt+W".to_string(),
            handlers_path: "handlers.json".to_string(),
            log_level: "info".to_string(),
            auto_start: false,
            mcp_port: 3000,
            agui_port: 3001,
        }
    }
}

pub fn migrate_settings(legacy: &LegacySettings) -> (TauriSettings, Vec<String>) {
    let mut warnings = Vec::new();
    let defaults = TauriSettings::default();

    let shortcut = legacy.shortcut.clone().unwrap_or_else(|| {
        warnings.push("No shortcut found, using default".to_string());
        defaults.shortcut.clone()
    });

    let handlers_path = legacy.handlers_path.clone().unwrap_or_else(|| {
        warnings.push("No handlers_path found, using default".to_string());
        defaults.handlers_path.clone()
    });

    let log_level = legacy.log_level.clone().unwrap_or_else(|| {
        defaults.log_level.clone()
    });

    let auto_start = legacy.auto_start.unwrap_or(defaults.auto_start);

    let settings = TauriSettings {
        shortcut,
        handlers_path,
        log_level,
        auto_start,
        mcp_port: defaults.mcp_port,
        agui_port: defaults.agui_port,
    };

    (settings, warnings)
}

pub fn migrate_from_file(path: &Path) -> Result<MigrationResult, String> {
    let content = std::fs::read_to_string(path)
        .map_err(|e| format!("Failed to read legacy config: {}", e))?;

    let legacy: LegacySettings = serde_json::from_str(&content)
        .map_err(|e| format!("Failed to parse legacy config: {}", e))?;

    let (settings, warnings) = migrate_settings(&legacy);

    let output_path = path.with_file_name("tauri_settings.json");
    let json = serde_json::to_string_pretty(&settings)
        .map_err(|e| format!("Failed to serialize settings: {}", e))?;
    std::fs::write(&output_path, json)
        .map_err(|e| format!("Failed to write migrated settings: {}", e))?;

    Ok(MigrationResult {
        success: true,
        handlers_migrated: 0,
        settings_migrated: true,
        warnings,
    })
}

pub fn validate_handlers_json(path: &Path) -> Result<usize, Vec<String>> {
    let content = std::fs::read_to_string(path)
        .map_err(|e| vec![format!("Failed to read handlers.json: {}", e)])?;

    let handlers: Vec<serde_json::Value> = serde_json::from_str(&content)
        .map_err(|e| vec![format!("Invalid JSON: {}", e)])?;

    let mut errors = Vec::new();
    for (i, handler) in handlers.iter().enumerate() {
        if handler["name"].is_null() {
            errors.push(format!("Handler {} missing 'name'", i));
        }
        if handler["type"].is_null() {
            errors.push(format!("Handler {} missing 'type'", i));
        }
    }

    if errors.is_empty() {
        Ok(handlers.len())
    } else {
        Err(errors)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_migrate_settings_with_all_fields() {
        let legacy = LegacySettings {
            shortcut: Some("Ctrl+Shift+C".to_string()),
            handlers_path: Some("my_handlers.json".to_string()),
            log_level: Some("debug".to_string()),
            auto_start: Some(true),
        };
        let (settings, warnings) = migrate_settings(&legacy);
        assert_eq!(settings.shortcut, "Ctrl+Shift+C");
        assert_eq!(settings.handlers_path, "my_handlers.json");
        assert_eq!(settings.log_level, "debug");
        assert!(settings.auto_start);
        assert!(warnings.is_empty());
    }

    #[test]
    fn test_migrate_settings_with_defaults() {
        let legacy = LegacySettings {
            shortcut: None,
            handlers_path: None,
            log_level: None,
            auto_start: None,
        };
        let (settings, warnings) = migrate_settings(&legacy);
        assert_eq!(settings.shortcut, "CommandOrControl+Alt+W");
        assert_eq!(settings.handlers_path, "handlers.json");
        assert!(!warnings.is_empty());
    }

    #[test]
    fn test_tauri_settings_default() {
        let settings = TauriSettings::default();
        assert_eq!(settings.mcp_port, 3000);
        assert_eq!(settings.agui_port, 3001);
        assert!(!settings.auto_start);
    }

    #[test]
    fn test_migrate_from_file() {
        let tmp = tempfile::tempdir().unwrap();
        let legacy_path = tmp.path().join("legacy.json");
        std::fs::write(
            &legacy_path,
            r#"{"shortcut":"Ctrl+C","handlers_path":"h.json"}"#,
        )
        .unwrap();
        let result = migrate_from_file(&legacy_path).unwrap();
        assert!(result.success);
        assert!(result.settings_migrated);

        let output = tmp.path().join("tauri_settings.json");
        assert!(output.exists());
        let content = std::fs::read_to_string(output).unwrap();
        let settings: TauriSettings = serde_json::from_str(&content).unwrap();
        assert_eq!(settings.shortcut, "Ctrl+C");
    }

    #[test]
    fn test_migrate_from_nonexistent_file() {
        let result = migrate_from_file(Path::new("/nonexistent/config.json"));
        assert!(result.is_err());
    }

    #[test]
    fn test_validate_handlers_json_valid() {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("handlers.json");
        std::fs::write(
            &path,
            r#"[{"name":"h1","type":"regex"},{"name":"h2","type":"lookup"}]"#,
        )
        .unwrap();
        let count = validate_handlers_json(&path).unwrap();
        assert_eq!(count, 2);
    }

    #[test]
    fn test_validate_handlers_json_missing_fields() {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("handlers.json");
        std::fs::write(&path, r#"[{"name":"h1"},{"type":"regex"}]"#).unwrap();
        let errors = validate_handlers_json(&path).unwrap_err();
        assert_eq!(errors.len(), 2);
    }

    #[test]
    fn test_validate_handlers_json_invalid_json() {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("handlers.json");
        std::fs::write(&path, "not json").unwrap();
        assert!(validate_handlers_json(&path).is_err());
    }

    #[test]
    fn test_migration_result_serialization() {
        let result = MigrationResult {
            success: true,
            handlers_migrated: 5,
            settings_migrated: true,
            warnings: vec!["test warning".to_string()],
        };
        let json = serde_json::to_string(&result).unwrap();
        let parsed: MigrationResult = serde_json::from_str(&json).unwrap();
        assert_eq!(parsed.handlers_migrated, 5);
        assert_eq!(parsed.warnings.len(), 1);
    }
}
