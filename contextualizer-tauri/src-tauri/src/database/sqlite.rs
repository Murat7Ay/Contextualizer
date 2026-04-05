use std::collections::HashMap;

/// Lightweight in-memory SQLite-like key-value store for local data.
/// Full SQLite integration (via rusqlite) will be added when the dependency is included.
#[derive(Debug, Default)]
pub struct LocalStore {
    data: HashMap<String, String>,
}

impl LocalStore {
    pub fn new() -> Self {
        Self::default()
    }

    pub fn set(&mut self, key: &str, value: &str) {
        self.data.insert(key.to_string(), value.to_string());
    }

    pub fn get(&self, key: &str) -> Option<&str> {
        self.data.get(key).map(|s| s.as_str())
    }

    pub fn delete(&mut self, key: &str) -> bool {
        self.data.remove(key).is_some()
    }

    pub fn keys(&self) -> Vec<&str> {
        self.data.keys().map(|k| k.as_str()).collect()
    }

    pub fn len(&self) -> usize {
        self.data.len()
    }

    pub fn is_empty(&self) -> bool {
        self.data.is_empty()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_local_store_set_get() {
        let mut store = LocalStore::new();
        store.set("key1", "value1");
        assert_eq!(store.get("key1"), Some("value1"));
    }

    #[test]
    fn test_local_store_delete() {
        let mut store = LocalStore::new();
        store.set("key1", "value1");
        assert!(store.delete("key1"));
        assert!(store.get("key1").is_none());
        assert!(!store.delete("nonexistent"));
    }

    #[test]
    fn test_local_store_len() {
        let mut store = LocalStore::new();
        assert!(store.is_empty());
        store.set("a", "1");
        store.set("b", "2");
        assert_eq!(store.len(), 2);
    }
}
