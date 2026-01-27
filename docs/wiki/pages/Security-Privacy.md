# Security & Privacy

## Data Scope
- Clipboard data and context content are processed by handlers and actions.

## Areas to Review
- Clipboard capture: [Contextualizer.PluginContracts/WindowsClipboardService.cs](Contextualizer.PluginContracts/WindowsClipboardService.cs)
- Logging: [Contextualizer.PluginContracts/ILoggingService.cs](Contextualizer.PluginContracts/ILoggingService.cs)
- Notifications: [Contextualizer.PluginContracts/INativeNotificationService.cs](Contextualizer.PluginContracts/INativeNotificationService.cs)

## Logging & Usage Tracking
- Local logging includes errors, warnings, and debug logs.
- Optional usage tracking supports remote endpoints.
- Performance metrics are collected per operation.

## Logging Configuration
- Default log path and levels are defined by `LoggingConfiguration`.
- Sensitive values should be stored in dedicated config/secret sources.

## Observability
- Logging & observability: [docs/wiki/pages/Logging-Observability.md](docs/wiki/pages/Logging-Observability.md)

## Notification Surface
- Native notifications provide out-of-WebView visibility.

## Recommendations
- Document data handling rules per handler.
- Flag any network-bound actions.
