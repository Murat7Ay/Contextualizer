# Actions

## Contract
- `IAction`: [Contextualizer.PluginContracts/IAction.cs](Contextualizer.PluginContracts/IAction.cs)

## Execution
- `ActionService`: [Contextualizer.Core/ActionService.cs](Contextualizer.Core/ActionService.cs)
- `ConditionEvaluator`: [Contextualizer.Core/ConditionEvaluator.cs](Contextualizer.Core/ConditionEvaluator.cs)

## Runtime Loading
- Actions, validators, and context providers are discovered via reflection across all loaded assemblies.
- Each action is initialized with `PluginServiceProviderImp`.

## Inner Actions
- Actions can contain nested `InnerActions` executed sequentially.
- Errors in an inner action do not block subsequent inner actions.

## Built-in Actions (Source)
- Actions folder: [Contextualizer.Core/Actions](Contextualizer.Core/Actions)

## Common Built-in Actions
- Show notification: [Contextualizer.Core/Actions/ShowNotification.cs](Contextualizer.Core/Actions/ShowNotification.cs)
- Show window (React tab): [Contextualizer.Core/Actions/ShowWindow.cs](Contextualizer.Core/Actions/ShowWindow.cs)
- Copy to clipboard: [Contextualizer.Core/Actions/CopyToClipboard.cs](Contextualizer.Core/Actions/CopyToClipboard.cs)

## Action Context Keys (Examples)
- `show_notification` uses `ContextKey._notification_title` and `ContextKey._duration` when present.
- `show_window` uses the handler config `ScreenId`, `Title`, `AutoFocusTab`, and `BringWindowToFront`.
- `copytoclipboard` copies the specified action key value into clipboard and shows a notification.

## Dispatcher
- Actions are invoked via a dispatcher wrapper: [Contextualizer.Core/Dispatcher.cs](Contextualizer.Core/Dispatcher.cs)

## Configuration
- `HandlerConfig` action list: [Contextualizer.PluginContracts/HandlerConfig.cs](Contextualizer.PluginContracts/HandlerConfig.cs)
