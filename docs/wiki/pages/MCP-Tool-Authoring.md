# MCP Tool Authoring

This page describes how handlers become MCP tools and how to design MCP inputs/outputs.

## How Handlers Become Tools
- A handler becomes a tool when `mcp_enabled` is true.
- Tool name defaults to a slug of handler name unless `mcp_tool_name` is provided.
- Tool description uses `mcp_description` or falls back to handler description/title.

## Tool Input
- `mcp_input_schema` defines the JSON Schema for tool arguments.
- If omitted, default schema is derived from `user_inputs` or falls back to `{ text: string }`.
- `mcp_input_template` can turn arguments into `ClipboardContent.Text` using placeholders.

## Tool Output
- By default, MCP returns `_formatted_output`.
- `mcp_return_keys` can restrict output keys to a custom set.

## Headless Execution
- `mcp_headless: true` disables UI prompts and confirmations.
- Required inputs must be provided via tool arguments (or defaults).

## Source References
- MCP tool registry: [WpfInteractionApp/Services/Mcp/McpToolRegistry.cs](WpfInteractionApp/Services/Mcp/McpToolRegistry.cs)
- Tool executor: [WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs](WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs)
- Handler MCP config fields: [Contextualizer.PluginContracts/HandlerConfig.cs](Contextualizer.PluginContracts/HandlerConfig.cs)