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
- Run shell command: [Contextualizer.Core/Actions/RunShell.cs](Contextualizer.Core/Actions/RunShell.cs)

## Action Context Keys (Examples)
- `show_notification` uses `ContextKey._notification_title` and `ContextKey._duration` when present.
- `show_window` uses the handler config `ScreenId`, `Title`, `AutoFocusTab`, and `BringWindowToFront`.
- `copytoclipboard` copies the specified action key value into clipboard and shows a notification.
- `run_shell` reads the command from `action.key`, optionally uses `ContextKey._shell_working_directory` and `ContextKey._shell_timeout_seconds`, and writes results into `_shell_stdout`, `_shell_stderr`, `_shell_exit_code`, `_shell_timed_out`, and `_shell_elapsed_ms`.
- `run_shell` also supports alias keys via `ContextKey._shell_stdout_key`, `ContextKey._shell_stderr_key`, `ContextKey._shell_exit_code_key`, `ContextKey._shell_timed_out_key`, and `ContextKey._shell_elapsed_ms_key`.

## Example

```json
{
	"name": "run_shell",
	"key": "shell_command",
	"seeder": {
		"shell_command": "git status --short",
		"_shell_working_directory": "$(repo_root)",
		"_shell_timeout_seconds": "20",
		"_shell_stdout_key": "git_status_stdout",
		"_shell_stderr_key": "git_status_stderr",
		"_shell_exit_code_key": "git_status_exit_code"
	}
}
```

## Dispatcher
- Actions are invoked via a dispatcher wrapper: [Contextualizer.Core/Dispatcher.cs](Contextualizer.Core/Dispatcher.cs)

## Configuration
- `HandlerConfig` action list: [Contextualizer.PluginContracts/HandlerConfig.cs](Contextualizer.PluginContracts/HandlerConfig.cs)
