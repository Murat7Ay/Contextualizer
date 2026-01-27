# UI

## WPF Host
- Main shell: [WpfInteractionApp/ReactShellWindow.xaml.cs](WpfInteractionApp/ReactShellWindow.xaml.cs)

## Interaction Routing
- UI requests are routed to WebView or Native WPF via a router.
- WebView mode renders UI inside React tabs (default).
- Native mode uses WPF dialogs/notifications outside WebView.

## WebView Interaction Service
- WebView user prompts and notifications are implemented by `WebViewUserInteractionService`.
- Confirmations, user input (with navigation), and toasts are mediated through the React UI.

## Source References
- Router: [WpfInteractionApp/Services/UserInteractionServiceRouter.cs](WpfInteractionApp/Services/UserInteractionServiceRouter.cs)
- WebView service: [WpfInteractionApp/WebViewUserInteractionService.cs](WpfInteractionApp/WebViewUserInteractionService.cs)

## WebView2 Bridge
- Interaction service: [WpfInteractionApp/Services/WebView2InteractionService.cs](WpfInteractionApp/Services/WebView2InteractionService.cs)

## UI Assets
- Web UI project: [Contextualizer.UI/package.json](Contextualizer.UI/package.json)
- UI source root: [Contextualizer.UI](Contextualizer.UI)

## Screen IDs & Routing
- Screens and route mapping: [docs/wiki/pages/UI-Screens.md](docs/wiki/pages/UI-Screens.md)
 - UI troubleshooting: [docs/wiki/pages/UI-Troubleshooting.md](docs/wiki/pages/UI-Troubleshooting.md)

## Marketplace
- Handler exchange: [docs/wiki/pages/Handler-Exchange.md](docs/wiki/pages/Handler-Exchange.md)
- Package format: [docs/wiki/pages/Handler-Exchange-Package-Format.md](docs/wiki/pages/Handler-Exchange-Package-Format.md)

## Host Messaging
- WebView2 bridge: [docs/wiki/pages/UI-Host-Messaging.md](docs/wiki/pages/UI-Host-Messaging.md)

## Prompts & Cron
- Prompts and user inputs: [docs/wiki/pages/UI-Prompts.md](docs/wiki/pages/UI-Prompts.md)
- Cron manager UI: [docs/wiki/pages/UI-Cron-Manager.md](docs/wiki/pages/UI-Cron-Manager.md)

## Settings & Handlers
- Settings screen: [docs/wiki/pages/UI-Settings.md](docs/wiki/pages/UI-Settings.md)
- Handler management: [docs/wiki/pages/UI-Handler-Management.md](docs/wiki/pages/UI-Handler-Management.md)
