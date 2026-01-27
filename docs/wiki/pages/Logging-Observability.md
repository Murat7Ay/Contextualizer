# Logging & Observability

This page documents logging, usage tracking, and diagnostics configuration.

## Logging Interfaces
- `ILoggingService` provides structured logs, usage events, and performance metrics.

## Configuration
- Logging settings are stored in app settings and mapped into `LoggingConfiguration`.
- Common fields: enable local logging, minimum log level, log path, max file size/count, usage tracking.

## Startup Lifecycle
- Logging service is initialized early during app startup.
- Errors during startup are logged and surfaced via UI notifications.

## Source References
- Logging contract: [Contextualizer.PluginContracts/ILoggingService.cs](Contextualizer.PluginContracts/ILoggingService.cs)
- Settings mapping: [WpfInteractionApp/Settings/AppSettings.cs](WpfInteractionApp/Settings/AppSettings.cs)
- Startup registration: [WpfInteractionApp/App.xaml.cs](WpfInteractionApp/App.xaml.cs)