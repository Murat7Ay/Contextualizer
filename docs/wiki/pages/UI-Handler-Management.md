# Handler Management UI

This page describes the handler management and editor screens.

## Handler Management
- Lists handlers and shows enabled/MCP status.
- Supports search and filtering.
- Enables/disables handlers and toggles MCP exposure.

## Handler Editor
- New/Edit handler wizard with advanced JSON.
- Validates on save via host.
- Field reference: [docs/wiki/pages/Handler-Editor-JSON-Schema-Fields.md](docs/wiki/pages/Handler-Editor-JSON-Schema-Fields.md)

## Source
- Management screen: [Contextualizer.UI/src/app/components/screens/HandlerManagement.tsx](Contextualizer.UI/src/app/components/screens/HandlerManagement.tsx)
- Editor page: [Contextualizer.UI/src/app/components/screens/HandlerEditorPage.tsx](Contextualizer.UI/src/app/components/screens/HandlerEditorPage.tsx)