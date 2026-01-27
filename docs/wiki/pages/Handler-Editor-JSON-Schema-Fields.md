# Handler Editor JSON Schema Fields

This page documents the JSON field model used by the Handler Editor wizard and advanced JSON view in the UI. It reflects the TypeScript draft schema used for validation and defaults.

Source of truth in UI:
- Draft types and defaults: [Contextualizer.UI/src/app/lib/handlerSchemas.ts](Contextualizer.UI/src/app/lib/handlerSchemas.ts)
- Editor implementation: [Contextualizer.UI/src/app/components/screens/HandlerEditorDialog.tsx](Contextualizer.UI/src/app/components/screens/HandlerEditorDialog.tsx)

## Overview
The editor models a handler as `HandlerConfigDraft`, which mirrors the handler configuration stored in handlers.json. The wizard uses handler-specific schemas to show only the relevant fields, while the advanced JSON editor allows full control.

Note: UI validation is minimal. Full validation and execution rules are enforced by the host.

## HandlerConfigDraft Fields
### Core Fields (All Types)
- name (required)
- description
- type (required)
- screen_id
- title
- enabled
- requires_confirmation
- output_format
- seeder
- constant_seeder
- actions
- user_inputs
- auto_focus_tab
- bring_window_to_front

Source:
- Common fields: [Contextualizer.UI/src/app/lib/handlerSchemas.ts](Contextualizer.UI/src/app/lib/handlerSchemas.ts)

### Regex
- regex (required)
- groups (optional group names to extract)

### File
- file_extensions (required)

### Lookup
- path (required)
- delimiter (required)
- key_names (required)
- value_names (required)

### Database
- connector (required, e.g., mssql or plsql)
- connectionString (required)
- query (required, must be SELECT-only)
- command_timeout_seconds
- connection_timeout_seconds
- max_pool_size
- min_pool_size
- disable_pooling
- regex (optional)
- groups (optional)

### API
- url (required)
- method
- headers
- request_body
- content_type
- timeout_seconds
- regex (optional)
- groups (optional)
- http (advanced HTTP config; available in the editor)

### Custom
- validator
- context_provider

### Manual
- no type-specific fields

### Synthetic
- reference_handler (name of existing handler)
- actual_type (embedded handler type)
- synthetic_input (optional `UserInputDraft` for synthetic invocation)

### Cron
- cron_job_id (required)
- cron_expression (required)
- cron_timezone
- cron_enabled
- actual_type (required, embedded handler type)

### MCP (Optional)
- mcp_enabled
- mcp_tool_name
- mcp_description
- mcp_input_schema
- mcp_input_template
- mcp_return_keys
- mcp_headless
- mcp_seed_overwrite

## ConfigActionDraft
Actions are embedded in handler configuration and may include nested actions.

Fields:
- name (required)
- requires_confirmation
- key
- conditions (ConditionDraft)
- user_inputs (UserInputDraft[])
- seeder (Record<string, string>)
- constant_seeder (Record<string, string>)
- inner_actions (ConfigActionDraft[])

Source:
- Action draft: [Contextualizer.UI/src/app/lib/handlerSchemas.ts](Contextualizer.UI/src/app/lib/handlerSchemas.ts)

## ConditionDraft
Conditions define logical rules for actions.

Fields:
- operator (required)
- field
- value
- conditions (nested ConditionDraft[] for grouped logic)

Source:
- Condition draft: [Contextualizer.UI/src/app/lib/handlerSchemas.ts](Contextualizer.UI/src/app/lib/handlerSchemas.ts)

## UserInputDraft
User inputs describe prompts used by handlers and actions.

Fields:
- key, title, message
- validation_regex
- is_required
- is_selection_list
- selection_items (SelectionItemDraft[])
- is_multi_select
- is_password
- is_file_picker
- file_extensions
- is_folder_picker
- is_multi_line
- is_date / is_date_picker
- is_time / is_time_picker
- is_date_time / is_datetime_picker
- default_value
- dependent_key
- dependent_selection_item_map
- config_target

Source:
- User input draft: [Contextualizer.UI/src/app/lib/handlerSchemas.ts](Contextualizer.UI/src/app/lib/handlerSchemas.ts)

## Validation Rules in the UI
The editor applies lightweight validation before saving:
- name and type must be present.
- Custom handler requires validator or context_provider.
- Synthetic handler requires reference_handler or actual_type.
- If is_selection_list is true, selection_items is required.
- If is_multi_select is true, is_selection_list must be true.
- config_target must be in the form secrets.section.key or config.section.key.

Source:
- Validation logic: [Contextualizer.UI/src/app/lib/handlerSchemas.ts](Contextualizer.UI/src/app/lib/handlerSchemas.ts)

## Related Docs
- Handler Management UI: [docs/wiki/pages/UI-Handler-Management.md](docs/wiki/pages/UI-Handler-Management.md)
- Configuration: [docs/wiki/pages/Configuration.md](docs/wiki/pages/Configuration.md)
- Handler Authoring Checklist: [docs/wiki/pages/Handler-Authoring-Checklist.md](docs/wiki/pages/Handler-Authoring-Checklist.md)
