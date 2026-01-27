# MCP Security Guidance

## Scope
MCP runs on localhost only and exposes handler tools. Use management tools carefully.

## Key Controls
- `mcp_enabled` per handler to limit exposed tools.
- `mcp_headless` to avoid interactive prompts in automated contexts.
- `mcp_seed_overwrite` only when needed.
- `management_tools_enabled` should remain false by default.

## Recommendations
- Limit MCP tool exposure to safe handlers.
- Avoid exposing handlers with destructive side effects.
- Validate and sanitize inputs used in database or API handlers.

## Source References
- MCP settings: [WpfInteractionApp/Settings/AppSettings.cs](WpfInteractionApp/Settings/AppSettings.cs)
- MCP registry: [WpfInteractionApp/Services/Mcp/McpToolRegistry.cs](WpfInteractionApp/Services/Mcp/McpToolRegistry.cs)