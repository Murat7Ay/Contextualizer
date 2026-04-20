# Project Guidelines

## Architecture
- Put reusable runtime logic in `Contextualizer.Core` when it is shared by handlers/actions and WPF MCP tools.
- Keep `Contextualizer.Core` free of WPF/UI-specific dependencies; `WpfInteractionApp` should consume Core, not the reverse.
- Built-in actions live under `Contextualizer.Core/Actions` and are discovered via reflection through `IAction`.

## Conventions
- For handler actions, prefer the existing `key` plus `seeder`/`constant_seeder` pattern instead of introducing ad hoc config shapes.
- Add new reserved context keys to `Contextualizer.PluginContracts/ContextKey.cs` when a built-in action needs shared option/result names.
- When adding or changing built-in actions or MCP tools, keep the handler docs in `docs/wiki/pages` aligned.

## Build And Test
- Validate code changes from the repo root with `dotnet build Contextualizer.sln`.
- If a change affects both MCP tooling and handler execution, verify the shared code path rather than maintaining duplicated implementations.