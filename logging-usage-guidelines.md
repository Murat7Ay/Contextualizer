# 📋 Logging Systems Usage Guidelines

## 🎯 **Two Distinct Logging Systems**

### **1. User Activity Feedback (UI Notifications)**
**Purpose:** Show user-friendly activity updates in the UI panel  
**Target:** End users  
**Visibility:** UI activity log panel, console output  

### **2. System Logging (Technical/Analytics)**
**Purpose:** Technical monitoring, debugging, usage analytics  
**Target:** Developers, system administrators  
**Visibility:** Log files, remote analytics endpoints  

---

## 🔧 **When to Use What**

### ✅ **Use UserFeedback / IUserInteractionService.ShowActivityFeedback for:**

#### **Handler Operations**
```csharp
// ✅ User-friendly progress updates
UserFeedback.ShowSuccess("RegexHandler completed successfully");
UserFeedback.ShowError("DatabaseHandler failed to connect");
UserFeedback.ShowWarning("Handler cancelled by user");
```

#### **Application Events**
```csharp
// ✅ Application state changes
UserFeedback.ShowActivity(LogType.Info, "Application started");
UserFeedback.ShowActivity(LogType.Info, "Clipboard monitoring enabled");
UserFeedback.ShowActivity(LogType.Warning, "Settings saved - restart required");
```

#### **User Actions**
```csharp
// ✅ Action results and confirmations
UserFeedback.ShowSuccess("Template installed successfully");
UserFeedback.ShowWarning("Action requires confirmation");
UserFeedback.ShowError("Template installation cancelled");
```

### ✅ **Use ILoggingService for:**

#### **System Monitoring**
```csharp
// ✅ Technical performance data
var logger = ServiceLocator.SafeGet<ILoggingService>();
logger?.LogPerformance("handler_execution", duration, context);
logger?.LogHandlerExecution(handlerName, handlerType, duration, success, context);
```

#### **Error Tracking**
```csharp
// ✅ Detailed error information with stack traces
logger?.LogError("Database connection failed", ex, new Dictionary<string, object>
{
    ["connection_string"] = connectionInfo,
    ["retry_count"] = retryCount,
    ["timeout_ms"] = timeoutMs
});
```

#### **Usage Analytics**
```csharp
// ✅ User behavior tracking
await logger?.LogUserActivityAsync("template_installed", new Dictionary<string, object>
{
    ["template_id"] = templateId,
    ["user_inputs_count"] = userInputs.Count,
    ["installation_duration_ms"] = duration.TotalMilliseconds
});
```

#### **Structured Debugging**
```csharp
// ✅ Rich contextual information
using (logger?.BeginScope("TemplateProcessing", new Dictionary<string, object>
{
    ["template_id"] = templateId,
    ["user_session"] = sessionId
}))
{
    logger?.LogInfo("Template processing started");
    // ... processing logic
    logger?.LogInfo("Template processing completed");
}
```

---

## 🚫 **What NOT to Do**

### ❌ **Don't Mix Purposes**
```csharp
// ❌ Don't use UserFeedback for technical details
UserFeedback.ShowError("NullReferenceException at line 152 in DatabaseHandler.cs");

// ❌ Don't use ILoggingService for user notifications
logger?.LogInfo("Please restart the application");
```

### ❌ **Don't Duplicate Messages**
```csharp
// ❌ Don't log the same event in both systems
UserFeedback.ShowSuccess("Handler completed");
logger?.LogInfo("Handler completed");  // Redundant

// ✅ Instead, use different levels of detail
UserFeedback.ShowSuccess("RegexHandler completed successfully");
logger?.LogPerformance("handler_execution", duration, detailedContext);
```

---

## 📝 **Practical Examples**

