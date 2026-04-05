use super::rhai_loader::RhaiPlugin;
use std::path::Path;

#[derive(Debug, Clone, PartialEq)]
pub enum PluginSource {
    Rhai,
    Wasm,
    Native,
}

#[derive(Debug)]
pub struct PluginEntry {
    pub type_name: String,
    pub source: PluginSource,
    pub name: String,
}

#[derive(Debug, Default)]
pub struct PluginManager {
    plugins: Vec<PluginEntry>,
}

impl PluginManager {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn register_rhai(&mut self, plugin: &RhaiPlugin) {
        self.plugins.push(PluginEntry {
            type_name: plugin.type_name.clone(),
            source: PluginSource::Rhai,
            name: plugin.name.clone(),
        });
    }

    pub fn load_rhai_directory(&mut self, dir: &Path) -> Result<usize, String> {
        let mut count = 0;
        let entries = std::fs::read_dir(dir)
            .map_err(|e| format!("Failed to read plugin directory: {}", e))?;

        for entry in entries.flatten() {
            let path = entry.path();
            if path.extension().and_then(|e| e.to_str()) == Some("rhai") {
                match RhaiPlugin::from_file(&path) {
                    Ok(plugin) => {
                        self.register_rhai(&plugin);
                        count += 1;
                    }
                    Err(e) => {
                        eprintln!("Failed to load plugin {:?}: {}", path, e);
                    }
                }
            }
        }

        Ok(count)
    }

    pub fn resolve(&self, type_name: &str) -> Option<&PluginEntry> {
        // Priority: Rhai > Wasm > Native
        self.plugins
            .iter()
            .filter(|p| p.type_name == type_name)
            .min_by_key(|p| match p.source {
                PluginSource::Rhai => 0,
                PluginSource::Wasm => 1,
                PluginSource::Native => 2,
            })
    }

    pub fn plugin_count(&self) -> usize {
        self.plugins.len()
    }

    pub fn list_plugins(&self) -> &[PluginEntry] {
        &self.plugins
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::io::Write;

    #[test]
    fn test_plugin_manager_register_and_resolve() {
        let mut manager = PluginManager::new();
        let plugin = RhaiPlugin::from_str("test", r#"fn type_name() { "test_type" }"#).unwrap();
        manager.register_rhai(&plugin);
        let resolved = manager.resolve("test_type").unwrap();
        assert_eq!(resolved.source, PluginSource::Rhai);
    }

    #[test]
    fn test_plugin_priority_order() {
        let mut manager = PluginManager::new();
        manager.plugins.push(PluginEntry {
            type_name: "shared".to_string(),
            source: PluginSource::Native,
            name: "native_shared".to_string(),
        });
        manager.plugins.push(PluginEntry {
            type_name: "shared".to_string(),
            source: PluginSource::Rhai,
            name: "rhai_shared".to_string(),
        });
        let resolved = manager.resolve("shared").unwrap();
        assert_eq!(resolved.source, PluginSource::Rhai);
    }

    #[test]
    fn test_plugin_directory_scan() {
        let tmp = tempfile::tempdir().unwrap();
        let mut f1 = std::fs::File::create(tmp.path().join("plugin1.rhai")).unwrap();
        writeln!(f1, r#"fn type_name() {{ "p1" }}"#).unwrap();
        let mut f2 = std::fs::File::create(tmp.path().join("plugin2.rhai")).unwrap();
        writeln!(f2, r#"fn type_name() {{ "p2" }}"#).unwrap();
        // Non-rhai file should be ignored
        std::fs::write(tmp.path().join("readme.txt"), "ignore me").unwrap();

        let mut manager = PluginManager::new();
        let count = manager.load_rhai_directory(tmp.path()).unwrap();
        assert_eq!(count, 2);
        assert_eq!(manager.plugin_count(), 2);
    }

    #[test]
    fn test_resolve_nonexistent() {
        let manager = PluginManager::new();
        assert!(manager.resolve("nonexistent").is_none());
    }

    #[test]
    fn test_empty_manager() {
        let manager = PluginManager::new();
        assert_eq!(manager.plugin_count(), 0);
        assert!(manager.list_plugins().is_empty());
    }
}
