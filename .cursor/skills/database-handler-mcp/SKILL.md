---
name: database-handler-mcp
description: Creates database handlers with MCP integration for Contextualizer. Handles SQL query configuration, parameter mapping, JSON Schema design, and MCP tool exposure. Use when creating database handlers, exposing database queries as MCP tools, or configuring database-to-MCP workflows.
---

# Database Handler with MCP Integration

## Quick Start

When creating a database handler with MCP:

1. Define handler configuration in `handlers.json`
2. Configure SQL query with parameterized inputs
3. Design MCP input schema (JSON Schema)
4. Map MCP arguments to query parameters
5. Configure output formatting
6. Test handler and MCP tool

## Handler Configuration Structure

### Basic Database Handler

```json
{
  "name": "Handler Name",
  "description": "Handler description",
  "type": "database",
  "enabled": true,
  "connectionString": "Server=...;Database=...;",
  "connector": "mssql",
  "query": "SELECT * FROM Table WHERE Column = @p_input",
  "regex": "^\\S+$",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### Required Fields

- `type`: Must be `"database"`
- `connectionString`: Database connection string (supports `$config:` placeholders)
- `connector`: Database type (`"mssql"` or `"plsql"`)
- `query`: SQL SELECT query with parameters

### Optional Fields

- `regex`: Pattern to match clipboard input (optional)
- `groups`: Named regex groups to extract as parameters
- `output_format`: Custom output template
- `command_timeout_seconds`: Query timeout
- `connection_timeout_seconds`: Connection timeout
- `max_pool_size` / `min_pool_size`: Connection pool settings
- `disable_pooling`: Disable connection pooling

## SQL Query Safety

**CRITICAL**: Only SELECT queries are allowed. The system blocks:
- INSERT, UPDATE, DELETE, DROP, ALTER, CREATE
- EXEC, EXECUTE, TRUNCATE, MERGE
- GRANT, REVOKE, SHUTDOWN
- SQL comments (`--`, `/* */`)
- Stored procedures (`xp_`, `sp_`)
- Semicolons (`;`)

**Query must start with `SELECT`** (case-insensitive).

## Parameter Mapping

### Standard Parameters

- `p_input`: Full clipboard text (or MCP input)
- `p_match`: Full regex match (if regex provided)
- `p_group_1`, `p_group_2`, ...: Unnamed regex groups (up to 20)
- Named groups: Use `groups` array to map regex groups to parameter names

### Parameter Prefixes by Connector

- **mssql**: `@p_input`, `@p_match`, `@group_name`
- **plsql**: `:p_input`, `:p_match`, `:group_name`

### Example Query with Parameters

```sql
-- Single input parameter
SELECT * FROM Users WHERE Email = @p_input

-- Multiple parameters from regex groups
SELECT * FROM Orders 
WHERE CustomerId = @p_group_1 
  AND OrderDate >= @p_group_2

-- Named groups (requires groups array in config)
SELECT * FROM Products 
WHERE Category = @category 
  AND Price <= @max_price
