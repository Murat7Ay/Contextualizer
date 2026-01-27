# UI Host ↔ WebView2 Messaging

This page documents the message bridge between the WPF host and the React UI.

## Overview
- WebView2 uses postMessage to exchange JSON payloads.
- The host listens for UI messages and responds with typed events.

## UI → Host
- UI sends messages through the WebView2 bridge.
- The bridge posts typed requests such as handler list, cron updates, settings, and exchange actions.

## Host → UI
- Host emits events such as `host_ready`, `log`, `open_tab`, and results for request types.

## Source References
- Host listener: [WpfInteractionApp/ReactShellWindow.xaml.cs](WpfInteractionApp/ReactShellWindow.xaml.cs)
- UI bridge: [Contextualizer.UI/src/app/host/webview2Bridge.ts](Contextualizer.UI/src/app/host/webview2Bridge.ts)
- UI host initialization: [Contextualizer.UI/src/app/host/initHostBridge.ts](Contextualizer.UI/src/app/host/initHostBridge.ts)
- UI event listener: [Contextualizer.UI/src/app/host/HostBridgeListener.tsx](Contextualizer.UI/src/app/host/HostBridgeListener.tsx)