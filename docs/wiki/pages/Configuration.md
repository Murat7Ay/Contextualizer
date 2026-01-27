# Configuration

## Primary Configuration
- handlers.json defines handlers, actions, conditions, and templates.

## Schema Reference
- `HandlerConfig`: [Contextualizer.PluginContracts/HandlerConfig.cs](Contextualizer.PluginContracts/HandlerConfig.cs)
- `Condition`: [Contextualizer.PluginContracts/Condition.cs](Contextualizer.PluginContracts/Condition.cs)
- `UserInputRequest`: [Contextualizer.PluginContracts/UserInputRequest.cs](Contextualizer.PluginContracts/UserInputRequest.cs)

## HandlerConfig Field Groups
### Core
- name, description, type, screen_id, title
- regex, groups, actions, output_format
- seeder, constant_seeder, user_inputs
- enabled, requires_confirmation

### File / Lookup
- path, delimiter, key_names, value_names, file_extensions

### API
- url, method, headers, request_body, content_type, timeout_seconds
- http (advanced HTTP config)

### Database
- connectionString, query, connector
- command_timeout_seconds, connection_timeout_seconds
- max_pool_size, min_pool_size, disable_pooling

### Cron
- cron_job_id, cron_expression, cron_timezone, cron_enabled

### MCP
- mcp_enabled, mcp_tool_name, mcp_description
- mcp_input_schema, mcp_input_template, mcp_return_keys
- mcp_headless, mcp_seed_overwrite

### UI Behavior
- auto_focus_tab, bring_window_to_front

## Templates & Functions
- `FunctionProcessor`: [Contextualizer.Core/FunctionProcessor.cs](Contextualizer.Core/FunctionProcessor.cs)
- Detailed reference: [docs/wiki/pages/Function-Pipeline-Reference.md](docs/wiki/pages/Function-Pipeline-Reference.md)

## Dynamic Value Resolution
Dynamic tokens are resolved during handler and action execution:
- $(key) placeholders are replaced from context.
- $config:key pulls values from the configuration service.
- $file:path loads a file and then processes placeholders.
- $func:... runs function processing pipelines.

## User Input Flow
- User inputs are collected using navigation (Next/Back/Cancel).
- Dependent selection items are supported based on previous input.

## User Input Fields (Reference)
- key, title, message
- validation_regex, is_required
- is_selection_list, selection_items, is_multi_select
- is_password, is_multi_line
- is_file_picker, is_folder_picker, file_extensions
- is_date, is_time, is_date_time (aliases supported)
- default_value
- dependent_key, dependent_selection_item_map
- config_target

Source:
- User input schema: [Contextualizer.PluginContracts/UserInputRequest.cs](Contextualizer.PluginContracts/UserInputRequest.cs)

## Source References
- Dynamic replacement logic: [Contextualizer.Core/HandlerContextProcessor.cs](Contextualizer.Core/HandlerContextProcessor.cs)
- Function pipeline: [Contextualizer.Core/FunctionProcessor.cs](Contextualizer.Core/FunctionProcessor.cs)
- Function pipeline reference: [docs/wiki/pages/Function-Pipeline-Reference.md](docs/wiki/pages/Function-Pipeline-Reference.md)

## Example Configuration
- Default config: [handlers.json](handlers.json)
 - Examples: [docs/wiki/pages/Configuration-Examples.md](docs/wiki/pages/Configuration-Examples.md)
 - Recipes: [docs/wiki/pages/Configuration-Recipes.md](docs/wiki/pages/Configuration-Recipes.md)
 - Settings & config files: [docs/wiki/pages/Settings-Config-Files.md](docs/wiki/pages/Settings-Config-Files.md)
