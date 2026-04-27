# Settings UI

This page describes the Settings screen and its sections.

## Sections
- General
- File Paths
- Keyboard
- Performance
- Config System
- Network Updates
- Initial Deployment
- Logging
- Advanced

## Host Integration
- Settings are loaded from the WPF host.
- Save sends updates to the host via WebView2 messages.

## Config Notes
- The MCP section now includes an **Enable Generic Data Tools** toggle.
- This toggle is restart-required and controls whether built-in generic MCP data tools such as `db_execute` and `db_scalar` are published.
- It is stored in the main app settings, not in `config.ini` / `secrets.ini`.

## Source
- Settings screen: [Contextualizer.UI/src/app/components/screens/Settings.tsx](Contextualizer.UI/src/app/components/screens/Settings.tsx)