### **Handler Execution Pattern**
```csharp
public async Task ExecuteHandler(IHandler handler, ClipboardContent content)
{
    var logger = ServiceLocator.SafeGet<ILoggingService>();
    
    using (logger?.BeginScope("HandlerExecution", new Dictionary<string, object>
    {
        ["handler_name"] = handler.HandlerConfig.Name,
        ["content_type"] = content.IsText ? "text" : "file",
        ["content_length"] = content.Text?.Length ?? 0
    }))
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // ✅ User feedback - simple, friendly
            UserFeedback.ShowActivity(LogType.Info, $"Executing {handler.HandlerConfig.Name}...");
            
            // ✅ Technical logging - detailed context
            logger?.LogInfo("Handler execution started");
            
            // Execute handler
            await handler.ExecuteAsync(content);
            stopwatch.Stop();
            
            // ✅ User feedback - success notification
            UserFeedback.ShowSuccess($"{handler.HandlerConfig.Name} completed successfully");
            
            // ✅ Technical logging - performance data
            logger?.LogPerformance("handler_execution", stopwatch.Elapsed);
            logger?.LogHandlerExecution(handler.HandlerConfig.Name, handler.GetType().Name, 
                                      stopwatch.Elapsed, true, context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // ✅ User feedback - user-friendly error
            UserFeedback.ShowError($"{handler.HandlerConfig.Name} failed");
            
            // ✅ Technical logging - detailed error information
            logger?.LogError("Handler execution failed", ex, new Dictionary<string, object>
            {
                ["execution_time_ms"] = stopwatch.ElapsedMilliseconds,
                ["error_type"] = ex.GetType().Name,
                ["stack_trace"] = ex.StackTrace
            });
            
            throw;
        }
    }
}
```

### **Template Installation Pattern**
```csharp
public async Task<bool> InstallTemplate(string templateId)
{
    var logger = ServiceLocator.SafeGet<ILoggingService>();
    
    using (logger?.BeginScope("TemplateInstallation", new Dictionary<string, object>
    {
        ["template_id"] = templateId,
        ["installation_id"] = Guid.NewGuid().ToString("N")[..8]
    }))
    {
        try
        {
            // ✅ User feedback
            UserFeedback.ShowActivity(LogType.Info, "Installing template...");
            
            // ✅ Technical logging
            logger?.LogInfo("Template installation started");
            
            var template = await LoadTemplate(templateId);
            if (template.TemplateUserInputs?.Any() == true)
            {
                // ✅ User feedback
                UserFeedback.ShowActivity(LogType.Info, "Collecting template configuration...");
                
                var userInputs = ProcessTemplateUserInputs(template.TemplateUserInputs);
                if (!userInputs.Any())
                {
                    // ✅ User feedback
                    UserFeedback.ShowWarning("Template installation cancelled");
                    
                    // ✅ Technical logging
                    logger?.LogInfo("Template installation cancelled by user");
                    return false;
                }
            }
            
            // ✅ User feedback
            UserFeedback.ShowSuccess("Template installed successfully");
            
            // ✅ Technical logging with analytics
            await logger?.LogUserActivityAsync("template_installed", new Dictionary<string, object>
            {
                ["template_id"] = templateId,
                ["installation_duration_ms"] = stopwatch.ElapsedMilliseconds
            });
            
            return true;
        }
        catch (Exception ex)
        {
            // ✅ User feedback
            UserFeedback.ShowError("Template installation failed");
            
            // ✅ Technical logging
            logger?.LogError("Template installation failed", ex);
            return false;
        }
    }
}
```

---

## 🎯 **Summary**

| **Aspect** | **UserFeedback / ShowActivityFeedback** | **ILoggingService** |
|------------|------------------------------------------|---------------------|
| **Purpose** | User notifications | Technical monitoring |
| **Audience** | End users | Developers/Admins |
| **Detail Level** | Simple, friendly | Rich, structured |
| **Visibility** | UI panel, console | Log files, analytics |
| **Context** | User-friendly messages | Technical data |
| **Performance** | Minimal | Comprehensive tracking |

**Golden Rule:** If a user would see it in the UI, use `UserFeedback`. If it's for debugging or analytics, use `ILoggingService`.
