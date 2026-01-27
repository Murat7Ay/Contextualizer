# Handler Exchange Package Format

This page documents the JSON schema used by handler exchange packages.

## Package Fields
- id, name, version, author, description
- tags, dependencies
- handlerJson (required)
- template_user_inputs (optional)
- metadata (optional)

## Source References
- Package model: [Contextualizer.PluginContracts/Models/HandlerPackage.cs](Contextualizer.PluginContracts/Models/HandlerPackage.cs)
- Exchange interface: [Contextualizer.PluginContracts/Interfaces/IHandlerExchange.cs](Contextualizer.PluginContracts/Interfaces/IHandlerExchange.cs)