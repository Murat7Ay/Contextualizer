# Clipboard Capture & Keyboard Shortcut

This page explains how selected text/files are captured and turned into handler input.

## Flow
1. Global keyboard hook listens for the configured shortcut.
2. Selected text is copied via simulated Ctrl+C.
3. Clipboard is read (text or file list).
4. Clipboard content is passed into handler execution.

## Source References
- Keyboard hook: [Contextualizer.Core/KeyboardHook.cs](Contextualizer.Core/KeyboardHook.cs)
- Keyboard simulator: [Contextualizer.PluginContracts/KeyboardSimulator.cs](Contextualizer.PluginContracts/KeyboardSimulator.cs)
- Clipboard API: [Contextualizer.PluginContracts/WindowsClipboard.cs](Contextualizer.PluginContracts/WindowsClipboard.cs)
- Clipboard model: [Contextualizer.PluginContracts/ClipboardContent.cs](Contextualizer.PluginContracts/ClipboardContent.cs)