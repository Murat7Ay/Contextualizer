use super::types::{Condition, ConfigAction};
use std::collections::HashMap;

pub fn evaluate_condition(condition: &Condition, context: &HashMap<String, String>) -> bool {
    match condition.operator.to_lowercase().as_str() {
        "equals" => {
            let field = condition.field.as_deref().unwrap_or("");
            let expected = condition.value.as_deref().unwrap_or("");
            context
                .get(field)
                .map(|v| v == expected)
                .unwrap_or(false)
        }
        "not_equals" => {
            let field = condition.field.as_deref().unwrap_or("");
            let expected = condition.value.as_deref().unwrap_or("");
            context
                .get(field)
                .map(|v| v != expected)
                .unwrap_or(true)
        }
        "contains" => {
            let field = condition.field.as_deref().unwrap_or("");
            let expected = condition.value.as_deref().unwrap_or("");
            context
                .get(field)
                .map(|v| v.contains(expected))
                .unwrap_or(false)
        }
        "exists" => {
            let field = condition.field.as_deref().unwrap_or("");
            context.contains_key(field)
        }
        "and" => condition
            .conditions
            .as_ref()
            .map(|conds| conds.iter().all(|c| evaluate_condition(c, context)))
            .unwrap_or(true),
        "or" => condition
            .conditions
            .as_ref()
            .map(|conds| conds.iter().any(|c| evaluate_condition(c, context)))
            .unwrap_or(false),
        _ => true,
    }
}

pub fn should_execute_action(action: &ConfigAction, context: &HashMap<String, String>) -> bool {
    match &action.conditions {
        Some(cond) => evaluate_condition(cond, context),
        None => true,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_equals_condition_match() {
        let cond = Condition {
            operator: "equals".to_string(),
            field: Some("status".to_string()),
            value: Some("active".to_string()),
            conditions: None,
        };
        let ctx = HashMap::from([("status".to_string(), "active".to_string())]);
        assert!(evaluate_condition(&cond, &ctx));
    }

    #[test]
    fn test_equals_condition_no_match() {
        let cond = Condition {
            operator: "equals".to_string(),
            field: Some("status".to_string()),
            value: Some("active".to_string()),
            conditions: None,
        };
        let ctx = HashMap::from([("status".to_string(), "inactive".to_string())]);
        assert!(!evaluate_condition(&cond, &ctx));
    }

    #[test]
    fn test_not_equals_condition() {
        let cond = Condition {
            operator: "not_equals".to_string(),
            field: Some("status".to_string()),
            value: Some("deleted".to_string()),
            conditions: None,
        };
        let ctx = HashMap::from([("status".to_string(), "active".to_string())]);
        assert!(evaluate_condition(&cond, &ctx));
    }

    #[test]
    fn test_contains_condition() {
        let cond = Condition {
            operator: "contains".to_string(),
            field: Some("text".to_string()),
            value: Some("hello".to_string()),
            conditions: None,
        };
        let ctx = HashMap::from([("text".to_string(), "say hello world".to_string())]);
        assert!(evaluate_condition(&cond, &ctx));
    }

    #[test]
    fn test_exists_condition() {
        let cond = Condition {
            operator: "exists".to_string(),
            field: Some("key".to_string()),
            value: None,
            conditions: None,
        };
        let ctx = HashMap::from([("key".to_string(), "value".to_string())]);
        assert!(evaluate_condition(&cond, &ctx));
        assert!(!evaluate_condition(&cond, &HashMap::new()));
    }

    #[test]
    fn test_and_condition() {
        let cond = Condition {
            operator: "and".to_string(),
            field: None,
            value: None,
            conditions: Some(vec![
                Condition {
                    operator: "equals".to_string(),
                    field: Some("a".to_string()),
                    value: Some("1".to_string()),
                    conditions: None,
                },
                Condition {
                    operator: "equals".to_string(),
                    field: Some("b".to_string()),
                    value: Some("2".to_string()),
                    conditions: None,
                },
            ]),
        };
        let ctx = HashMap::from([
            ("a".to_string(), "1".to_string()),
            ("b".to_string(), "2".to_string()),
        ]);
        assert!(evaluate_condition(&cond, &ctx));

        let ctx2 = HashMap::from([("a".to_string(), "1".to_string())]);
        assert!(!evaluate_condition(&cond, &ctx2));
    }

    #[test]
    fn test_or_condition() {
        let cond = Condition {
            operator: "or".to_string(),
            field: None,
            value: None,
            conditions: Some(vec![
                Condition {
                    operator: "equals".to_string(),
                    field: Some("a".to_string()),
                    value: Some("1".to_string()),
                    conditions: None,
                },
                Condition {
                    operator: "equals".to_string(),
                    field: Some("b".to_string()),
                    value: Some("2".to_string()),
                    conditions: None,
                },
            ]),
        };
        let ctx = HashMap::from([("a".to_string(), "1".to_string())]);
        assert!(evaluate_condition(&cond, &ctx));
    }

    #[test]
    fn test_should_execute_action_no_conditions() {
        let action = ConfigAction {
            name: "test".to_string(),
            conditions: None,
            ..Default::default()
        };
        assert!(should_execute_action(&action, &HashMap::new()));
    }

    #[test]
    fn test_should_execute_action_with_false_condition() {
        let action = ConfigAction {
            name: "test".to_string(),
            conditions: Some(Condition {
                operator: "equals".to_string(),
                field: Some("flag".to_string()),
                value: Some("true".to_string()),
                conditions: None,
            }),
            ..Default::default()
        };
        let ctx = HashMap::from([("flag".to_string(), "false".to_string())]);
        assert!(!should_execute_action(&action, &ctx));
    }
}
