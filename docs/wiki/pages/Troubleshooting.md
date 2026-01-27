# Troubleshooting

## Common Issues
- Handler not firing
- Plugin not loaded
- UI blank or unresponsive
- MCP tool not found
 - Cron job not executing

## Decision Flow
1. Handler not firing?
	- Check `enabled` in handler config.
	- Confirm `CanHandle` conditions (regex/file extensions/lookup key).
	- Check logs and activity feed.
2. UI blank?
	- Confirm WebView2 UI dist exists.
	- Check UI routing and `screen_id` values.
3. MCP tool not found?
	- Ensure handler has `mcp_enabled: true`.
	- Verify tool name and slug mapping.
4. Cron job not executing?
	- Verify `cron_expression` and `cron_enabled`.
	- Check cron service is running.

## Condition Operators
- equals / not_equals
- greater_than / less_than
- contains / starts_with / ends_with
- matches_regex
- is_empty / is_not_empty

Source:
- Condition evaluation: [Contextualizer.Core/ConditionEvaluator.cs](Contextualizer.Core/ConditionEvaluator.cs)

## Diagnostics Entry Points
- Handler loading: [Contextualizer.Core/HandlerLoader.cs](Contextualizer.Core/HandlerLoader.cs)
- Dispatch execution: [Contextualizer.Core/Dispatch.cs](Contextualizer.Core/Dispatch.cs)
- MCP host: [WpfInteractionApp/Services/McpServerHost.cs](WpfInteractionApp/Services/McpServerHost.cs)
 - Cron scheduler: [Contextualizer.Core/Services/CronScheduler.cs](Contextualizer.Core/Services/CronScheduler.cs)
 - UI host: [WpfInteractionApp/ReactShellWindow.xaml.cs](WpfInteractionApp/ReactShellWindow.xaml.cs)

## UI Appendix
- UI troubleshooting: [docs/wiki/pages/UI-Troubleshooting.md](docs/wiki/pages/UI-Troubleshooting.md)

## Metrics & Logs
- Execution metrics: [docs/wiki/pages/Handler-Execution-Metrics.md](docs/wiki/pages/Handler-Execution-Metrics.md)

## Logs
- Logging interface: [Contextualizer.PluginContracts/ILoggingService.cs](Contextualizer.PluginContracts/ILoggingService.cs)
