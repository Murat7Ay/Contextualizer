# Configuration Recipes

Practical patterns for conditions, templates, and action chains.

## Conditional Action (selector key)
```json
{
  "name": "copytoclipboard",
  "key": "drivercode",
  "conditions": {
    "operator": "and",
    "conditions": [
      { "operator": "equals", "field": "_selector_key", "value": "oid" }
    ]
  }
}
```

## Regex-driven Handler
```json
{
  "type": "regex",
  "regex": "(\\w+)-(\\d+)",
  "groups": ["name", "id"],
  "output_format": "Name=$(name), Id=$(id)"
}
```

## Dynamic Template with Functions
```json
{
  "output_format": "$func:{{ $(full_name) | string.trim | string.upper }}"
}
```

See also: [docs/wiki/pages/Function-Pipeline-Reference.md](docs/wiki/pages/Function-Pipeline-Reference.md)

## File-based Template
```json
{
  "output_format": "$file:C:\\Templates\\summary.md"
}
```

## MCP Seed Overwrite (headless)
```json
{
  "mcp_enabled": true,
  "mcp_headless": true,
  "mcp_seed_overwrite": true
}
```

## Source
- Condition evaluation: [Contextualizer.Core/ConditionEvaluator.cs](Contextualizer.Core/ConditionEvaluator.cs)
- Dynamic value replacement: [Contextualizer.Core/HandlerContextProcessor.cs](Contextualizer.Core/HandlerContextProcessor.cs)