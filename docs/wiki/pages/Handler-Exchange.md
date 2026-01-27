# Handler Exchange & Marketplace

This page describes the local file-based handler exchange (marketplace) used to list, install, update, and remove handler packages.

## How It Works
- Exchange packages are stored as JSON files in the exchange directory.
- Installed packages are tracked in a separate Installed directory.
- Installing a package appends its handler JSON to handlers.json.

## Source References
- Exchange service: [WpfInteractionApp/Services/FileHandlerExchange.cs](WpfInteractionApp/Services/FileHandlerExchange.cs)
- Exchange registration: [WpfInteractionApp/App.xaml.cs](WpfInteractionApp/App.xaml.cs)
- UI bridge (WebView2): [Contextualizer.UI/src/app/host/webview2Bridge.ts](Contextualizer.UI/src/app/host/webview2Bridge.ts)

## Key Operations
- List packages (search + tag filter)
- Get package details
- Install package (adds handler to handlers.json)
- Update package (remove + reinstall)
- Remove package
- Publish package (write JSON to exchange)

## Directories
- Exchange directory (packages)
- Installed directory (tracking)

## UI Route
- Marketplace screen: `handler_exchange` / `marketplace`

## Package Format
- Package schema: [docs/wiki/pages/Handler-Exchange-Package-Format.md](docs/wiki/pages/Handler-Exchange-Package-Format.md)