```

## Regex Configuration

### Simple Regex

```json
{
  "regex": "^\\d+$",
  "query": "SELECT * FROM Orders WHERE OrderId = @p_input"
}
```

### Named Groups

```json
{
  "regex": "^(?<category>\\w+)\\s+(?<max_price>\\d+)$",
  "groups": ["category", "max_price"],
  "query": "SELECT * FROM Products WHERE Category = @category AND Price <= @max_price"
}
```

### Unnamed Groups

```json
{
  "regex": "^(\\w+)\\s+(\\d+)$",
  "query": "SELECT * FROM Products WHERE Category = @p_group_1 AND Price <= @p_group_2"
}
```

**Note**: Unnamed groups are automatically mapped to `p_group_1`, `p_group_2`, etc. (max 20 groups).

## MCP Integration

### Enable MCP

```json
{
  "mcp_enabled": true,
  "mcp_tool_name": "unique_tool_name",
  "mcp_description": "Tool description for MCP clients",
  "mcp_headless": true
}
```

### MCP Input Schema

Define JSON Schema for tool arguments:

```json
{
  "mcp_input_schema": {
    "type": "object",
    "properties": {
      "email": {
        "type": "string",
        "description": "User email address"
      },
      "status": {
        "type": "string",
        "enum": ["active", "inactive", "pending"]
      }
    },
    "required": ["email"]
  }
}
```

### MCP Input Template

Map MCP arguments to clipboard input:

```json
{
  "mcp_input_template": "$(email)",
  "query": "SELECT * FROM Users WHERE Email = @p_input"
}
```

**Template placeholders**:
- `$(key)`: MCP argument value
- `$config:section.key`: Configuration value
- `$file:path`: File content
- `$func:function_name`: Function result

### MCP Seed Context

MCP arguments are available as seed context. Use `mcp_seed_overwrite: true` to allow MCP values to override handler-generated context:

```json
{
  "mcp_seed_overwrite": true,
  "mcp_input_schema": {
    "type": "object",
    "properties": {
      "customer_id": { "type": "string" },
      "order_date": { "type": "string" }
    }
  },
  "query": "SELECT * FROM Orders WHERE CustomerId = @customer_id AND OrderDate = @order_date"
}
```

### MCP Return Keys

Control what MCP returns:

```json
{
  "mcp_return_keys": ["_formatted_output", "customer_name", "order_total"]
}
```

**Default**: Returns `{ "_formatted_output": "..." }` if not specified.

## Output Formatting

### Default Format

If `output_format` is not specified, system generates a Markdown table from query results.

### Custom Template

```json
{
  "output_format": "Customer: $(customer_name#1)\nTotal: $(order_total#1)\nStatus: $(status#1)"
}
```

**Result keys**:
- `column_name#1`, `column_name#2`, ...: Row values
- `_count`: Number of rows returned
- `_formatted_output`: Formatted output string

### File Template

```json
{
  "output_format": "$file:C:\\Templates\\customer_report.txt"
}
```

## Complete Example

### Example 1: Simple User Lookup

```json
{
  "name": "User Lookup",
  "description": "Find user by email",
  "type": "database",
  "enabled": true,
  "connectionString": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
  "connector": "mssql",
  "query": "SELECT UserId, Name, Email, CreatedDate FROM Users WHERE Email = @p_input",
  "regex": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "requires_confirmation": false
    }
  ],
  "mcp_enabled": true,
  "mcp_tool_name": "user_lookup",
  "mcp_description": "Lookup user information by email address",
  "mcp_input_schema": {
    "type": "object",
    "properties": {
      "email": {
        "type": "string",
        "description": "User email address"
      }
    },
    "required": ["email"]
  },
  "mcp_input_template": "$(email)",
  "mcp_headless": true,
  "mcp_return_keys": ["_formatted_output"]
}
```

### Example 2: Multi-Parameter Query with Named Groups

```json
{
  "name": "Order Search",
  "description": "Find orders by customer and date range",
  "type": "database",
  "enabled": true,
  "connectionString": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
  "connector": "mssql",
  "regex": "^(?<customer_id>\\d+)\\s+(?<start_date>\\d{4}-\\d{2}-\\d{2})\\s+(?<end_date>\\d{4}-\\d{2}-\\d{2})$",
  "groups": ["customer_id", "start_date", "end_date"],
  "query": "SELECT OrderId, OrderDate, TotalAmount FROM Orders WHERE CustomerId = @customer_id AND OrderDate BETWEEN @start_date AND @end_date ORDER BY OrderDate DESC",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ],
  "mcp_enabled": true,
  "mcp_tool_name": "order_search",
  "mcp_description": "Search orders by customer ID and date range",
  "mcp_input_schema": {
    "type": "object",
    "properties": {
      "customer_id": {
        "type": "string",
        "description": "Customer ID"
      },
      "start_date": {
        "type": "string",
        "format": "date",
        "description": "Start date (YYYY-MM-DD)"
      },
      "end_date": {
        "type": "string",
        "format": "date",
        "description": "End date (YYYY-MM-DD)"
      }
    },
    "required": ["customer_id", "start_date", "end_date"]
  },
  "mcp_input_template": "$(customer_id) $(start_date) $(end_date)",
  "mcp_headless": true
}
```

