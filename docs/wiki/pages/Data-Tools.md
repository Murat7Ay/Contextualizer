# Data Tools

## Purpose
Data tools are registry-backed MCP tools for parameterized data access. They are intended for MCP/LLM scenarios where the model should call a narrow, named operation instead of generating long SQL text.

Key goals:
- keep SQL and procedure names in a controlled registry
- expose discoverable MCP tools with stable names
- support both generic invocation and direct first-class tool names
- leave room for future providers beyond relational databases

Initial execution support is currently relational:
- `mssql`
- `plsql`

The registry and tool model are provider-oriented so future adapters can be added for systems such as Neo4j, Redis, or Elasticsearch without redesigning the MCP surface.

## Registry File
- Setting: `mcp_settings.data_tools_registry_path`
- Default path: `C:\PortableApps\Contextualizer\Config\data-tools.json`
- Behavior: the file is auto-created on first run with disabled sample definitions

Registry shape:

```json
{
  "definitions": [
    {
      "id": "customer_by_code",
      "name": "Customer By Code",
      "tool_name": "get_customer_by_code",
      "description": "Read customer summary by institution code.",
      "provider": "mssql",
      "operation": "select",
      "connection": "$config:connections.main_mssql",
      "statement": "SELECT TOP 10 customer_code, customer_name FROM dbo.customers WHERE customer_code = @institution_code",
      "enabled": true,
      "expose_as_tool": true,
      "parameters": [
        {
          "name": "institution_code",
          "db_parameter_name": "institution_code",
          "type": "string",
          "description": "Institution code to search for",
          "required": true
        }
      ],
      "tags": ["customer", "mssql"],
      "result": {
        "max_rows": 50,
        "include_execution_metadata": true
      }
    }
  ]
}
```

## Definition Fields
- `id`: required stable identifier used by generic tools such as `db_select_statement`
- `name`: optional human-readable label
- `tool_name`: optional direct MCP tool name; if omitted, the runtime slugifies `name`, then `id`
- `description`: optional MCP tool description
- `provider`: required provider key such as `mssql` or `plsql`
- `operation`: required operation kind: `select`, `scalar`, `execute`, or `procedure`
- `connection`: required connection string or config-backed template; config placeholders are resolved through the same runtime placeholder engine used elsewhere
- `statement`: required for `select`, `scalar`, and `execute`
- `procedure_name`: required for `procedure`
- `enabled`: allows soft-disable without deleting the definition
- `expose_as_tool`: when `true`, supported enabled definitions are published as first-class MCP tools
- `parameters`: optional parameter metadata used for validation, schema generation, defaults, output parameters, and stored procedure directions
- `input_schema`: optional explicit JSON schema override for the direct MCP tool
- `tags`: optional discovery tags used by `db_statements_list`
- `result`: controls row limits, metadata, output parameters, and procedure result mode
- `provider_options`: reserved for provider-specific extensions

## Parameter Metadata
Each parameter definition can include:
- `name`: public argument name used by MCP callers
- `db_parameter_name`: optional database parameter name; if omitted, `name` is used
- `type`: schema/data hint such as `string`, `integer`, `number`, `boolean`, `array`, or `object`
- `required`: marks required input parameters
- `default_value`: optional default used when the caller omits the value
- `enum`: optional allowed values list for schema generation
- `array_item_type`: item type when `type` is `array`
- `direction`: `input`, `output`, or `input_output`
- `db_type`: optional Dapper/ADO db type hint such as `string`, `int32`, `decimal`, `datetime`
- `serialize_as_json`: serializes complex input into JSON before sending it to the database parameter

## Built-in Generic MCP Tools
These tools are always registered when the MCP server is enabled.

- `db_statements_list`
  - Lists enabled registry definitions
  - Supports optional filters: `provider`, `operation`, `tag`, `search`
- `db_statement_get`
  - Returns a single definition by `id`
  - Includes generated or overridden input schema
- `db_select_statement`
  - Executes a `select` definition by `statement_id`
- `db_scalar`
  - Executes a `scalar` definition by `statement_id`
