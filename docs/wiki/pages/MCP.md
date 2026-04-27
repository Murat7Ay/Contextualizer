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
- Built-in shell execution tool (`run_shell`)
- Optional built-in generic data tools (`db_statements_list`, `db_statement_get`, `db_select_statement`, `db_scalar`, `db_execute`, `db_procedure_execute`) when **Enable Generic Data Tools** is enabled in MCP settings
- Config-driven raw SQL tools from `[mcp_raw_sql_tools]`
- Registry-backed direct data tools (published from `data-tools.json`)
- Handler-backed tools (per handler with MCP enabled)
- Management tools (optional, gated)

The generic data tools flag is stored in the normal app settings and is exposed in **Settings → Advanced → MCP HTTP Server** as a restart-required toggle.

## Handler Tool Execution
- Request arguments are converted into a seed context.
- Clipboard input is synthesized from args or files.
- `Dispatch.ExecuteWithResultAsync` is used for handler-backed tools.

## Data Tool Execution
- Generic built-in data tools resolve definitions by `id` from the data-tools registry when enabled.
- Direct data tools resolve by tool name for enabled definitions with `expose_as_tool: true`.
- Config-driven raw SQL tools resolve to a fixed configured provider + connection and accept runtime SQL text.
- Raw SQL tool descriptions can now be authored explicitly from the UI, and single-mode tools omit the `mode` argument from the MCP schema.
- Current execution support is relational (`mssql`, `plsql`); the registry model is provider-oriented for future extensions.
- Data tool statements are executed outside the handler pipeline and return structured JSON payloads directly.

## Source References
- JSON-RPC handler: [WpfInteractionApp/Services/Mcp/McpJsonRpcHandler.cs](WpfInteractionApp/Services/Mcp/McpJsonRpcHandler.cs)
- Tool registry: [WpfInteractionApp/Services/Mcp/McpToolRegistry.cs](WpfInteractionApp/Services/Mcp/McpToolRegistry.cs)
- Tool execution: [WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs](WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs)
- Data tool execution: [WpfInteractionApp/Services/Mcp/McpToolHandlers/DataToolToolHandler.cs](WpfInteractionApp/Services/Mcp/McpToolHandlers/DataToolToolHandler.cs)
- Data tool registry: [Contextualizer.Core/Services/DataTools/DataToolRegistryService.cs](Contextualizer.Core/Services/DataTools/DataToolRegistryService.cs)

## Schemas
- Schema definitions: [WpfInteractionApp/Services/Mcp/McpSchemas/SchemaDefinitions.cs](WpfInteractionApp/Services/Mcp/McpSchemas/SchemaDefinitions.cs)
 - MCP schemas guide: [docs/wiki/pages/MCP-Schemas.md](docs/wiki/pages/MCP-Schemas.md)
 - MCP tool authoring: [docs/wiki/pages/MCP-Tool-Authoring.md](docs/wiki/pages/MCP-Tool-Authoring.md)
 - MCP security guidance: [docs/wiki/pages/MCP-Security.md](docs/wiki/pages/MCP-Security.md)
 - MCP management tools: [docs/wiki/pages/MCP-Management-Tools.md](docs/wiki/pages/MCP-Management-Tools.md)
 - Data tools guide: [docs/wiki/pages/Data-Tools.md](docs/wiki/pages/Data-Tools.md)

## Default Schema Selection
- File handlers use a files array input schema.
- Handlers with `UserInputs` use generated user input schema.
- Otherwise, a default text schema is used.

## Tools & Handlers
- MCP handlers: [WpfInteractionApp/Services/Mcp](WpfInteractionApp/Services/Mcp)
