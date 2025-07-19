# Contextualizer

Contextualizer is a powerful Windows application that provides context-aware clipboard management and automation capabilities. It allows you to define custom handlers and actions for different types of clipboard content, making it a versatile tool for automating repetitive tasks.

## Features

- **Context-Aware Clipboard Management**: Monitors and captures clipboard content with customizable handlers
- **Multiple Content Handlers**:
  - **Regex Handler**: Process text using regular expressions
  - **File Handler**: Handle file-based operations
  - **Database Handler**: Interact with MSSQL and Oracle databases
  - **API Handler**: Handle HTTP API requests and responses
  - **Lookup Handler**: Perform lookups against predefined data
  - **Custom Handler**: Implement custom handling logic
  - **Manual Handler**: Trigger actions manually
  - **Synthetic Handler**: Generate and handle synthetic content programmatically
  - **Cron Handler**: Schedule and automate handlers using cron expressions with Quartz.NET

- **Plugin System**:
  - Extensible architecture supporting custom plugins
  - Dynamic plugin loading
  - Well-defined plugin contracts
  - Plugin-specific settings management

- **Modern User Interface**:
  - Clean, modern WPF interface with dark/light theme support
  - Tab-based content management
  - Real-time activity logging
  - Toast notifications
  - Support for markdown, JSON, and XML formatting
  - PL/SQL editor with syntax highlighting
  - Markdown viewer with live preview
  - User input dialogs with validation

- **Advanced Function Processing System**:
  - Comprehensive function library with 50+ built-in functions
  - Unix-style pipeline syntax: `$func:{{ input | function1 | function2 }}`
  - Method chaining: `$func:today.add(days,5).format(yyyy-MM-dd)`
  - Context variable substitution: `$(variableName)`
  - Advanced parsing with nested parentheses and quote handling
  - Functions for dates, strings, math, arrays, JSON, URL, web requests, hashing, and more
  - Pipeline-safe processing of placeholders containing special characters

- **Cron Scheduling System**:
  - Enterprise-grade job scheduling using Quartz.NET
  - Standard cron expressions (6-field format with seconds)
  - Timezone support for global deployments
  - Job pause/resume and manual triggering
  - Real-time execution monitoring and logging
  - Seamless integration with all existing handler types
  - Synthetic content generation for scheduled tasks
  - Robust error handling and retry mechanisms

- **Advanced Features**:
  - Theme-aware components
  - Dynamic screen management
  - Context-based content processing
  - Customizable user interactions
  - Toast notifications with actions
  - Confirmation dialogs
  - Multi-line text input support
  - File picker integration
  - Selection lists with multi-select support

## System Requirements

- Windows 10 or later
- .NET 9.0
- For database features:
  - Microsoft SQL Server client (for MSSQL connections)
  - Oracle client (for Oracle connections)

## Configuration

The application uses a JSON configuration file (`handlers.json`) to define handlers and their behaviors. The configuration includes:

- Handler definitions
- Cron scheduling configurations
- Plugin settings
- Database connections
- Lookup data
- Custom actions
- Theme preferences

### Cron Handler Configuration Example

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
  "actions": [
    {
      "name": "simple_print_key",
      "key": "_formatted_output"
    }
  ],
  "output_format": "Daily Report: $(total) new users on $(execution_time)"
}
```

## Project Structure

- **Contextualizer.Core**: Core business logic and handler implementations
- **Contextualizer.PluginContracts**: Interface definitions for the plugin system
- **Contextualizer.Plugins**: Built-in plugin implementations
- **WpfInteractionApp**: WPF-based user interface with modern components
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
   - `IDynamicScreen` for custom UI components
   - `IThemeAware` for theme-aware components

## Dependencies

- SharpHook: For global keyboard hooks
- Dapper: For database operations
- Microsoft.Data.SqlClient: For SQL Server connections
- Oracle.ManagedDataAccess.Core: For Oracle connections
- Markdig: For markdown processing
- Microsoft.Web.WebView2: For web view components
- System.Text.Json: For JSON processing
- Quartz.NET: For enterprise-grade cron scheduling

## License

This project is licensed under the MIT License - see below for details:

```
MIT License

Copyright (c) 2024 Contextualizer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Contributing

We welcome contributions to the Contextualizer project! Here's how you can contribute:

1. **Fork the Repository**
   - Fork the project to your GitHub account

2. **Create a Branch**
   - Create a branch from `master` for your feature or bug fix
   - Use a descriptive name for your branch (e.g., `feature/new-handler` or `fix/database-connection`)

3. **Make Your Changes**
   - Write clean, readable code
   - Follow the existing code style and conventions
   - Add comments where necessary
   - Update documentation if needed

4. **Test Your Changes**
   - Ensure all existing tests pass
   - Add new tests if you're adding new functionality
   - Test the application thoroughly

5. **Submit a Pull Request**
   - Push your changes to your fork
   - Create a Pull Request from your branch to our `master` branch
   - Provide a clear description of the changes
   - Reference any related issues

6. **Code Review**
   - Wait for the maintainers to review your PR
   - Make any requested changes
   - Once approved, your PR will be merged

### Code Style Guidelines

- Follow C# coding conventions
- Use meaningful variable and function names
- Keep methods focused and concise
- Document public APIs
- Add XML comments for public methods and classes
- Implement interfaces for extensibility
- Use dependency injection where appropriate
- Follow SOLID principles

### Reporting Issues

If you find a bug or have a suggestion for improvement:

1. Check if the issue already exists in our issue tracker
2. If not, create a new issue with:
   - A clear title and description
   - Steps to reproduce (for bugs)
   - Expected and actual behavior
   - Screenshots if applicable
   - Any relevant code snippets 