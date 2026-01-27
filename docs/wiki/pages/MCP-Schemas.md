# MCP Schemas

This page explains how MCP tool input schemas are generated and how to author custom schemas.

## Source References
- Schema builder: [WpfInteractionApp/Services/Mcp/McpSchemas/SchemaBuilder.cs](WpfInteractionApp/Services/Mcp/McpSchemas/SchemaBuilder.cs)
- Default selection: [WpfInteractionApp/Services/Mcp/McpSchemas/SchemaDefinitions.cs](WpfInteractionApp/Services/Mcp/McpSchemas/SchemaDefinitions.cs)

## Default Schema Selection Rules
- File handlers use a files array schema.
- Handlers with `user_inputs` use a derived schema built from those inputs.
- Everything else defaults to a simple text schema.

## Built-in Schemas
- UI confirm schema
- UI user inputs schema
- UI notify schema
- UI show markdown schema
- Default text schema
- Files schema

## Management Tool Schemas
The management tools are defined as schemas for:
- handlers_list
- handlers_get
- handler_create / handler_update / handler_delete / handler_reload
- config_get_section / config_set_value
- database_tool_create

## Custom Handler Schema
You can override the default schema using `mcp_input_schema` inside handler configuration.
This should be a JSON Schema object compatible with MCP tool input requirements.