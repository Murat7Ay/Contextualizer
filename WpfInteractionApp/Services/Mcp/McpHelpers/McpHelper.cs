using System;
using System.Collections.Generic;
using System.Text;
using Contextualizer.Core;
using Contextualizer.PluginContracts;
using Contextualizer.Core.Services;

namespace WpfInteractionApp.Services.Mcp.McpHelpers
{
    internal static class McpHelper
    {
        public static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "tool";

            var sb = new StringBuilder(name.Length);
            foreach (var ch in name.Trim())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    continue;
                }

                if (ch == ' ' || ch == '-' || ch == '_' || ch == '.')
                {
                    if (sb.Length == 0 || sb[^1] == '_') continue;
                    sb.Append('_');
                }
            }

            var result = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "tool" : result;
        }

        public static string BuildInputText(HandlerConfig config, Dictionary<string, string> argsDict)
        {
            if (!string.IsNullOrWhiteSpace(config.McpInputTemplate))
            {
                return HandlerContextProcessor.ReplaceDynamicValues(config.McpInputTemplate, argsDict);
            }

            if (argsDict.TryGetValue("text", out var text))
                return text;

            if (config.McpHeadless)
                return string.Empty;

            return System.Text.Json.JsonSerializer.Serialize(argsDict);
        }

        public static Dictionary<string, string> BuildReturnPayload(HandlerConfig config, DispatchExecutionResult execResult)
        {
            if (config.McpReturnKeys == null || config.McpReturnKeys.Count == 0)
            {
                return new Dictionary<string, string>
                {
                    [ContextKey._formatted_output] = execResult.FormattedOutput ?? string.Empty
                };
            }

            var context = execResult.Context ?? new Dictionary<string, string>();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in config.McpReturnKeys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (context.TryGetValue(key, out var value))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        public static void ShowMarkdownTab(string title, string markdown, bool autoFocus, bool bringToFront)
        {
            var ui = ServiceLocator.SafeGet<WebViewUserInteractionService>()
                ?? ServiceLocator.SafeGet<IUserInteractionService>();

            if (ui == null) return;

            var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ContextKey._body] = markdown
            };

            ui.ShowWindow("markdown2", title, context, null, autoFocus, bringToFront);
        }

        public static string BuildHandlerDocsMarkdown()
        {
            return """
# Handler Authoring Guide (Contextualizer) — Single Source of Truth

This document fully describes **all supported fields** in `HandlerConfig` and how to compose a handler from scratch.
If you know nothing about the system, start here.

---

## 0) Minimal handler
Every handler requires:
```
name: string
type: string
```
Everything else is optional but may be required by the specific handler type.

---

## 1) Handler lifecycle (execution order)
1. **CanHandle** decides if a handler should run for clipboard content.
2. **CreateContext** builds a context map (regex groups, API JSON, DB rows, file metadata, etc.).
3. **ConstantSeeder** merges into context (no templating).
4. **Seeder** merges into context (templating enabled).
5. **Templating** resolves `$config:`, `$file:`, `$func:` then `$(key)` placeholders.
6. **Actions** execute in order (with optional conditions and action user_inputs).
7. **Output** is produced using `output_format` (if missing, handler may produce defaults).

---

## 2) All fields (HandlerConfig schema)

### Common identity + UI
```
name: string (required)
description?: string
type: string (required)
screen_id?: string         // tab id for show_window actions
title?: string             // tab title
enabled?: boolean          // default true
requires_confirmation?: boolean
auto_focus_tab?: boolean
bring_window_to_front?: boolean
```

### Output + context seeding
```
output_format?: string
seeder?: { [key:string]: string }           // templated values
constant_seeder?: { [key:string]: string }  // raw values
user_inputs?: UserInputRequest[]            // handler-level prompts
actions?: ConfigAction[]                    // handler-level actions
```

### Regex / Groups (used by Regex, optional in Api/Database)
```
regex?: string
groups?: string[]
```

### File handler
```
file_extensions?: string[]   // e.g. [".txt", ".json"]
```

### Lookup handler
```
path?: string                 // supports $config:, $file:
delimiter?: string            // e.g. "\\t" or ","
key_names?: string[]          // which columns are keys
value_names?: string[]        // all column names
```

### Database handler
```
connectionString?: string
query?: string
connector?: string            // "mssql" | "plsql"
command_timeout_seconds?: int?
connection_timeout_seconds?: int?
max_pool_size?: int?
min_pool_size?: int?
disable_pooling?: bool?
regex?: string                // optional
groups?: string[]             // optional
```

### API handler
```
url?: string
method?: string               // GET/POST/PUT/PATCH/DELETE
headers?: { [key:string]: string }
request_body?: object|array   // JSON
content_type?: string         // e.g. application/json
timeout_seconds?: int?
regex?: string                // optional
groups?: string[]             // optional
```

### Custom handler
```
validator?: string            // IContextValidator.Name
context_provider?: string     // IContextProvider.Name
```

### Synthetic handler
```
reference_handler?: string    // name of existing handler
actual_type?: string          // embed actual handler type
synthetic_input?: UserInputRequest
```

### Cron handler
```
cron_job_id?: string
cron_expression?: string
cron_timezone?: string
cron_enabled?: bool
actual_type?: string          // embedded handler type (required)
```

### MCP (Model Context Protocol)
```
mcp_enabled?: bool
mcp_tool_name?: string
mcp_description?: string
mcp_input_schema?: object     // JSON Schema
mcp_input_template?: string   // builds ClipboardContent.Text
mcp_return_keys?: string[]    // filter outputs
mcp_headless?: bool           // disable UI prompts
mcp_seed_overwrite?: bool
```

---

## 3) Placeholders & templating
Templating order: `$file:` → `$config:` → `$func:` → `$(key)`

### $(key)
```
"Order $(orderId) for $(customer)"
```

### $config:
```
$config:secrets.api_key
$config:database.default
```

### $file:
```
$file:C:\\path\\file.txt
```

### $func:
Functions run before `$(key)` substitution.
Examples:
```
$func:today().format("yyyy-MM-dd")
$func:guid()
$func:string.upper("hello")
$func:{{ $(id) | string.upper() }}
```

---

## 4) FunctionProcessor reference
Supported base functions (case-insensitive):
```
today, now, yesterday, tomorrow, guid, random,
base64encode, base64decode, env, username, computername
```

Supported namespaces (base):
```
hash.*, url.*, web.*, ip.*, json.*, string.*, math.*, array.*
```

Common examples:
```
$func:hash.sha256("text")
$func:url.encode("a b")
$func:web.get("https://example.com")
$func:ip.local()
$func:json.get("{\\"a\\":1}", "a")
$func:string.upper("hi")
$func:math.add(2, 3)
$func:array.join("[\\"a\\",\\"b\\"]", ",")
```

Pipeline format:
```
$func:{{ "abc" | string.upper() | string.substring(0,2) }}
```

---

## 5) Conditions (ConditionEvaluator)
Operators:
```
and, or, equals, not_equals, greater_than, less_than,
contains, starts_with, ends_with, matches_regex,
is_empty, is_not_empty
```

Leaf condition:
```
{ "operator": "equals", "field": "StatusCode", "value": "200" }
```

Group:
```
{ "operator": "and", "conditions": [ ... ] }
```

---

## 6) Seeder vs ConstantSeeder
- `constant_seeder` merges raw values first.
- `seeder` merges after and resolves templates.

Example:
```
constant_seeder: { "source": "ui" }
seeder: { "key": "$(group_1)", "ts": "$func:now().format(\\"o\\")" }
```

---

## 7) UserInputRequest (all options)
```
{
  "key": "username",
  "title": "User",
  "message": "Enter user name",
  "validation_regex": "^[a-z0-9_]+$",
  "is_required": true,
  "is_selection_list": false,
  "is_password": false,
  "selection_items": [ {"value":"a","display":"A"} ],
  "is_multi_select": false,
  "is_file_picker": false,
  "is_folder_picker": false,
  "is_multi_line": false,
  "is_date": false,
  "is_date_picker": false,
  "is_time": false,
  "is_time_picker": false,
  "is_date_time": false,
  "is_datetime_picker": false,
  "default_value": "",
  "dependent_key": "country",
  "dependent_selection_item_map": {
    "TR": { "selection_items": [ {"value":"34","display":"Istanbul"} ], "default_value": "34" }
  },
  "config_target": "secrets.section.key"
}
```

---

## 8) ConfigAction (all options)
```
{
  "name": "show_window",
  "key": "optional",
  "requires_confirmation": false,
  "conditions": { ...Condition... },
  "user_inputs": [ ...UserInputRequest... ],
  "seeder": { "k": "$(value)" },
  "constant_seeder": { "k": "v" },
  "inner_actions": [ ...ConfigAction... ]
}
```

---

## 9) Type-specific minimal examples

### Regex
```
{ "name":"R1", "type":"Regex", "regex":"^ABC(?<id>\\d+)$", "groups":["id"] }
```

### File
```
{ "name":"F1", "type":"File", "file_extensions":[".txt",".json"] }
```

### Lookup
```
{ "name":"L1", "type":"Lookup", "path":"$config:data.lookup_path", "delimiter":"\\t",
  "key_names":["sku"], "value_names":["sku","desc","price"] }
```

### Database
```
{ "name":"DB1", "type":"Database", "connector":"mssql",
  "connectionString":"$config:db.main",
  "query":"select * from Orders where Id=@orderId",
  "regex":"^ORD-(?<orderId>\\d+)$", "groups":["orderId"] }
```

### API
```
{ "name":"API1", "type":"Api",
  "url":"https://api/items/$(id)",
  "method":"GET",
  "headers": { "Authorization":"Bearer $config:secrets.api" } }
```

### Custom
```
{ "name":"C1", "type":"Custom", "validator":"MyValidator", "context_provider":"MyProvider" }
```

### Manual
```
{ "name":"M1", "type":"Manual" }
```

### Synthetic (reference)
```
{ "name":"S1", "type":"Synthetic", "reference_handler":"API1" }
```

### Synthetic (embedded)
```
{ "name":"S2", "type":"Synthetic", "actual_type":"Database",
  "connectionString":"$config:db.main", "connector":"mssql", "query":"select 1" }
```

### Cron (embedded)
```
{ "name":"CR1", "type":"Cron", "cron_expression":"0 */5 * * * ?",
  "cron_timezone":"Europe/Istanbul", "cron_enabled":true,
  "actual_type":"Api", "url":"https://api/ping", "method":"GET" }
```

---

## 10) MCP usage
Set `mcp_enabled` to expose the handler as a tool.

Optional MCP fields:
```
mcp_tool_name, mcp_description, mcp_input_schema,
mcp_input_template, mcp_return_keys, mcp_headless, mcp_seed_overwrite
```

If no `mcp_input_schema` is provided:
- File handlers expect `{ files: string[] }`
- If `user_inputs` exist, a schema is generated from them
- Otherwise `{ text: string }`

---

## 11) Complete example (API + actions + user_inputs)
```
{
  "name":"OrderLookup",
  "type":"Api",
  "url":"https://api/orders/$(orderId)",
  "method":"GET",
  "headers": { "Authorization":"Bearer $config:secrets.api_key" },
  "user_inputs":[
    { "key":"orderId", "title":"Order", "message":"Enter order id" }
  ],
  "seeder": { "requested_at":"$func:now().format(\\"o\\")" },
  "actions": [
    {
      "name":"show_notification",
      "conditions": { "operator":"equals", "field":"StatusCode", "value":"200" }
    }
  ],
  "output_format":"Order $(id) — $(status)"
}
```
""";
        }
    }
}
