# Contextualizer Logging System Implementation

## 🎯 Overview
Comprehensive logging and usage analytics system implemented for Contextualizer application.

## 📊 Features Implemented

### 1. **Dual Logging Architecture**
- **Local Logging**: Errors, warnings, debug info stored locally
- **Usage Analytics**: User activities sent to remote endpoint for statistics

### 2. **Components Added**

#### Core Interfaces & Services
- `ILoggingService` - Main logging interface
- `LoggingService` - Full implementation with local/remote logging
- `UsageEvent` - Usage analytics data model
- `LoggingConfiguration` - Configuration management

#### Integration Points
- **HandlerManager**: Application lifecycle, handler execution tracking
- **ActionService**: Plugin loading and action execution
- **FunctionProcessor**: Function processing error handling
- **CronScheduler**: Cron job execution and errors

### 3. **Logging Categories**

#### Local File Logging
```
%AppData%/Contextualizer/logs/
├── error_2024-08-12.log      # Critical errors
├── warning_2024-08-12.log    # Warnings 
├── info_2024-08-12.log       # General information
└── debug_2024-08-12.log      # Debug information
```

#### Usage Analytics (Remote)
- Application start/stop events
- Handler execution statistics
- Clipboard capture metrics
- Plugin loading events
- Performance metrics (execution times)

### 4. **Key Features**

#### Security & Privacy
- Anonymous user ID (hashed machine + username)
- Session-based tracking
- No sensitive data in usage logs
- Local errors stay local

#### Performance Monitoring
- Handler execution timing
- Function processing performance
- Plugin loading metrics
- Error rate tracking

#### Log Management
- Automatic log rotation (10MB max per file)
- Configurable retention (5 files max)
- JSON structured logging
- Async remote posting

## 🔧 Configuration

### Default Settings
```csharp
var config = new LoggingConfiguration
{
    EnableLocalLogging = true,
    EnableUsageTracking = true,
    LocalLogPath = "%AppData%/Contextualizer/logs",
    UsageEndpointUrl = "https://your-endpoint.com/api/usage", // TODO: Set actual URL
    MinimumLogLevel = LogLevel.Info,
    MaxLogFileSizeMB = 10,
    MaxLogFileCount = 5,
    EnableDebugMode = false
};
```

### Customization Options
- Log levels (Debug, Info, Warning, Error, Critical)
- Local log path configuration
- Remote endpoint URL
- File rotation settings
- Debug mode toggle

## 📈 Usage Analytics Data Points

### System Events
- `application_start` - App startup
- `application_stop` - App shutdown  
- `cron_scheduler_start` - Cron system start
- `clipboard_capture` - Clipboard content detected

### Handler Metrics
- `handler_execution` - Handler performance data
- Handler success/failure rates
- Execution time distributions
- Content type processing stats

### Sample Usage Event
```json
{
  "eventType": "handler_execution",
  "timestamp": "2024-08-12T10:30:00Z",
  "userId": "a1b2c3d4e5f6g7h8",
  "sessionId": "12345678-1234-1234-1234-123456789012",
  "version": "1.0.0.0",
  "data": {
    "handler_name": "RegexHandler",
    "handler_type": "RegexHandler", 
    "duration_ms": 45.2,
    "success": true,
    "content_length": 150
  }
}
```

## 🚀 Next Steps

### Required Actions
1. **Set Remote Endpoint URL**: Replace placeholder URL in HandlerManager.cs:31
2. **Deploy Endpoint**: Create API endpoint to receive usage data
3. **Test Integration**: Verify local logging and remote posting
4. **Monitor Performance**: Check logging overhead impact

### Optional Enhancements
- Log viewer UI component
- Real-time log streaming
- Error reporting dashboard
- Usage analytics visualization

## 💻 Usage

### For Developers
```csharp
// Get logging service
var logger = ServiceLocator.Get<ILoggingService>();

// Log errors with context
logger.LogError("Something failed", exception, new Dictionary<string, object>
{
    ["user_input"] = userInput,
    ["operation"] = "data_processing"
});

// Track user activities
await logger.LogUserActivityAsync("feature_used", new Dictionary<string, object>
{
    ["feature_name"] = "regex_handler",
    ["execution_time"] = 123.45
});
```

### For System Analysis
- Check `%AppData%/Contextualizer/logs/` for local logs
- Monitor remote endpoint for usage patterns
- Analyze handler performance metrics
- Track error rates and patterns

## 🔄 File Locations

### New Files Created
- `Contextualizer.PluginContracts/ILoggingService.cs`
- `Contextualizer.Core/Services/LoggingService.cs`

### Modified Files
- `Contextualizer.Core/HandlerManager.cs` - Core logging integration
- `Contextualizer.Core/Services/CronScheduler.cs` - Cron logging
- `Contextualizer.Core/ActionService.cs` - Plugin loading logs
- `Contextualizer.Core/FunctionProcessor.cs` - Function error logging

---

✅ **Status**: Implementation Complete
🏗️ **Build**: Successful (warnings only, no errors)
🔧 **Integration**: Fully integrated with existing codebase
📊 **Ready**: For testing and endpoint configuration