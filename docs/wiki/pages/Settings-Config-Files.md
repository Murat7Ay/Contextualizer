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

## Source References
- Config service: [Contextualizer.Core/Services/ConfigurationService.cs](Contextualizer.Core/Services/ConfigurationService.cs)
- Config system settings: [WpfInteractionApp/Settings/AppSettings.cs](WpfInteractionApp/Settings/AppSettings.cs)