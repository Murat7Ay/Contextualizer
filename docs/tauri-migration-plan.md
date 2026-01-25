# Tauri Migration Plan

## Overview
This plan describes how to migrate the current WPF + WebView2 host to Tauri while preserving the existing React UI and re-implementing host/backend responsibilities in Rust.

## Goals
- Keep the existing React UI with minimal changes.
- Replace the WPF host bridge with Tauri IPC (commands + events).
- Move host-side functionality (handlers, settings, cron, exchange, dialogs) into Rust.
- Rebuild native integrations (clipboard, hotkeys, window focus) with Rust/Tauri APIs.
- Define a plugin compatibility strategy early to reduce risk.

## Scope
- Frontend: React + Vite UI remains intact.
- Host: WPF WebView2 host replaced by Tauri.
- Backend: .NET Core host responsibilities re-implemented in Rust.
- Native: clipboard and global hotkeys re-implemented with Rust/Tauri.

## Step-by-Step Migration
### 1) Map UI bridge to Tauri IPC
- Replace WebView2 `postMessage` usage with Tauri `invoke` and event APIs.
- Maintain the existing payload shapes to minimize UI changes.
- Targets:
  - UI bridge: Contextualizer.UI/src/app/host/HostBridge.ts
  - Host message flow reference: WpfInteractionApp/ContextualizerHost.cs

### 2) Rebuild host responsibilities in Rust
Re-implement the host APIs that the UI depends on:
- Handlers CRUD + reload
- Cron list/update/trigger
- Settings read/write
- Exchange list/install/remove
- Dialogs, notifications, file/folder pickers

### 3) Recreate native integrations
- Clipboard access (text + file drop)
- Global hotkey / keyboard hook
- Window focus/activation behavior

### 4) Decide plugin compatibility strategy
Options:
- **Short-term:** Keep .NET plugins by running a sidecar process and communicating via IPC/HTTP.
- **Long-term:** Port plugins to Rust or WASM.

### 5) Rebuild MCP server (if required)
Recreate MCP JSON-RPC + SSE behaviors in Rust to match current functionality.

## Risks & Mitigations
- **Plugin ecosystem:** Start with sidecar compatibility; migrate gradually.
- **Native hooks:** Validate platform-specific APIs early; prototype on Windows first.
- **Behavior drift:** Keep payload schemas stable; add contract tests if possible.

## Deliverables
- Tauri app shell with the React UI loaded.
- Rust command/event layer mirroring existing host message types.
- Native integrations for clipboard + hotkeys.
- Documented plugin strategy and migration timeline.

## Follow-up
Add a link to this document in README.md under the Documentation section.
