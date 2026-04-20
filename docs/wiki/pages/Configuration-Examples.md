# Configuration Examples

This page provides real-world configuration patterns based on the default handlers file.

## Source
- Default handlers file: [handlers.json](handlers.json)

## Lookup Handler Example
Key concepts: lookup file, key/value mapping, output template, and actions.

```json
{
  "name": "Corp Lookup",
  "type": "lookup",
  "screen_id": "markdown2",
  "title": "Kurum Bilgileri",
  "actions": [
    { "name": "show_window", "key": "_formatted_output" },
    { "name": "copytoclipboard", "key": "oid" }
  ],
  "path": "C:\\Finder\\corp_data.txt",
  "delimiter": "||",
  "key_names": ["drivercode", "oid"],
  "value_names": ["drivercode", "oid", "name", "engine"],
  "output_format": "$file:C:\\Finder\\output_template\\markdown_capabilities.txt",
  "mcp_enabled": true
}
```

## Seeder + Function Pipeline Example
Key concepts: placeholder replacement, function pipeline, and config lookup.

See also: [docs/wiki/pages/Function-Pipeline-Reference.md](docs/wiki/pages/Function-Pipeline-Reference.md)

```json
"seeder": {
  "customer_id_cloned": "$(customer_id)",
  "url_param": "$func:base64encode($(name))",
  "url_param_encode": "$func:url.encode($(url_param))",
  "encoded_pipeline": "$func:{{ $(pipeline) | string.trim | string.upper }}"
}
```

## User Inputs Example
Key concepts: selection list, password input, validation regex.

```json
"user_inputs": [
  {
    "key": "platform",
    "title": "Platform Selection",
    "message": "Please select your target platform:",
    "is_required": true,
    "is_selection_list": true,
    "selection_items": [
      { "value": "windows", "display": "Windows" },
      { "value": "linux", "display": "Linux" }
    ],
    "default_value": "windows"
  },
  {
    "key": "api_key",
    "title": "API Key",
    "message": "Enter your API key:",
    "validation_regex": "^[A-Za-z0-9-_]{3}$",
    "is_required": true,
    "is_password": true
  }
]
```

## Run Shell Action Example
Key concepts: action seeding, context-driven shell execution, result aliasing, `inner_actions`, and `show_window` screen selection.

```json
{
  "name": "Repo Status",
  "type": "Manual",
  "screen_id": "markdown2",
  "title": "Git Status",
  "auto_focus_tab": true,
  "bring_window_to_front": true,
  "actions": [
    {
      "name": "run_shell",
      "key": "shell_command",
      "seeder": {
        "shell_command": "git status",
        "repo_root": "C:\\Users\\murat\\source\\repos\\Contextualizer",
        "_shell_working_directory": "$(repo_root)",
        "_shell_timeout_seconds": "20",
        "_shell_stdout_key": "git_status_stdout",
        "_shell_stderr_key": "git_status_stderr"
      },
      "inner_actions": [
        {
          "name": "show_window",
          "key": "git_status_stdout"
        }
      ]
    }
  ]
}
```

Notes:
- `show_window` is nested under `run_shell` so it runs after the shell command finishes and writes its result into context.
- If one action depends on values produced by another action, prefer `inner_actions` over separate top-level action entries.
- `screen_id` is only needed because this example uses `show_window`. `run_shell` by itself does not require it.
- Use a valid UI screen such as `markdown2`, `jsonformatter`, `xmlformatter`, or `url_viewer`.
- `git status --short` can return empty output on a clean repository, which makes the tab look blank. `git status` is a better visible example.

## MCP Flags Example
Key concepts: enabling MCP, tool naming, headless behavior, seed overwrite.

```json
{
  "mcp_enabled": true,
  "mcp_tool_name": "corp_lookup",
  "mcp_description": "Lookup company records",
  "mcp_headless": false,
  "mcp_seed_overwrite": false
}
```