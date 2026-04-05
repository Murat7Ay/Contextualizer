use super::lookup_handler::LookupHandler;
use super::regex_handler::RegexHandler;
use super::traits::Handler;
use super::types::HandlerConfig;
use anyhow::Result;

const KNOWN_TYPES: &[&str] = &[
    "regex", "lookup", "file", "database", "api", "custom", "manual", "synthetic", "cron",
];

pub struct HandlerFactory;

impl HandlerFactory {
    pub fn create(config: HandlerConfig) -> Result<Box<dyn Handler>> {
        let type_name = config.type_name.to_lowercase();
        match type_name.as_str() {
            "regex" => Ok(Box::new(RegexHandler::new(config)?)),
            "lookup" => Ok(Box::new(LookupHandler::new(config)?)),
            _ => anyhow::bail!("Unknown handler type: {}", type_name),
        }
    }

    pub fn is_type_registered(type_name: &str) -> bool {
        KNOWN_TYPES.contains(&type_name.to_lowercase().as_str())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_create_regex_handler() {
        let config = HandlerConfig {
            type_name: "regex".to_string(),
            regex: Some(r"\d+".to_string()),
            ..Default::default()
        };
        let handler = HandlerFactory::create(config);
        assert!(handler.is_ok());
        assert_eq!(handler.unwrap().type_name(), "regex");
    }

    #[test]
    fn test_create_lookup_handler() {
        let config = HandlerConfig {
            type_name: "lookup".to_string(),
            path: Some("/nonexistent".to_string()),
            delimiter: Some("|".to_string()),
            key_names: Some(vec!["k".to_string()]),
            value_names: Some(vec!["k".to_string()]),
            ..Default::default()
        };
        let handler = HandlerFactory::create(config);
        assert!(handler.is_ok());
        assert_eq!(handler.unwrap().type_name(), "lookup");
    }

    #[test]
    fn test_unknown_type_returns_error() {
        let config = HandlerConfig {
            type_name: "nonexistent".to_string(),
            ..Default::default()
        };
        assert!(HandlerFactory::create(config).is_err());
    }

    #[test]
    fn test_is_type_registered() {
        assert!(HandlerFactory::is_type_registered("regex"));
        assert!(HandlerFactory::is_type_registered("Regex"));
        assert!(HandlerFactory::is_type_registered("LOOKUP"));
        assert!(HandlerFactory::is_type_registered("database"));
        assert!(HandlerFactory::is_type_registered("api"));
        assert!(HandlerFactory::is_type_registered("manual"));
        assert!(HandlerFactory::is_type_registered("synthetic"));
        assert!(HandlerFactory::is_type_registered("cron"));
        assert!(HandlerFactory::is_type_registered("file"));
        assert!(HandlerFactory::is_type_registered("custom"));
        assert!(!HandlerFactory::is_type_registered("nonexistent"));
    }

    #[test]
    fn test_case_insensitive_type() {
        let config = HandlerConfig {
            type_name: "Regex".to_string(),
            regex: Some(r"\d+".to_string()),
            ..Default::default()
        };
        let handler = HandlerFactory::create(config);
        assert!(handler.is_ok());
    }
}