- `db_execute`
  - Executes an `execute` definition by `statement_id`
  - Intended for parameterized `insert`, `update`, `delete`, and `merge`
- `db_procedure_execute`
  - Executes a `procedure` definition by `procedure_id`

Generic invocation accepts either a nested `arguments` object or flat top-level parameters.

Example generic select call:

```json
{
  "statement_id": "customer_by_code",
  "arguments": {
    "institution_code": "123456"
  }
}
```

Flat form also works:

```json
{
  "statement_id": "customer_by_code",
  "institution_code": "123456"
}
```

## Dynamic First-Class Tools
When a definition is:
- `enabled: true`
- `expose_as_tool: true`
- supported by the current execution layer

it is also published directly as an MCP tool.

For the example above, `tools/list` will also include:

```text
get_customer_by_code
```

Direct tools accept their parameters directly at the top level:

```json
{
  "institution_code": "123456"
}
```

Schema behavior:
- if `input_schema` is provided, that schema is used as-is
- otherwise the runtime builds a JSON schema from `parameters`
- output-only parameters are excluded from the input schema
- if a direct data-tool name collides with a handler MCP tool name, the handler tool keeps precedence

## Result Shapes
Default payloads depend on the operation:

- `select`
  - `rows`
  - `row_count_total`
  - `row_count_returned`
  - `truncated`
- `scalar`
  - `value`
- `execute`
  - `affected_rows`
- `procedure`
  - depends on `result.mode`
  - `select` mode returns rows
  - `scalar` mode returns `value` plus `affected_rows`
  - other modes return `affected_rows`

When enabled, execution metadata is appended:
- `tool_id`
- `operation`
- `provider`
- `elapsed_ms`

When enabled and available, output parameters are returned under:
- `output_parameters`

## Safety Model
Current relational execution rules are intentionally narrow:

- `select` and `scalar`
  - statement must start with `select` or `with`
- `execute`
  - statement must start with `insert`, `update`, `delete`, or `merge`
- forbidden content is blocked across statement execution:
  - semicolons
  - SQL comments (`--`, `/*`, `*/`)
  - `drop`, `alter`, `create`, `truncate`
  - `exec`, `execute`, `xp_`, `sp_`
  - `grant`, `revoke`, `shutdown`

Stored procedures are only executed through explicit `procedure` definitions. That keeps procedure names and parameter directions under registry control.

## Procedure Example

```json
{
  "id": "approve_customer",
  "name": "Approve Customer",
  "tool_name": "approve_customer",
  "provider": "plsql",
  "operation": "procedure",
  "connection": "$config:connections.main_oracle",
  "procedure_name": "pkg_customer.approve_customer",
  "enabled": true,
  "expose_as_tool": true,
  "parameters": [
    {
      "name": "customer_code",
      "db_parameter_name": "p_customer_code",
      "type": "string",
      "required": true
    },
    {
      "name": "status",
      "db_parameter_name": "p_status",
      "type": "string",
      "direction": "output"
    }
  ],
  "result": {
    "mode": "execute",
    "include_output_parameters": true,
    "include_execution_metadata": true
  }
}
```

## Quick Start

1. Open **Settings → MCP** and note the value of **Data Tools Registry Path** (default: `C:\PortableApps\Contextualizer\Config\data-tools.json`).  
   The file is created automatically on first run with two disabled sample definitions.
2. Open **Settings → Config / Secrets** and add your connection string to the `[connections]` section of your `secrets.ini`:
   ```ini
   [connections]
   main_mssql = Server=localhost\SQLEXPRESS;Database=MyDB;User Id=sa;Password=***;TrustServerCertificate=True;
   ```
3. Navigate to the **Data Tools** screen (toolbar button or tab). The screen lists all registry definitions.
4. Click **New** and fill in:
   - **Id** — a stable identifier such as `customer_by_code`
   - **Provider** — use the preset button `mssql` (or `plsql`)
   - **Operation** — `select`, `scalar`, `execute`, or `procedure`
   - **Connection** — click the quick-pick button for your `$config:connections.*` key (it appears automatically from your secrets file) or type the reference manually
   - **Statement** — the parameterized SQL, e.g. `SELECT TOP 10 * FROM dbo.Customers WHERE Code = @code`
   - Enable **Enabled** and **Expose As Tool** to publish the tool to MCP clients
