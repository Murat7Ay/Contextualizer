# UI Screens & Screen IDs

This page lists built-in screen IDs and how they map to UI routes and renderers.

## Screen ID Routing
- `screen_id` is used by `show_window` actions to open a tab.
- Tabs are keyed by `screen_id + title` (one tab per unique pair).
- Routing falls back to `/tab/:screenId/:title` when not mapped to a fixed route.

## Source References
- Tab routing & routes: [Contextualizer.UI/src/app/stores/tabStore.ts](Contextualizer.UI/src/app/stores/tabStore.ts)
- Dynamic tab renderer: [Contextualizer.UI/src/app/components/screens/DynamicTabScreen.tsx](Contextualizer.UI/src/app/components/screens/DynamicTabScreen.tsx)
- App routes: [Contextualizer.UI/src/app/App.tsx](Contextualizer.UI/src/app/App.tsx)

## Known Screen IDs (DynamicTabScreen)
- `markdown2` → Markdown viewer
- `jsonformatter` → JSON viewer
- `xmlformatter` → XML viewer
- `url_viewer` → URL viewer
- `plsql_editor` → SQL editor

## Known Screen IDs (Fixed Routes)
- `settings` / `react_settings` → /settings
- `handler_management` / `handlers` → /handlers
- `handler_exchange` / `marketplace` → /marketplace
- `cron_manager` / `cron` → /cron
- `handler_editor_new` → /handlers/new
- `handler_editor_edit` → /handlers/edit/:name

## Default Handler Screen IDs
Default handlers include screens like `jsonformatter`, `xmlformatter`, `markdown2`, and `url_viewer`.

Source:
- Default handlers bootstrap: [WpfInteractionApp/Services/SettingsService.cs](WpfInteractionApp/Services/SettingsService.cs)