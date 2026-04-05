use std::collections::HashSet;

#[derive(Debug, Clone, PartialEq, Eq, Hash)]
pub enum Modifier {
    Control,
    Alt,
    Shift,
    Super,
}

#[derive(Debug, Clone)]
pub struct ShortcutDef {
    pub modifiers: HashSet<Modifier>,
    pub key: String,
}

pub fn parse_shortcut(shortcut_str: &str) -> Result<ShortcutDef, String> {
    let parts: Vec<&str> = shortcut_str.split('+').collect();
    if parts.is_empty() {
        return Err("Empty shortcut string".to_string());
    }

    let mut modifiers = HashSet::new();
    let mut key = String::new();

    for (i, part) in parts.iter().enumerate() {
        let normalized = part.trim().to_lowercase();
        if i == parts.len() - 1 && !is_modifier(&normalized) {
            key = part.trim().to_string();
        } else {
            match normalized.as_str() {
                "ctrl" | "control" | "commandorcontrol" => {
                    modifiers.insert(Modifier::Control);
                }
                "alt" | "option" => {
                    modifiers.insert(Modifier::Alt);
                }
                "shift" => {
                    modifiers.insert(Modifier::Shift);
                }
                "super" | "win" | "cmd" | "command" | "meta" => {
                    modifiers.insert(Modifier::Super);
                }
                _ => {
                    if i == parts.len() - 1 {
                        key = part.trim().to_string();
                    } else {
                        return Err(format!("Unknown modifier: {}", part.trim()));
                    }
                }
            }
        }
    }

    if key.is_empty() {
        return Err("No key specified in shortcut".to_string());
    }

    Ok(ShortcutDef { modifiers, key })
}

fn is_modifier(s: &str) -> bool {
    matches!(
        s,
        "ctrl"
            | "control"
            | "commandorcontrol"
            | "alt"
            | "option"
            | "shift"
            | "super"
            | "win"
            | "cmd"
            | "command"
            | "meta"
    )
}

pub fn to_tauri_shortcut_string(def: &ShortcutDef) -> String {
    let mut parts = Vec::new();
    if def.modifiers.contains(&Modifier::Control) {
        parts.push("CommandOrControl");
    }
    if def.modifiers.contains(&Modifier::Alt) {
        parts.push("Alt");
    }
    if def.modifiers.contains(&Modifier::Shift) {
        parts.push("Shift");
    }
    if def.modifiers.contains(&Modifier::Super) {
        parts.push("Super");
    }
    parts.push(&def.key);
    parts.join("+")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_shortcut_string() {
        let shortcut = parse_shortcut("CommandOrControl+Alt+W").unwrap();
        assert!(shortcut.modifiers.contains(&Modifier::Control));
        assert!(shortcut.modifiers.contains(&Modifier::Alt));
        assert_eq!(shortcut.key, "W");
    }

    #[test]
    fn test_parse_shortcut_ctrl_shift() {
        let shortcut = parse_shortcut("Ctrl+Shift+C").unwrap();
        assert!(shortcut.modifiers.contains(&Modifier::Control));
        assert!(shortcut.modifiers.contains(&Modifier::Shift));
        assert_eq!(shortcut.key, "C");
    }

    #[test]
    fn test_invalid_shortcut_empty() {
        assert!(parse_shortcut("").is_err());
    }

    #[test]
    fn test_single_key_no_modifier() {
        let shortcut = parse_shortcut("F1").unwrap();
        assert!(shortcut.modifiers.is_empty());
        assert_eq!(shortcut.key, "F1");
    }

    #[test]
    fn test_to_tauri_shortcut_string() {
        let def = ShortcutDef {
            modifiers: HashSet::from([Modifier::Control, Modifier::Alt]),
            key: "W".to_string(),
        };
        let s = to_tauri_shortcut_string(&def);
        assert!(s.contains("CommandOrControl"));
        assert!(s.contains("Alt"));
        assert!(s.contains("W"));
    }
}
