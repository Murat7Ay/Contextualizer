# Handler Authoring Checklist

## 1) Define the Handler
- Pick a `type` that maps to a built-in handler or plugin.
- Set `name`, `description`, and `title`.
- Choose `screen_id` for UI output (if using `show_window`).

## 2) Configure Matching
- Regex handlers: `regex`, `groups`.
- File handlers: `file_extensions`.
- Lookup handlers: `path`, `delimiter`, `key_names`, `value_names`.

## 3) Provide Outputs
- `output_format` for formatted output.
- Use templates: $(key), $config:, $file:, $func:.

## 4) Add Actions
- Typical: `show_window`, `show_notification`, `copytoclipboard`.
- Add `conditions` to gate actions.

## 5) User Inputs
- `user_inputs` for interactive prompts.
- Use `default_value` and validation regex.

## 6) MCP (optional)
- `mcp_enabled`, `mcp_tool_name`, `mcp_input_schema`.
- `mcp_headless` if no UI prompts should appear.

## 7) Test
- Ensure `enabled: true`.
- Validate with clipboard input and MCP calls.

## Source
- Handler config schema: [Contextualizer.PluginContracts/HandlerConfig.cs](Contextualizer.PluginContracts/HandlerConfig.cs)