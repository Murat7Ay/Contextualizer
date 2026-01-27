# Plugin System

## Contracts
- `IHandler`: [Contextualizer.PluginContracts/IHandler.cs](Contextualizer.PluginContracts/IHandler.cs)
- `IAction`: [Contextualizer.PluginContracts/IAction.cs](Contextualizer.PluginContracts/IAction.cs)
- `IContextProvider`: [Contextualizer.PluginContracts/IContextProvider.cs](Contextualizer.PluginContracts/IContextProvider.cs)
- `IContextValidator`: [Contextualizer.PluginContracts/IContextValidator.cs](Contextualizer.PluginContracts/IContextValidator.cs)

## Loading & Discovery
- Loader: [Contextualizer.Core/HandlerLoader.cs](Contextualizer.Core/HandlerLoader.cs)
- Assembly loading: [Contextualizer.Core/DynamicAssemblyLoader.cs](Contextualizer.Core/DynamicAssemblyLoader.cs)
- Service provider: [Contextualizer.Core/PluginServiceProviderImp.cs](Contextualizer.Core/PluginServiceProviderImp.cs)

## Action Discovery
- Actions, validators, and context providers are discovered via reflection and initialized at startup.
- Action registry and initialization: [Contextualizer.Core/ActionService.cs](Contextualizer.Core/ActionService.cs)

## Built-in Validators & Providers
- JSON content validator: [Contextualizer.Core/Actions/JsonContentValidator.cs](Contextualizer.Core/Actions/JsonContentValidator.cs)
- XML content validator: [Contextualizer.Core/Actions/XmlContentValidator.cs](Contextualizer.Core/Actions/XmlContentValidator.cs)
- JSON context provider: [Contextualizer.Core/Actions/JsonContextProvider.cs](Contextualizer.Core/Actions/JsonContextProvider.cs)
- XML context provider: [Contextualizer.Core/Actions/XmlContextProvider.cs](Contextualizer.Core/Actions/XmlContextProvider.cs)

## Deep Dive
- Plugin details: [docs/wiki/pages/Plugins-Deep-Dive.md](docs/wiki/pages/Plugins-Deep-Dive.md)

## Handler Registration
- Handler types are registered via static `TypeName` properties and stored in a registry.
- Factory logic: [Contextualizer.Core/HandlerFactory.cs](Contextualizer.Core/HandlerFactory.cs)

## Examples
- OpenFile: [Contextualizer.Plugins/OpenFile.cs](Contextualizer.Plugins/OpenFile.cs)
- PrintContextJson: [Contextualizer.Plugins/PrintContextJson.cs](Contextualizer.Plugins/PrintContextJson.cs)
- PrintDetails: [Contextualizer.Plugins/PrintDetails.cs](Contextualizer.Plugins/PrintDetails.cs)
