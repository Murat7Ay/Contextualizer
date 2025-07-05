# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Development Commands

### Building the Solution
```bash
dotnet build Contextualizer.sln
```

### Running the Applications
```bash
# WPF Application (primary UI)
dotnet run --project WpfInteractionApp

# Console Application (alternative interface)
dotnet run --project Contextualizer.ConsoleApp
```

### Restore NuGet Packages
```bash
dotnet restore
```

### Clean Build
```bash
dotnet clean
dotnet build
```

## Project Architecture

### Core Components

**Contextualizer.Core** - Contains the main business logic and handler system:
- **Handler Architecture**: Plugin-based system where handlers inherit from `Dispatch` base class
- **Service Locator Pattern**: Central service registry in `ServiceLocator.cs` for dependency injection
- **Handler Types**: RegexHandler, FileHandler, DatabaseHandler, ApiHandler, CustomHandler, ManualHandler, SyntheticHandler
- **Dynamic Loading**: Uses `DynamicAssemblyLoader` to load plugins at runtime

**Contextualizer.PluginContracts** - Interface definitions for extensibility:
- Core interfaces: `IHandler`, `IAction`, `IContentValidator`, `IContextProvider`
- Plugin service access through `IPluginServiceProvider`
- Handler execution pipeline: `CanHandle()` → `CreateContext()` → `DispatchAction()`

**Contextualizer.Plugins** - Built-in plugin implementations:
- Actions: CopyToClipboard, OpenFile, ShowNotification, ShowWindow
- Validators: JsonContentValidator, XmlContentValidator  
- Context Providers: JsonContextProvider, XmlContextProvider

**WpfInteractionApp** - Modern WPF UI with theme support:
- Theme system with dark/light modes in `Services/ThemeManager.cs`
- Dynamic screens implementing `IDynamicScreen` interface
- User interaction dialogs and toast notifications
- PL/SQL editor with syntax highlighting using WebView2

### Handler Execution Flow

```
ClipboardContent → Handler.CanHandle() → Handler.CreateContext() → Dispatcher.DispatchAction() → ActionService.Action()
```

### Key Architecture Patterns

1. **Template Method**: `Dispatch` base class defines execution template
2. **Factory Pattern**: `HandlerFactory` creates handlers from configuration
3. **Plugin Architecture**: Dynamic assembly loading with interface contracts
4. **Service Locator**: Central service registry for dependency resolution
5. **Observer Pattern**: Event-driven clipboard monitoring via `KeyboardHook`

### Configuration System

- **handlers.json**: Central configuration file defining all handlers and their behaviors
- **JSON Schema**: Handlers configured with type, conditions, actions, and user inputs
- **Dynamic Configuration**: Handlers support runtime configuration changes

### Handler Management

- **HandlerManager**: Central coordinator managing handler lifecycle and clipboard events
- **HandlerLoader**: Deserializes JSON configuration into handler instances
- **ActionService**: Manages action discovery and execution across all loaded assemblies

### Plugin Development

To create custom plugins:
1. Reference `Contextualizer.PluginContracts`
2. Implement relevant interfaces (`IHandler`, `IAction`, `IContextProvider`)
3. Use `IPluginServiceProvider` to access core services
4. Deploy assemblies to plugin folder for dynamic loading

### Condition Evaluation System

**ConditionEvaluator.cs** - Advanced condition evaluation engine:
- **Logical Operations**: Supports `and`, `or` operators with nested conditions
- **Comparison Operators**: `equals`, `not_equals`, `greater_than`, `less_than`, `contains`, `starts_with`, `ends_with`, `matches_regex`, `is_empty`, `is_not_empty`
- **Dynamic Value Resolution**: Supports `$(variable)` syntax for context variable substitution
- **Recursive Evaluation**: Handles complex nested condition structures
- **Usage**: Integrated into handler execution pipeline to determine when handlers should execute

### Function Processing System

**FunctionProcessor.cs** - Comprehensive function processing engine:
- **Function Syntax**: `$func:functionName(parameters)` with support for method chaining
- **Method Chaining**: Supports fluent syntax like `$func:today.add(days,5).format(yyyy-MM-dd)`
- **Built-in Functions**:
  - **Date/Time**: `today`, `now`, `yesterday`, `tomorrow` with chaining methods (`add`, `subtract`, `format`)
  - **Utility**: `guid`, `random`, `base64encode`, `base64decode`, `env`, `username`, `computername`
  - **Hash**: `hash.md5`, `hash.sha256`
  - **URL**: `url.encode`, `url.decode`, `url.domain`, `url.path`, `url.query`, `url.combine`
  - **Web**: `web.get`, `web.post`, `web.put`, `web.delete`
  - **IP**: `ip.local`, `ip.public`, `ip.isprivate`, `ip.ispublic`
  - **JSON**: `json.get`, `json.length`, `json.first`, `json.last`, `json.create`
  - **String**: `string.upper`, `string.lower`, `string.trim`, `string.replace`, `string.substring`, `string.contains`, `string.startswith`, `string.endswith`, `string.split`
  - **Math**: `math.add`, `math.subtract`, `math.multiply`, `math.divide`, `math.round`, `math.floor`, `math.ceil`, `math.min`, `math.max`, `math.abs`
  - **Array**: `array.get`, `array.length`, `array.join`
- **Error Handling**: Comprehensive error handling with logging integration
- **Complex Parsing**: Handles nested parentheses, quotes, and complex parameter structures

### Database Support

- **Database Handlers**: Support for MSSQL and Oracle via Dapper
- **Connection Management**: Configured through handlers.json
- **Query Execution**: Parameterized queries with context variable substitution

### Target Framework

- **.NET 9.0**: All projects target .NET 9.0
- **WPF**: UI application uses net9.0-windows with WPF support
- **Dependencies**: SharpHook for keyboard hooks, Dapper for database access, Markdig for markdown processing