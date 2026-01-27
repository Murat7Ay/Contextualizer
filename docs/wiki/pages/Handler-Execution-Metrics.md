# Handler Execution Metrics

This page describes runtime feedback and metrics surfaced during handler execution.

## User Feedback
- Activity log shows success, warning, and error events.
- `UserFeedback` is a UI-facing layer (distinct from `ILoggingService`).

## Source References
- User feedback: [Contextualizer.Core/UserFeedback.cs](Contextualizer.Core/UserFeedback.cs)
- Logging contract: [Contextualizer.PluginContracts/ILoggingService.cs](Contextualizer.PluginContracts/ILoggingService.cs)