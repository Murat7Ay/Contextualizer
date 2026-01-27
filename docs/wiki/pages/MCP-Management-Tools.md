# MCP Management Tools

This page describes the optional MCP management tools for handlers, plugins, and configuration.

## Available Tools (when enabled)
- handlers_list / handlers_get
- handler_create / handler_update / handler_delete / handler_reload
- plugins_list
- config_get_keys / config_get_section / config_set_value / config_reload
- handler_docs

## Behavior Notes
- Management tools are gated by `management_tools_enabled`.
- Config section values are masked for sensitive sections and keys.

## Source References
- MCP management handler: [WpfInteractionApp/Services/Mcp/McpToolHandlers/ManagementToolHandler.cs](WpfInteractionApp/Services/Mcp/McpToolHandlers/ManagementToolHandler.cs)
- MCP settings: [WpfInteractionApp/Settings/AppSettings.cs](WpfInteractionApp/Settings/AppSettings.cs)