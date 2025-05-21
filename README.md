# Contextualizer

Contextualizer is a powerful Windows application that provides context-aware clipboard management and automation capabilities. It allows you to define custom handlers and actions for different types of clipboard content, making it a versatile tool for automating repetitive tasks.

## Features

- **Context-Aware Clipboard Management**: Monitors and captures clipboard content with customizable handlers
- **Multiple Content Handlers**:
  - **Regex Handler**: Process text using regular expressions
  - **File Handler**: Handle file-based operations
  - **Database Handler**: Interact with MSSQL and Oracle databases
  - **Lookup Handler**: Perform lookups against predefined data
  - **Custom Handler**: Implement custom handling logic
  - **Manual Handler**: Trigger actions manually

- **Plugin System**:
  - Extensible architecture supporting custom plugins
  - Dynamic plugin loading
  - Well-defined plugin contracts

- **Modern User Interface**:
  - Clean, dark-themed WPF interface
  - Tab-based content management
  - Real-time activity logging
  - Toast notifications
  - Support for markdown, JSON, and XML formatting

## System Requirements

- Windows 10 or later
- .NET 9.0
- For database features:
  - Microsoft SQL Server client (for MSSQL connections)
  - Oracle client (for Oracle connections)

## Configuration

The application uses a JSON configuration file (`handlers.json`) located at `C:\Finder\handlers.json` to define handlers and their behaviors. Plugins should be placed in the `C:\Finder\Plugins` directory.

## Project Structure

- **Contextualizer.Core**: Core business logic and handler implementations
- **Contextualizer.PluginContracts**: Interface definitions for the plugin system
- **Contextualizer.Plugins**: Built-in plugin implementations
- **WpfInteractionApp**: WPF-based user interface
- **Contextualizer.ConsoleApp**: Console application interface (alternative to WPF)

## Development

### Building the Project

1. Clone the repository
2. Open `Contextualizer.sln` in Visual Studio
3. Restore NuGet packages
4. Build the solution

### Creating Custom Plugins

1. Create a new class library project
2. Reference `Contextualizer.PluginContracts`
3. Implement relevant interfaces:
   - `IHandler` for custom handlers
   - `IAction` for custom actions
   - `IHandlerContextProvider` for custom context providers

## Dependencies

- SharpHook: For global keyboard hooks
- Dapper: For database operations
- Microsoft.Data.SqlClient: For SQL Server connections
- Oracle.ManagedDataAccess.Core: For Oracle connections
- Markdig: For markdown processing
- MdXaml: For markdown rendering
- Microsoft.Web.WebView2: For web view components

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here] 