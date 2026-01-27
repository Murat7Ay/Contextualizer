# Handlers

## Contract
- `IHandler`: [Contextualizer.PluginContracts/IHandler.cs](Contextualizer.PluginContracts/IHandler.cs)

## Handler Lifecycle
- Selection
- CanHandle
- CreateContext
- Dispatch

## Loading & Type Registration
- Handlers are loaded from handlers.json and instantiated via `HandlerFactory`.
- Each handler exposes a static `TypeName` used for registration and lookup.
- Optional plugin assemblies are loaded from the plugins folder before handler discovery.

## Source References
- Handler loader: [Contextualizer.Core/HandlerLoader.cs](Contextualizer.Core/HandlerLoader.cs)
- Handler factory: [Contextualizer.Core/HandlerFactory.cs](Contextualizer.Core/HandlerFactory.cs)
- Dynamic assembly loading: [Contextualizer.Core/DynamicAssemblyLoader.cs](Contextualizer.Core/DynamicAssemblyLoader.cs)

## Triggerable vs Automatic
- Automatic handlers are executed on clipboard capture.
- `ITriggerableHandler` instances are executed manually by name.
- Manual execution entry point: [Contextualizer.Core/HandlerManager.cs](Contextualizer.Core/HandlerManager.cs)

## Built-in Handler Types (Source)
- File handler: [Contextualizer.Core/FileHandler.cs](Contextualizer.Core/FileHandler.cs)
- Regex handler: [Contextualizer.Core/RegexHandler.cs](Contextualizer.Core/RegexHandler.cs)
- Lookup handler: [Contextualizer.Core/LookupHandler.cs](Contextualizer.Core/LookupHandler.cs)
- Cron handler: [Contextualizer.Core/CronHandler.cs](Contextualizer.Core/CronHandler.cs)
 - API handler: [Contextualizer.Core/ApiHandler.cs](Contextualizer.Core/ApiHandler.cs)
 - Database handler: [Contextualizer.Core/DatabaseHandler.cs](Contextualizer.Core/DatabaseHandler.cs)

## Behavior Summary
- Regex handler: matches clipboard text against a compiled regex and populates groups into context.
- File handler: reads file metadata for each clipboard file and exposes keys like name, size, and dates.
- Lookup handler: loads a lookup file, then maps clipboard input to a preloaded record.
- API handler: builds and executes HTTP requests using normalized config and optional regex groups.
- Database handler: runs parameterized queries (Dapper) and builds result-set context.

## Handler Modules (Advanced)
- HTTP/API handlers: [Contextualizer.Core/Handlers/Api](Contextualizer.Core/Handlers/Api)
- Database handlers: [Contextualizer.Core/Handlers/Database](Contextualizer.Core/Handlers/Database)

## Dedicated Pages
- API Handler: [docs/wiki/pages/API-Handler.md](docs/wiki/pages/API-Handler.md)
- Database Handler: [docs/wiki/pages/Database-Handler.md](docs/wiki/pages/Database-Handler.md)
 - Handler Authoring Checklist: [docs/wiki/pages/Handler-Authoring-Checklist.md](docs/wiki/pages/Handler-Authoring-Checklist.md)

## Loading & Registry
- Loader: [Contextualizer.Core/HandlerLoader.cs](Contextualizer.Core/HandlerLoader.cs)
- Factory: [Contextualizer.Core/HandlerFactory.cs](Contextualizer.Core/HandlerFactory.cs)
