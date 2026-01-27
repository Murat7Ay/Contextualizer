# Core Services

This page documents core services that are registered in the service locator and used across the application.

## Service Locator
- Central registry for runtime services.

## Clipboard Service
- `IClipboardService` abstracts clipboard operations.

## Keyboard Hook
- Global shortcut listener captures selected text and triggers handler execution.

## Source References
- Service locator: [Contextualizer.Core/ServiceLocator.cs](Contextualizer.Core/ServiceLocator.cs)
- Clipboard service interface: [Contextualizer.PluginContracts/IClipboardService.cs](Contextualizer.PluginContracts/IClipboardService.cs)
- Keyboard hook: [Contextualizer.Core/KeyboardHook.cs](Contextualizer.Core/KeyboardHook.cs)