### Example 3: MCP-Only (No Clipboard)

```json
{
  "name": "Product Catalog",
  "description": "Get product information",
  "type": "database",
  "enabled": true,
  "connectionString": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
  "connector": "mssql",
  "query": "SELECT ProductId, Name, Price, Stock FROM Products WHERE ProductId = @product_id",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ],
  "mcp_enabled": true,
  "mcp_tool_name": "get_product",
  "mcp_description": "Retrieve product details by ID",
  "mcp_input_schema": {
    "type": "object",
    "properties": {
      "product_id": {
        "type": "string",
        "description": "Product ID"
      }
    },
    "required": ["product_id"]
  },
  "mcp_seed_overwrite": true,
  "mcp_headless": true
}
```

## Connection String Configuration

Use `$config:` placeholders for dynamic configuration:

```json
{
  "connectionString": "Server=$config:database.server;Database=$config:database.name;Trusted_Connection=$config:database.trusted_con;TrustServerCertificate=$config:database.trusted_cert;"
}
```

## Testing Checklist

- [ ] Handler enabled (`enabled: true`)
- [ ] Connection string valid and accessible
- [ ] Query is SELECT-only and passes safety validation
- [ ] Parameters correctly mapped (`@p_input`, `@group_name`, etc.)
- [ ] Regex pattern matches test inputs (if regex provided)
- [ ] MCP schema matches expected arguments (if MCP enabled)
- [ ] MCP input template correctly maps arguments (if provided)
- [ ] Output format displays correctly
- [ ] Actions execute as expected
- [ ] MCP tool callable and returns expected format

## Common Issues

### Query Rejected

**Problem**: Query fails safety validation  
**Solution**: Ensure query starts with `SELECT` and contains no forbidden keywords

### Parameters Not Found

**Problem**: Query fails with "parameter not found"  
**Solution**: Check parameter names match (`@p_input` vs `@p_input`, `@group_name` vs `:group_name`)

### MCP Arguments Not Mapped

**Problem**: MCP arguments not reaching query  
**Solution**: Use `mcp_input_template` or `mcp_seed_overwrite: true` with direct parameter mapping

### No Results Returned

**Problem**: Query executes but returns no rows  
**Solution**: Check parameter values, query logic, and database data

## Best Practices

1. **Always use parameterized queries** - Never concatenate user input into SQL
2. **Validate input length** - System truncates to 4000 chars, but validate in schema
3. **Use descriptive MCP tool names** - Follow `snake_case` convention
4. **Provide clear MCP descriptions** - Help MCP clients understand tool purpose
5. **Set appropriate timeouts** - Use `command_timeout_seconds` for long-running queries
6. **Test regex patterns** - Verify regex matches expected inputs
7. **Use named groups** - Makes parameter mapping clearer than `p_group_1`, `p_group_2`
8. **Enable headless mode** - Set `mcp_headless: true` for automated MCP calls
9. **Specify return keys** - Use `mcp_return_keys` to control MCP output
10. **Document in description** - Include query purpose and expected inputs

## Reference Files

- Handler implementation: `Contextualizer.Core/DatabaseHandler.cs`
- Safety validator: `Contextualizer.Core/Handlers/Database/DatabaseSafetyValidator.cs`
- Parameter builder: `Contextualizer.Core/Handlers/Database/DatabaseParameterBuilder.cs`
- Query executor: `Contextualizer.Core/Handlers/Database/DatabaseQueryExecutor.cs`
- Handler config: `Contextualizer.PluginContracts/HandlerConfig.cs`
- MCP tool executor: `WpfInteractionApp/Services/Mcp/McpToolHandlers/HandlerToolExecutor.cs`
