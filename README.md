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

- **Template Handler System**:
  - Create reusable handler templates with configurable parameters
  - Template user inputs with navigation support (back/next/cancel)
  - Dynamic placeholder replacement using `$(key)` syntax
  - Installation-time customization for different environments
  - Seamless integration with the existing function processing system

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

- **Advanced Logging & Analytics System**:
  - **Dual Logging Architecture**: Separate UI activity logs and technical system logs
  - **User Activity Tracking**: Comprehensive analytics for "who used what" insights
  - **Smart Handler Execution Logging**: Only logs handlers that actually process content
  - **Parallel Handler Processing**: All handlers execute simultaneously for optimal performance
  - **Usage Analytics**: Track successful executions, failed attempts, and unmatched content
  - **Performance Monitoring**: Detailed execution times and system metrics
  - **Structured Logging**: Rich contextual information with correlation IDs
  - **Asynchronous Logging**: Non-blocking log writes for better performance
  - **Remote Analytics**: Optional usage data collection for product insights
  - **No-Match Detection**: Tracks when users attempt to use the system but no handlers match

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

### Template Handler Configuration Example

Create reusable handler templates that can be customized during installation:

```json
{
  "id": "database-connection-template",
  "name": "Database Connection Template",
  "version": "1.0.0",
  "author": "Contextualizer Team",
  "description": "Template for creating database handlers with custom connection settings",
  "tags": ["database", "template", "mssql"],
  "template_user_inputs": [
    {
      "key": "server_name",
      "title": "Database Server",
      "message": "Enter the SQL Server name or IP address:",
      "is_required": true,
      "default_value": "localhost\\SQLEXPRESS",
      "validation_regex": "^[a-zA-Z0-9\\.\\\\-]+$"
    },
    {
      "key": "database_name",
      "title": "Database Name",
      "message": "Enter the database name:",
      "is_required": true,
      "validation_regex": "^[a-zA-Z0-9_]+$"
    },
    {
      "key": "connection_timeout",
      "title": "Connection Timeout",
      "message": "Enter connection timeout in seconds:",
      "is_required": false,
      "default_value": "30",
      "validation_regex": "^[0-9]+$"
    }
  ],
  "handlerJson": {
    "name": "Custom Database Handler - $(database_name)",
    "type": "manual",
    "screen_id": "database_screen",
    "title": "Query $(database_name) Database",
    "connectionString": "Server=$(server_name);Database=$(database_name);Connection Timeout=$(connection_timeout);Trusted_Connection=True;TrustServerCertificate=True;",
    "connector": "mssql",
    "query": "SELECT TOP 10 * FROM $(database_name).sys.tables",
    "actions": [
      {
        "name": "db_context_enricher"
      },
      {
        "name": "simple_print_key",
        "key": "_self"
      }
    ],
    "user_inputs": [
      {
        "key": "custom_query",
        "title": "Custom SQL Query",
        "message": "Enter your SQL query (use $(table_name) for dynamic values):",
        "is_required": false,
        "is_multi_line": true,
        "default_value": "SELECT * FROM Users WHERE id = 1"
      }
    ],
    "output_format": "Database: $(database_name) | Server: $(server_name) | Results: $(query_result)"
  }
}
```

**Template Installation Process:**
1. User selects template from marketplace
2. System presents step-by-step navigation for template inputs
3. User can navigate back/forward through configuration steps
4. Placeholders in `handlerJson` are replaced with user-provided values
5. Customized handler is installed and ready to use

**Template Placeholder Syntax:**
- `$(key)` - Replaced with user input value during installation
- Supports all existing function processing features
- Can be used in any string field within the handler configuration

## Logging & Analytics System

The application features a comprehensive dual logging system designed for both user experience and technical monitoring:

### Dual Logging Architecture

#### **1. User Activity Feedback (`UserFeedback` / `IUserInteractionService`)**
- **Purpose**: Show user-friendly notifications in the UI activity panel
- **Target Audience**: End users
- **Usage**: Success messages, warnings, errors visible to users
- **Examples**:
  ```csharp
  UserFeedback.ShowSuccess("Handler completed successfully");
  UserFeedback.ShowWarning("No handlers could process the content");
  UserFeedback.ShowError("Database connection failed");
  ```

