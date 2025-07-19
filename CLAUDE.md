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
- **Handler Types**: RegexHandler, FileHandler, DatabaseHandler, ApiHandler, CustomHandler, ManualHandler, SyntheticHandler, CronHandler
- **Dynamic Loading**: Uses `DynamicAssemblyLoader` to load plugins at runtime
- **Cron Scheduling**: Enterprise-grade scheduling system using Quartz.NET for time-based automation

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
- Cron Management UI with real-time job monitoring and control

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
- **Pipeline Syntax**: `$func:{{ input | function1 | function2 | function3 }}` for Unix-style pipelines
- **Method Chaining**: Supports fluent syntax like `$func:today.add(days,5).format(yyyy-MM-dd)`
- **Built-in Functions**:
  - **Date/Time**: `today`, `now`, `yesterday`, `tomorrow` with chaining methods (`add`, `subtract`, `format`)
  - **Utility**: `guid`, `random`, `base64encode`, `base64decode`, `env`, `username`, `computername`
  - **Hash**: `hash.md5`, `hash.sha256`
  - **URL**: `url.encode`, `url.decode`, `url.domain`, `url.path`, `url.query`, `url.combine`
  - **Web**: `web.get`, `web.post`, `web.put`, `web.delete`
  - **IP**: `ip.local`, `ip.public`, `ip.isprivate`, `ip.ispublic`
  - **JSON**: `json.get`, `json.length`, `json.first`, `json.last`, `json.create`
  - **String**: `string.upper`, `string.lower`, `string.trim`, `string.replace`, `string.substring`, `string.contains`, `string.startswith`, `string.endswith`, `string.split`, `string.length`
  - **Math**: `math.add`, `math.subtract`, `math.multiply`, `math.divide`, `math.round`, `math.floor`, `math.ceil`, `math.min`, `math.max`, `math.abs`
  - **Array**: `array.get`, `array.length`, `array.join` (supports negative indexing)
- **Error Handling**: Comprehensive error handling with logging integration
- **Complex Parsing**: Handles nested parentheses, quotes, and complex parameter structures
- **Smart Parser**: Bracket-aware parsing handles nested JSON objects and complex syntax
- **Quote Handling**: Automatic quote stripping for parameters and literal values

### Function Testing

**Comprehensive Test Files Available**:
- **`function_test_complete.md`**: Complete test suite for all function types and chaining
- **`pipeline_test_final.md`**: Unix-style pipeline comprehensive validation (39 test cases)
- **`pipeline_test_simple.md`**: Basic pipeline functionality tests
- **`pipeline_test_arrays.md`**: Array processing specific tests
- **`pipeline_test_json_fix.md`**: JSON pipeline functionality tests

These test files cover all supported functions and provide examples for:
- Basic function usage and parameter handling
- Method chaining and pipeline syntax
- Error handling and edge cases
- Real-world usage patterns and complex transformations
- Integration between different function types

**Usage**: Use any test file as OutputFormat (`$file:path/to/test.md`) to validate function processor capabilities and see live examples of all supported functionality.

### Cron Scheduling System

**CronHandler.cs** - Time-based automation system:
- **Enterprise Scheduling**: Quartz.NET integration with robust job execution and persistence
- **Standard Cron Expressions**: 6-field format supporting seconds-level precision
- **Timezone Support**: Global deployment support with configurable timezones
- **Synthetic Content Generation**: Integrates with existing handler pipeline via SyntheticHandler
- **Job Management**: Enable/disable, manual triggering, and execution monitoring
- **Error Handling**: Comprehensive logging with retry mechanisms and error tracking

**ICronService Interface** - Cron service contract:
- `ScheduleJob(jobId, cronExpression, handlerConfig, timezone)` - Schedule new jobs
- `GetScheduledJobs()` - Retrieve all scheduled jobs with status information
- `SetJobEnabled(jobId, enabled)` - Enable/disable jobs without removing them
- `TriggerJob(jobId)` - Manually trigger job execution for testing
- `IsRunning` - Check scheduler status and health

**CronScheduler Implementation** - Core scheduling service:
- **Quartz.NET Integration**: Enterprise-grade job scheduling with persistence
- **Job Execution Context**: Passes HandlerConfig and execution metadata to handlers
- **Background Service**: Runs independently of UI with proper lifecycle management
- **Memory Management**: Efficient job storage and cleanup mechanisms
- **Thread Safety**: Concurrent job execution with proper synchronization

**Configuration Example**:
```json
{
  "name": "Daily Database Report",
  "type": "cron",
  "cron_job_id": "daily_report",
  "cron_expression": "0 0 8 * * ?",
  "cron_timezone": "Europe/Istanbul",
  "cron_enabled": true,
  "actual_type": "database",
  "connectionString": "Server=localhost;Database=MyDB;Trusted_Connection=True;",
  "connector": "mssql",
  "query": "SELECT COUNT(*) as total FROM Users WHERE created_date = CAST(GETDATE() as DATE)",
  "actions": [{"name": "simple_print_key", "key": "_formatted_output"}],
  "output_format": "Daily Report: $(total) new users on $(execution_time)"
}
```

**Cron Management UI Features**:
- Real-time job status monitoring with color-coded status indicators
- Manual job triggering with confirmation dialogs and user feedback
- Job enable/disable controls with immediate state updates
- Professional table layout showing cron expressions, execution history, and next run times
- Refresh capability for live updates without restart
- Toast notifications for all operations with success/error feedback
- Carbon design system integration for consistent theming
- Proper converter architecture following existing application patterns

### Database Support

- **Database Handlers**: Support for MSSQL and Oracle via Dapper
- **Connection Management**: Configured through handlers.json
- **Query Execution**: Parameterized queries with context variable substitution

### Target Framework

- **.NET 9.0**: All projects target .NET 9.0
- **WPF**: UI application uses net9.0-windows with WPF support
- **Dependencies**: SharpHook for keyboard hooks, Dapper for database access, Markdig for markdown processing, Quartz.NET for enterprise cron scheduling