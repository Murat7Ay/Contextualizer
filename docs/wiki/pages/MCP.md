# MCP

## MCP Server
- Host: [WpfInteractionApp/Services/McpServerHost.cs](WpfInteractionApp/Services/McpServerHost.cs)

## Endpoints
- GET /mcp/health → simple health check JSON
- POST /mcp → JSON-RPC (supports SSE when Accept: text/event-stream)

## JSON-RPC Methods
- initialize
- tools/list
- tools/call

## Tool Registry
- Built-in UI tools (confirm, user inputs, notify, show markdown)
- Handler-backed tools (per handler with MCP enabled)
- Management tools (optional, gated)

## Handler Tool Execution
- Request arguments are converted into a seed context.
- Clipboard input is synthesized from args or files.
- `Dispatch.ExecuteWithResultAsync` is used for handler-backed tools.

## Source References
- JSON-RPC handler: [WpfInteractionApp/Services/Mcp/McpJsonRpcHandler.cs](WpfInteractionApp/Services/Mcp/McpJsonRpcHandler.cs)
- Tool registry: [WpfInteractionApp/Services/Mcp/McpToolRegistry.cs](WpfInteractionApp/Services/Mcp/McpToolRegistry.cs)
- Tool execution: [WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs](WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs)

## Schemas
- Schema definitions: [WpfInteractionApp/Services/Mcp/McpSchemas/SchemaDefinitions.cs](WpfInteractionApp/Services/Mcp/McpSchemas/SchemaDefinitions.cs)
 - MCP schemas guide: [docs/wiki/pages/MCP-Schemas.md](docs/wiki/pages/MCP-Schemas.md)
 - MCP tool authoring: [docs/wiki/pages/MCP-Tool-Authoring.md](docs/wiki/pages/MCP-Tool-Authoring.md)
 - MCP security guidance: [docs/wiki/pages/MCP-Security.md](docs/wiki/pages/MCP-Security.md)
 - MCP management tools: [docs/wiki/pages/MCP-Management-Tools.md](docs/wiki/pages/MCP-Management-Tools.md)

## Default Schema Selection
- File handlers use a files array input schema.
- Handlers with `UserInputs` use generated user input schema.
- Otherwise, a default text schema is used.

## Tools & Handlers
- MCP handlers: [WpfInteractionApp/Services/Mcp](WpfInteractionApp/Services/Mcp)
