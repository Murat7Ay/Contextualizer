use crate::mcp::registry::McpToolDefinition;
use sysinfo::System;

pub fn system_tools() -> Vec<McpToolDefinition> {
    vec![
        McpToolDefinition {
            name: "system_info".to_string(),
            description: "Get system information (OS, CPU, memory)".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {}
            }),
        },
        McpToolDefinition {
            name: "list_processes".to_string(),
            description: "List running processes".to_string(),
            input_schema: serde_json::json!({
                "type": "object",
                "properties": {
                    "filter": { "type": "string", "description": "Filter by process name" }
                }
            }),
        },
    ]
}

pub fn execute_system_info() -> serde_json::Value {
    let mut sys = System::new_all();
    sys.refresh_all();

    serde_json::json!({
        "os": System::name().unwrap_or_default(),
        "os_version": System::os_version().unwrap_or_default(),
        "kernel_version": System::kernel_version().unwrap_or_default(),
        "hostname": System::host_name().unwrap_or_default(),
        "cpu_count": sys.cpus().len(),
        "total_memory_mb": sys.total_memory() / 1024 / 1024,
        "used_memory_mb": sys.used_memory() / 1024 / 1024,
        "total_swap_mb": sys.total_swap() / 1024 / 1024,
    })
}

pub fn execute_list_processes(filter: Option<&str>) -> serde_json::Value {
    let mut sys = System::new_all();
    sys.refresh_all();

    let processes: Vec<serde_json::Value> = sys
        .processes()
        .values()
        .filter(|p| {
            filter
                .map(|f| p.name().to_string_lossy().to_lowercase().contains(&f.to_lowercase()))
                .unwrap_or(true)
        })
        .take(100)
        .map(|p| {
            serde_json::json!({
                "pid": p.pid().as_u32(),
                "name": p.name().to_string_lossy(),
                "memory_kb": p.memory() / 1024,
                "cpu_usage": p.cpu_usage(),
            })
        })
        .collect();

    serde_json::json!({
        "count": processes.len(),
        "processes": processes,
    })
}

pub fn execute_http_request_sync(
    url: &str,
    method: &str,
) -> Result<serde_json::Value, String> {
    let method_upper = method.to_uppercase();
    if !["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD"].contains(&method_upper.as_str()) {
        return Err(format!("Unsupported HTTP method: {}", method));
    }

    Ok(serde_json::json!({
        "url": url,
        "method": method_upper,
        "status": "pending",
        "note": "Use async runtime for actual HTTP execution"
    }))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_system_info_returns_valid_json() {
        let info = execute_system_info();
        assert!(info["os"].is_string());
        assert!(info["cpu_count"].is_number());
        assert!(info["total_memory_mb"].is_number());
    }

    #[test]
    fn test_system_tools_registered() {
        let tools = system_tools();
        assert!(tools.iter().any(|t| t.name == "system_info"));
        assert!(tools.iter().any(|t| t.name == "list_processes"));
    }

    #[test]
    fn test_system_info_has_all_fields() {
        let info = execute_system_info();
        assert!(info["os_version"].is_string());
        assert!(info["kernel_version"].is_string());
        assert!(info["hostname"].is_string());
        assert!(info["used_memory_mb"].is_number());
        assert!(info["total_swap_mb"].is_number());
    }

    #[test]
    fn test_list_processes_returns_array() {
        let result = execute_list_processes(None);
        assert!(result["processes"].is_array());
        assert!(result["count"].is_number());
        let count = result["count"].as_u64().unwrap();
        assert!(count > 0, "Should find at least one process");
    }

    #[test]
    fn test_list_processes_with_filter() {
        // Filter for a process that should exist on any Windows system
        let result = execute_list_processes(Some("svchost"));
        let count = result["count"].as_u64().unwrap_or(0);
        // svchost may or may not be running in test env, just verify structure
        assert!(result["processes"].is_array());
        if count > 0 {
            let first = &result["processes"][0];
            assert!(first["pid"].is_number());
            assert!(first["name"].is_string());
        }
    }

    #[test]
    fn test_list_processes_empty_filter() {
        let result = execute_list_processes(Some("zzz_nonexistent_process_zzz"));
        assert_eq!(result["count"].as_u64().unwrap(), 0);
    }

    #[test]
    fn test_http_request_sync_valid_methods() {
        for method in &["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD"] {
            let result = execute_http_request_sync("http://example.com", method).unwrap();
            assert_eq!(result["method"].as_str().unwrap(), *method);
        }
    }

    #[test]
    fn test_http_request_sync_invalid_method() {
        let result = execute_http_request_sync("http://example.com", "INVALID");
        assert!(result.is_err());
        assert!(result.unwrap_err().contains("Unsupported HTTP method"));
    }

    #[test]
    fn test_http_request_sync_case_insensitive() {
        let result = execute_http_request_sync("http://example.com", "get").unwrap();
        assert_eq!(result["method"].as_str().unwrap(), "GET");
    }
}
