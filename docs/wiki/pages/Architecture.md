# Architecture

## End-to-End Flow
Clipboard/Trigger → Handler selection → Context creation → Action execution → UI/MCP output

## Execution Lifecycle (Clipboard)
1. `HandlerManager` listens to clipboard via `KeyboardHook` and captures text or file content.
2. Enabled handlers run `CanHandle` checks in parallel.
3. `Dispatch` executes the selected handler pipeline.
4. User confirmations and user inputs are enforced (unless MCP headless).
5. Seeder, template processing, and actions are executed.
6. The final formatted output is produced and sent to UI or MCP.

## Execution Lifecycle (MCP)
- MCP calls pass a seed context that can override or populate handler context when enabled.
- Headless MCP mode skips prompts and fails fast if required inputs are missing.

## Source References
- Clipboard listener and handler execution: [Contextualizer.Core/HandlerManager.cs](Contextualizer.Core/HandlerManager.cs)
- Dispatch pipeline and headless MCP flow: [Contextualizer.Core/Dispatch.cs](Contextualizer.Core/Dispatch.cs)
- User inputs and context resolution: [Contextualizer.Core/HandlerContextProcessor.cs](Contextualizer.Core/HandlerContextProcessor.cs)

## Core Components
- `HandlerManager` – orchestrates handler execution.
- `Dispatch` – builds contexts and dispatches results.
- `ActionService` – executes action chains and conditions.

## Related Pages
- Core services: [docs/wiki/pages/Core-Services.md](docs/wiki/pages/Core-Services.md)
- Lifecycle & startup: [docs/wiki/pages/Lifecycle-Startup.md](docs/wiki/pages/Lifecycle-Startup.md)
 - Clipboard capture: [docs/wiki/pages/Clipboard-Capture.md](docs/wiki/pages/Clipboard-Capture.md)
 - Execution metrics: [docs/wiki/pages/Handler-Execution-Metrics.md](docs/wiki/pages/Handler-Execution-Metrics.md)

## Source References
- Handler orchestration: [Contextualizer.Core/HandlerManager.cs](Contextualizer.Core/HandlerManager.cs)
- Dispatch pipeline: [Contextualizer.Core/Dispatch.cs](Contextualizer.Core/Dispatch.cs)
- Action execution: [Contextualizer.Core/ActionService.cs](Contextualizer.Core/ActionService.cs)
- Condition evaluation: [Contextualizer.Core/ConditionEvaluator.cs](Contextualizer.Core/ConditionEvaluator.cs)

## Data Contracts
- `HandlerConfig`: [Contextualizer.PluginContracts/HandlerConfig.cs](Contextualizer.PluginContracts/HandlerConfig.cs)
- `ContextWrapper`: [Contextualizer.PluginContracts/ContextWrapper.cs](Contextualizer.PluginContracts/ContextWrapper.cs)
- `ClipboardContent`: [Contextualizer.PluginContracts/ClipboardContent.cs](Contextualizer.PluginContracts/ClipboardContent.cs)
