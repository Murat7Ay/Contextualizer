# Glossary

- Handler: A pipeline component that detects clipboard/context and produces structured output.
- Action: A unit of work executed after context creation.
- Context: Structured data derived from clipboard or other sources.
- MCP: Model Context Protocol interface exposing tools.
- Trigger: A condition that initiates handler execution.

## Context Keys
- `_input`: Raw clipboard input.
- `_match`: Regex full match (when applicable).
- `_formatted_output`: Final formatted output string.
- `_body`: Content passed to UI screens (markdown/json/xml/etc).
- `_selector_key`: Which key matched the original clipboard text.
- `_notification_title`: Title for notification actions.
- `_duration`: Notification duration seconds.
- `_error`: Error message (when execution fails).
- `_trigger`: Execution source (`app` or `mcp`).

## UI
- Screen ID: Routes a tab to a specific UI renderer.
- Tab: A unique UI view keyed by screen id + title.

## Scheduling
- Cron: Time-based handler execution (Quartz-backed).

## Source References
- Context keys: [Contextualizer.PluginContracts/ContextKey.cs](Contextualizer.PluginContracts/ContextKey.cs)