5. Click **Save**. The registry file is updated immediately and the MCP server re-registers the new tool.

## Viewing Registered Tools

### From MCP clients (Claude Desktop, Cursor, etc.)

When Contextualizer's MCP server is running (`mcp_settings.enabled = true`), data tools appear alongside handler tools. Two discovery methods are available:

**1. Ask the built-in `db_statements_list` tool**

```json
{
  "name": "db_statements_list",
  "arguments": {}
}
```

Optional filters: `provider`, `operation`, `tag`, `search`.

Example — list only MSSQL select tools:
```json
{
  "name": "db_statements_list",
  "arguments": { "provider": "mssql", "operation": "select" }
}
```

**2. `tools/list` MCP call**

Each definition with `enabled: true` and `expose_as_tool: true` is also published as a first-class tool. A `tools/list` response will include it:

```json
{
  "name": "get_customer_by_code",
  "description": "Read customer summary by institution code.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "institution_code": { "type": "string", "description": "Institution code to search for" }
    },
    "required": ["institution_code"]
  }
}
```

Calling it directly:
```json
{
  "name": "get_customer_by_code",
  "arguments": { "institution_code": "123456" }
}
```

**3. Inspect a single definition with `db_statement_get`**

```json
{
  "name": "db_statement_get",
  "arguments": { "id": "customer_by_code" }
}
```

Returns the full definition including the generated input schema.

### From the in-app Data Tools screen

Open the **Data Tools** screen from the toolbar. It shows:
- Total / enabled / exposed / supported counts as stats cards
- A searchable, filterable list of all registry definitions
- Provider and operation badges on each item
- Tool name that MCP clients will use to call the tool
- A "Supported today / Stored for future provider" status indicator on each editor

The **Runtime Status** field in the editor tells you whether the current execution layer can run the selected provider. Currently `mssql` and `plsql` are executable; other providers can be stored for future runtime support.

## Relationship To Database Handler
The existing Database handler remains useful for handler-pipeline scenarios:
- select-only queries
- clipboard-driven extraction and formatting
- handler actions, UI output, and existing seeder logic

Data tools target a different problem:
- MCP-native parameterized operations
- discoverable named tools for LLMs and agents
- registry-controlled read, scalar, write, and procedure access

Use Database handlers when the operation belongs naturally inside the handler pipeline. Use data tools when the better abstraction is a named MCP operation.

## Source References
- Registry service: [Contextualizer.Core/Services/DataTools/DataToolRegistryService.cs](Contextualizer.Core/Services/DataTools/DataToolRegistryService.cs)
- Execution service: [Contextualizer.Core/Services/DataTools/DataToolExecutionService.cs](Contextualizer.Core/Services/DataTools/DataToolExecutionService.cs)
- Models: [Contextualizer.Core/Services/DataTools/DataToolModels.cs](Contextualizer.Core/Services/DataTools/DataToolModels.cs)
- MCP handler: [WpfInteractionApp/Services/Mcp/McpToolHandlers/DataToolToolHandler.cs](WpfInteractionApp/Services/Mcp/McpToolHandlers/DataToolToolHandler.cs)
- MCP schemas: [WpfInteractionApp/Services/Mcp/McpSchemas/DataToolSchemas.cs](WpfInteractionApp/Services/Mcp/McpSchemas/DataToolSchemas.cs)

## Related
- MCP overview: [docs/wiki/pages/MCP.md](docs/wiki/pages/MCP.md)
- Database handler: [docs/wiki/pages/Database-Handler.md](docs/wiki/pages/Database-Handler.md)
- Settings and config files: [docs/wiki/pages/Settings-Config-Files.md](docs/wiki/pages/Settings-Config-Files.md)