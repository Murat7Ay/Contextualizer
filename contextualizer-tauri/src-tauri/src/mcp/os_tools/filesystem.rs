use crate::mcp::registry::McpToolDefinition;
use std::fs;

pub fn filesystem_tools() -> Vec<McpToolDefinition> {
    vec![
        McpToolDefinition {
            name: "read_file".to_string(),
            description: "Read a file's content".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "path": { "type": "string", "description": "File path to read" }
                },
                "required": ["path"]
            }),
        },
        McpToolDefinition {
            name: "write_file".to_string(),
            description: "Write content to a file".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "path": { "type": "string", "description": "File path to write" },
                    "content": { "type": "string", "description": "Content to write" }
                },
                "required": ["path", "content"]
            }),
        },
        McpToolDefinition {
            name: "list_directory".to_string(),
            description: "List files in a directory".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "path": { "type": "string", "description": "Directory path" }
                },
                "required": ["path"]
            }),
        },
        McpToolDefinition {
            name: "search_files".to_string(),
            description: "Search for files matching a regex pattern".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "root": { "type": "string", "description": "Root directory to search" },
                    "pattern": { "type": "string", "description": "Regex pattern to match filenames" },
                    "max_results": { "type": "integer", "description": "Maximum results to return" }
                },
                "required": ["root", "pattern"]
            }),
        },
        McpToolDefinition {
            name: "file_info".to_string(),
            description: "Get file metadata (size, type, permissions)".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "path": { "type": "string", "description": "File path" }
                },
                "required": ["path"]
            }),
        },
        McpToolDefinition {
            name: "run_command".to_string(),
            description: "Execute a shell command".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "command": { "type": "string" },
                    "args": { "type": "array", "items": { "type": "string" } },
                    "cwd": { "type": "string" },
                    "timeout_ms": { "type": "integer" }
                },
                "required": ["command"]
            }),
        },
    ]
}

pub fn execute_read_file(path: &str) -> Result<String, String> {
    fs::read_to_string(path).map_err(|e| format!("Failed to read file: {}", e))
}

pub fn execute_write_file(path: &str, content: &str) -> Result<String, String> {
    fs::write(path, content).map_err(|e| format!("Failed to write file: {}", e))?;
    Ok(format!("Written {} bytes to {}", content.len(), path))
}

pub fn execute_list_directory(path: &str) -> Result<String, String> {
    let entries: Vec<String> = fs::read_dir(path)
        .map_err(|e| format!("Failed to read directory: {}", e))?
        .filter_map(|entry| {
            entry.ok().map(|e| {
                let file_type = if e.path().is_dir() { "dir" } else { "file" };
                format!("{} [{}]", e.file_name().to_string_lossy(), file_type)
            })
        })
        .collect();
    Ok(entries.join("\n"))
}

pub fn execute_search_files(
    root: &str,
    pattern: &str,
    max_results: Option<usize>,
) -> Result<Vec<String>, String> {
    let regex = regex::Regex::new(pattern)
        .map_err(|e| format!("Invalid search pattern: {}", e))?;
    let limit = max_results.unwrap_or(100);
    let mut results = Vec::new();

    for entry in walkdir::WalkDir::new(root)
        .max_depth(10)
        .into_iter()
        .filter_map(|e| e.ok())
    {
        if results.len() >= limit {
            break;
        }
        let name = entry.file_name().to_string_lossy();
        if regex.is_match(&name) {
            results.push(entry.path().to_string_lossy().to_string());
        }
    }

    Ok(results)
}

pub fn execute_file_info(path: &str) -> Result<serde_json::Value, String> {
    let metadata = fs::metadata(path)
        .map_err(|e| format!("Failed to get file info: {}", e))?;

    Ok(serde_json::json!({
        "path": path,
        "size_bytes": metadata.len(),
        "is_file": metadata.is_file(),
        "is_dir": metadata.is_dir(),
        "readonly": metadata.permissions().readonly(),
    }))
}

