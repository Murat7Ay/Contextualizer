# UI Prompts & User Inputs

This page documents UI prompts for confirmations, user inputs, and navigation flows.

## WebView Prompt Layer
- WebView prompts are handled by the HostPromptLayer in the React UI.
- Prompts are queued and displayed sequentially.
- File/folder pickers are supported and round-trip via the host bridge.

## Native Prompts
- NativeUserInteractionService can show WPF dialogs directly.
- Useful when WebView is not focused or for system-level prompts.

## Source References
- Prompt layer: [Contextualizer.UI/src/app/host/HostPromptLayer.tsx](Contextualizer.UI/src/app/host/HostPromptLayer.tsx)
- Native prompts: [WpfInteractionApp/Services/NativeUserInteractionService.cs](WpfInteractionApp/Services/NativeUserInteractionService.cs)