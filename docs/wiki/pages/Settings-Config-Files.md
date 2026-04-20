# Settings & Configuration Files

This page documents application settings, config system files, and how config values are resolved.

## Settings Entry Points
- App settings: [WpfInteractionApp/Settings/AppSettings.cs](WpfInteractionApp/Settings/AppSettings.cs)

## Config System
- `config.ini` and `secrets.ini` are loaded by the configuration service.
- Secrets override config values with the same key.
- Keys are referenced as `section.key`.

## Defaults & Auto-Creation
- When enabled, default config and secrets files are created on first run.
- Paths are set via `ConfigSystemSettings`.

## MCP Settings Notes
- `mcp_settings.data_tools_registry_path` controls where the data-tools registry JSON is loaded from.
- Default value: `C:\PortableApps\Contextualizer\Config\data-tools.json`
- If the file does not exist, Contextualizer creates it with disabled sample definitions.

## Source References
- Config service: [Contextualizer.Core/Services/ConfigurationService.cs](Contextualizer.Core/Services/ConfigurationService.cs)
- Config system settings: [WpfInteractionApp/Settings/AppSettings.cs](WpfInteractionApp/Settings/AppSettings.cs)