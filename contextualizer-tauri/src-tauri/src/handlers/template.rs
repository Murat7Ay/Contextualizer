use regex::Regex;
use std::collections::HashMap;
use std::sync::LazyLock;

static PLACEHOLDER_REGEX: LazyLock<Regex> =
    LazyLock::new(|| Regex::new(r"\$\(([^)]+)\)").unwrap());

static CONFIG_REGEX: LazyLock<Regex> =
    LazyLock::new(|| Regex::new(r#"\$config:([^)\s,\];&|<>"]+)"#).unwrap());

pub fn replace_dynamic_values(input: &str, context: &HashMap<String, String>) -> String {
    if input.is_empty() {
        return String::new();
    }

    let mut result = input.to_string();

    if result.starts_with("$file:") {
        let file_path = &result[6..];
        match std::fs::read_to_string(file_path) {
            Ok(content) => result = content,
            Err(_) => return result,
        }
    }

    result = CONFIG_REGEX
        .replace_all(&result, |caps: &regex::Captures| {
            let key = &caps[1];
            context
                .get(key)
                .cloned()
                .unwrap_or_else(|| caps[0].to_string())
        })
        .to_string();

    // Process $(key) placeholders
    result = PLACEHOLDER_REGEX
        .replace_all(&result, |caps: &regex::Captures| {
            let key = &caps[1];
            if key.is_empty() {
                return caps[0].to_string();
            }
            context
                .get(key)
                .cloned()
                .unwrap_or_else(|| caps[0].to_string())
        })
        .to_string();

    result
}

pub fn context_resolve(
    constant_seeder: Option<&HashMap<String, String>>,
    seeder: Option<&HashMap<String, String>>,
    context: &mut HashMap<String, String>,
) {
    if let Some(constants) = constant_seeder {
        for (key, value) in constants {
            if !key.is_empty() {
                context.insert(key.clone(), value.clone());
            }
        }
    }

    if let Some(seeder) = seeder {
        for (key, value) in seeder {
            let resolved = replace_dynamic_values(value, context);
            context.insert(key.clone(), resolved);
        }
    }

    let keys: Vec<String> = context.keys().cloned().collect();
    for key in keys {
        let value = context[&key].clone();
        let resolved = replace_dynamic_values(&value, context);
        context.insert(key, resolved);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_simple_placeholder_replacement() {
        let ctx = HashMap::from([("name".to_string(), "World".to_string())]);
        assert_eq!(replace_dynamic_values("Hello $(name)!", &ctx), "Hello World!");
    }

    #[test]
    fn test_missing_placeholder_stays() {
        let ctx = HashMap::new();
        assert_eq!(replace_dynamic_values("$(missing)", &ctx), "$(missing)");
    }

    #[test]
    fn test_multiple_placeholders() {
        let ctx = HashMap::from([
            ("a".to_string(), "1".to_string()),
            ("b".to_string(), "2".to_string()),
        ]);
        assert_eq!(
            replace_dynamic_values("$(a) and $(b)", &ctx),
            "1 and 2"
        );
    }

    #[test]
    fn test_empty_input_returns_empty() {
        assert_eq!(replace_dynamic_values("", &HashMap::new()), "");
    }

    #[test]
    fn test_no_placeholders_returns_input() {
        let ctx = HashMap::new();
        assert_eq!(
            replace_dynamic_values("no placeholders", &ctx),
            "no placeholders"
        );
    }

    #[test]
    fn test_file_placeholder_resolution() {
        let tmp = tempfile::NamedTempFile::new().unwrap();
        std::fs::write(tmp.path(), "file_content_here").unwrap();
        let input = format!("$file:{}", tmp.path().display());
        let result = replace_dynamic_values(&input, &HashMap::new());
        assert_eq!(result, "file_content_here");
    }

    #[test]
    fn test_context_resolve_constant_seeder() {
        let constants = HashMap::from([("env".to_string(), "prod".to_string())]);
        let mut ctx = HashMap::from([("name".to_string(), "test".to_string())]);
        context_resolve(Some(&constants), None, &mut ctx);
        assert_eq!(ctx.get("env").unwrap(), "prod");
        assert_eq!(ctx.get("name").unwrap(), "test");
    }

    #[test]
    fn test_context_resolve_seeder_with_placeholders() {
        let seeder = HashMap::from([("greeting".to_string(), "Hello $(name)!".to_string())]);
        let mut ctx = HashMap::from([("name".to_string(), "World".to_string())]);
        context_resolve(None, Some(&seeder), &mut ctx);
        assert_eq!(ctx.get("greeting").unwrap(), "Hello World!");
    }
}