#### **2. System Logging (`ILoggingService`)**
- **Purpose**: Technical monitoring, debugging, and usage analytics
- **Target Audience**: Developers, system administrators, product analytics
- **Features**:
  - **Asynchronous Processing**: Non-blocking log writes using `System.Threading.Channels`
  - **Structured Logging**: Rich contextual data with correlation IDs
  - **Performance Tracking**: Execution times, system metrics per component
  - **Remote Analytics**: Optional usage data collection
  - **Log Rotation**: Automatic cleanup of old log files
  - **Scoped Logging**: Component-based log contexts

### Smart Handler Execution Logging

The system intelligently tracks handler usage with the following approach:

#### **Parallel Handler Processing**
```csharp
// All handlers execute simultaneously for optimal performance
var handlerTasks = new List<Task<bool>>();
foreach (var handler in _handlers)
{
    var handlerTask = ExecuteHandlerAsync(handler, clipboardContent, logger, contentLength);
    handlerTasks.Add(handlerTask);
}

// Wait for all handlers and count successful ones
bool[] results = await Task.WhenAll(handlerTasks);
int handlersProcessed = results.Count(r => r);
```

#### **Accurate Usage Analytics**
- **Only Successful Executions Logged**: Handlers that cannot process content are not logged as "successful"
- **Comprehensive Tracking**: Both successful executions and "no handlers matched" scenarios
- **Performance Metrics**: Real execution times (not including CanHandle checks)

#### **Example Log Outputs**

**Successful Handler Execution:**
```json
{
  "eventType": "handler_execution",
  "timestamp": "2025-09-18T21:54:10.3741914Z",
  "data": {
    "handler_name": "Database Handler",
    "handler_type": "CustomHandler",
    "duration_ms": 2.47,
    "success": true,
    "content_length": 150,
    "can_handle": true,
    "executed_actions": 2
  }
}
```

**No Handlers Matched:**
```json
{
  "eventType": "clipboard_no_handlers_matched",
  "timestamp": "2025-09-18T21:54:15.1234567Z",
  "data": {
    "content_length": 45,
    "total_handlers_checked": 5,
    "content_type": "text",
    "content_preview": "Some text that no handler could process..."
  }
}
```

### Usage Guidelines

#### **Use UserFeedback for:**
- Handler completion notifications
- User-friendly error messages
- Application state changes
- Action confirmations and warnings

#### **Use ILoggingService for:**
- Performance monitoring and metrics
- Detailed error tracking with stack traces
- Usage analytics and user behavior tracking
- System debugging and troubleshooting
- Remote telemetry data collection

### Benefits

- **Accurate Analytics**: "Who used what" reports show only actual usage
- **Performance Insights**: Real execution times and system bottlenecks
- **User Experience**: Clear feedback on what happened and why
- **Product Intelligence**: Understanding which content types need new handlers
- **Debugging**: Rich contextual information for troubleshooting

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

### Creating Handler Templates

1. **Design Your Template**
   - Identify configurable parameters (connection strings, URLs, etc.)
   - Plan the user input flow and validation requirements
   - Consider default values and help text

2. **Create Template Package**
   ```json
   {
     "id": "unique-template-id",
     "name": "Display Name",
     "version": "1.0.0",
     "author": "Your Name",
     "description": "Template description",
     "tags": ["tag1", "tag2"],
     "template_user_inputs": [
       {
         "key": "parameter_name",
         "title": "User-friendly Title",
         "message": "Help text for user",
         "is_required": true,
         "default_value": "default_value",
         "validation_regex": "^[a-zA-Z0-9]+$"
       }
     ],
     "handlerJson": {
       // Your handler configuration with $(parameter_name) placeholders
     }
   }
   ```

3. **Template User Input Options**
   - `key`: Parameter identifier for replacement
   - `title`: Display title in the UI
   - `message`: Help text or prompt
   - `is_required`: Whether input is mandatory
   - `default_value`: Pre-filled value
   - `validation_regex`: Input validation pattern
   - `is_password`: Hide input for sensitive data
   - `is_multi_line`: Allow multi-line text input
   - `is_selection_list`: Provide dropdown options
   - `is_file_picker`: File/folder selection dialog

4. **Publishing Templates**
   - Place template JSON files in the exchange directory
   - Use `IHandlerExchange.PublishHandlerAsync()` programmatically
   - Templates appear in the marketplace for installation

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