pub async fn execute_run_command(
    command: &str,
    args: &[String],
    cwd: Option<&str>,
    timeout_ms: Option<u64>,
) -> Result<String, String> {
    let mut cmd = tokio::process::Command::new(command);
    cmd.args(args);
    if let Some(dir) = cwd {
        cmd.current_dir(dir);
    }

    let timeout = std::time::Duration::from_millis(timeout_ms.unwrap_or(30000));
    let output = tokio::time::timeout(timeout, cmd.output())
        .await
        .map_err(|_| "Command timed out".to_string())?
        .map_err(|e| format!("Failed to execute command: {}", e))?;

    let stdout = String::from_utf8_lossy(&output.stdout);
    let stderr = String::from_utf8_lossy(&output.stderr);

    if output.status.success() {
        Ok(stdout.to_string())
    } else {
        Err(format!("Command failed (exit {}): {}", output.status, stderr))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_list_directory() {
        let tmp = tempfile::tempdir().unwrap();
        fs::write(tmp.path().join("a.txt"), "").unwrap();
        let result = execute_list_directory(tmp.path().to_str().unwrap()).unwrap();
        assert!(result.contains("a.txt"));
    }

    #[test]
    fn test_read_file() {
        let tmp = tempfile::NamedTempFile::new().unwrap();
        fs::write(tmp.path(), "content123").unwrap();
        let result = execute_read_file(tmp.path().to_str().unwrap()).unwrap();
        assert!(result.contains("content123"));
    }

    #[test]
    fn test_write_read_roundtrip() {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("test.txt");
        let path_str = path.to_str().unwrap();
        execute_write_file(path_str, "hello").unwrap();
        let result = execute_read_file(path_str).unwrap();
        assert_eq!(result, "hello");
    }

    #[tokio::test]
    async fn test_run_command_success() {
        let result = execute_run_command("cmd", &["/C".to_string(), "echo hello".to_string()], None, None).await;
        assert!(result.is_ok());
        assert!(result.unwrap().contains("hello"));
    }

    #[tokio::test]
    async fn test_run_command_timeout() {
        let result = execute_run_command(
            "cmd",
            &["/C".to_string(), "timeout /t 60 /nobreak".to_string()],
            None,
            Some(100),
        )
        .await;
        assert!(result.is_err());
    }

    #[test]
    fn test_filesystem_tools_registered() {
        let tools = filesystem_tools();
        let names: Vec<&str> = tools.iter().map(|t| t.name.as_str()).collect();
        assert!(names.contains(&"read_file"));
        assert!(names.contains(&"write_file"));
        assert!(names.contains(&"list_directory"));
        assert!(names.contains(&"run_command"));
        assert!(names.contains(&"search_files"));
        assert!(names.contains(&"file_info"));
    }

    #[test]
    fn test_read_nonexistent_file() {
        let result = execute_read_file("/nonexistent/path/file.txt");
        assert!(result.is_err());
        assert!(result.unwrap_err().contains("Failed to read file"));
    }

    #[test]
    fn test_write_to_readonly_parent_fails() {
        let result = execute_write_file("/nonexistent/dir/file.txt", "data");
        assert!(result.is_err());
    }

    #[test]
    fn test_list_nonexistent_directory() {
        let result = execute_list_directory("/nonexistent/dir");
        assert!(result.is_err());
        assert!(result.unwrap_err().contains("Failed to read directory"));
    }

    #[test]
    fn test_search_files_finds_matches() {
        let tmp = tempfile::tempdir().unwrap();
        fs::write(tmp.path().join("report.csv"), "").unwrap();
        fs::write(tmp.path().join("data.csv"), "").unwrap();
        fs::write(tmp.path().join("readme.md"), "").unwrap();

        let results = execute_search_files(tmp.path().to_str().unwrap(), r"\.csv$", None).unwrap();
        assert_eq!(results.len(), 2);
    }

    #[test]
    fn test_search_files_respects_limit() {
        let tmp = tempfile::tempdir().unwrap();
        for i in 0..10 {
            fs::write(tmp.path().join(format!("file{}.txt", i)), "").unwrap();
        }
        let results = execute_search_files(tmp.path().to_str().unwrap(), r"\.txt$", Some(3)).unwrap();
        assert_eq!(results.len(), 3);
    }

    #[test]
    fn test_search_files_invalid_regex() {
        let result = execute_search_files(".", "[invalid", None);
        assert!(result.is_err());
        assert!(result.unwrap_err().contains("Invalid search pattern"));
    }

    #[test]
    fn test_file_info_returns_metadata() {
        let tmp = tempfile::NamedTempFile::new().unwrap();
        fs::write(tmp.path(), "hello world").unwrap();
        let info = execute_file_info(tmp.path().to_str().unwrap()).unwrap();
        assert!(info["is_file"].as_bool().unwrap());
        assert!(!info["is_dir"].as_bool().unwrap());
        assert_eq!(info["size_bytes"].as_u64().unwrap(), 11);
    }

    #[test]
    fn test_file_info_nonexistent() {
        let result = execute_file_info("/nonexistent/file.txt");
        assert!(result.is_err());
    }

    #[test]
    fn test_write_empty_content() {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("empty.txt");
        let path_str = path.to_str().unwrap();
        execute_write_file(path_str, "").unwrap();
        let content = execute_read_file(path_str).unwrap();
        assert!(content.is_empty());
    }

    #[test]
    fn test_write_unicode_content() {
        let tmp = tempfile::tempdir().unwrap();
        let path = tmp.path().join("unicode.txt");
        let path_str = path.to_str().unwrap();
        let unicode = "Merhaba dünya! 🌍 日本語テスト";
        execute_write_file(path_str, unicode).unwrap();
        let content = execute_read_file(path_str).unwrap();
        assert_eq!(content, unicode);
    }

    #[tokio::test]
    async fn test_run_command_with_cwd() {
        let tmp = tempfile::tempdir().unwrap();
        let result = execute_run_command(
            "cmd",
            &["/C".to_string(), "cd".to_string()],
            Some(tmp.path().to_str().unwrap()),
            None,
        )
        .await;
        assert!(result.is_ok());
    }

    #[tokio::test]
    async fn test_run_command_nonexistent() {
        let result = execute_run_command("nonexistent_command_xyz", &[], None, None).await;
        assert!(result.is_err());
    }
}
