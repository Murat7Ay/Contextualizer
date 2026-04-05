pub mod system;
pub mod filesystem;

use super::registry::McpToolDefinition;

pub fn register_all_os_tools() -> Vec<McpToolDefinition> {
    let mut tools = Vec::new();
    tools.extend(system::system_tools());
    tools.extend(filesystem::filesystem_tools());
    tools
}
