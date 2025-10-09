# Contextualizer - Action System

## 📋 İçindekiler
- [Genel Bakış](#genel-bakış)
- [ConfigAction Yapısı](#configaction-yapısı)
- [Action Lifecycle](#action-lifecycle)
- [Built-in Actions](#built-in-actions)
- [Action Özellikleri](#action-özellikleri)
- [Inner Actions](#inner-actions)
- [Custom Action Geliştirme](#custom-action-geliştirme)
- [Örnekler](#örnekler)

---

## Genel Bakış

Action System, handler'ların sonunda veya herhangi bir noktada çalışacak aksiyonları tanımlamanıza olanak tanır.

### Temel Kavramlar

**Action** = Handler veya başka bir action tarafından tetiklenen bir operasyon

**Dosyalar**:
- `ActionService.cs`: Action orchestration
- `Dispatcher.cs`: Action dispatching
- `ConfigAction.cs`: Action configuration modeli
- `IAction.cs`: Action interface

### Action Türleri

1. **Built-in Actions**: `copytoclipboard`, `show_notification`, `show_window`
2. **Custom Actions**: Plugin olarak geliştirilen
3. **Inner Actions**: Bir action içinde çalışan nested action'lar

---

## ConfigAction Yapısı

**Dosya**: `Contextualizer.PluginContracts/ConfigAction.cs`

```csharp
public class ConfigAction
{
    // Action adı (unique identifier)
    public string Name { get; set; }
    
    // Kullanıcı onayı gereksinimi
    public bool RequiresConfirmation { get; set; }
    
    // Context'ten hangi key kullanılacak
    public string? Key { get; set; }
    
    // Koşullar
    public Condition Conditions { get; set; }
    
    // Kullanıcı girişleri
    public List<UserInputRequest> UserInputs { get; set; }
    
    // Dinamik değerler
    public Dictionary<string, string> Seeder { get; set; }
    
    // Sabit değerler
    public Dictionary<string, string> ConstantSeeder { get; set; }
    
    // İç içe action'lar
    public List<ConfigAction>? InnerActions { get; set; }
}
```

### JSON Örneği

```json
{
  "name": "copytoclipboard",
  "key": "_formatted_output",
  "requires_confirmation": false,
  "conditions": {
    "key": "$(status)",
    "operator": "equals",
    "value": "success"
  },
  "user_inputs": [
    {
      "key": "custom_note",
      "prompt": "Enter a note:",
      "required": false
    }
  ],
  "seeder": {
    "timestamp": "$func:now.format(yyyy-MM-dd HH:mm:ss)"
  },
  "constant_seeder": {
    "app_name": "Contextualizer"
  },
  "inner_actions": [
    {
      "name": "show_notification",
      "key": "_notification_message"
    }
  ]
}
```

---

## Action Lifecycle

**Dosya**: `Contextualizer.Core/ActionService.cs`

Action çalıştırma akışı:

```csharp
public async Task Action(ConfigAction configAction, ContextWrapper contextWrapper)
{
    // 1. Log başlangıç
    UserFeedback.ShowActivity(LogType.Info, $"Action '{configAction.Name}' started");
    
    // 2. User inputs
    handlerContextProcessor.PromptUserInputsAsync(configAction.UserInputs, contextWrapper);
    
    // 3. Context resolve (seeders)
    handlerContextProcessor.ContextResolve(
        configAction.ConstantSeeder, 
        configAction.Seeder, 
        contextWrapper);
    
    // 4. Condition evaluation
    bool isConditionSuccessed = ConditionEvaluator.EvaluateCondition(
        configAction.Conditions, 
        contextWrapper);
    if (!isConditionSuccessed) {
        UserFeedback.ShowWarning($"Action {configAction.Name} condition failed");
        return;
    }
    
    // 5. Confirmation (if required)
    if (configAction.RequiresConfirmation) {
        bool confirmed = await ShowConfirmationAsync(...);
        if (!confirmed) {
            UserFeedback.ShowWarning($"Action {configAction.Name} cancelled");
            return;
        }
    }
    
    // 6. Execute main action
    await actionInstance.Action(configAction, contextWrapper);
    UserFeedback.ShowSuccess($"Action '{configAction.Name}' finished");
    
    // 7. Execute inner actions (recursive)
    if (configAction.InnerActions != null && configAction.InnerActions.Count > 0) {
        foreach (var innerAction in configAction.InnerActions) {
            await Action(innerAction, contextWrapper);
        }
    }
}
```

### Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│ 1. ACTION START                                         │
│    - Log: "Action 'copytoclipboard' started"          │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 2. USER INPUTS                                          │
│    - Prompt user for each UserInputRequest             │
│    - Validate input (regex, required)                  │
│    - Add to context: context[input.Key] = value        │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 3. SEEDER RESOLUTION                                    │
│    A) constant_seeder → Merge to context               │
│    B) seeder → ReplaceDynamicValues → Merge            │
│       - $file: resolution                               │
│       - $config: resolution                             │
│       - $func: resolution                               │
│       - $() placeholder resolution                      │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 4. CONDITION EVALUATION                                 │
│    - Evaluate conditions                                │
│    - If false → Log warning + Exit                     │
│    - If true → Continue                                 │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 5. CONFIRMATION (if required)                           │
│    - Show confirmation dialog                           │
│    - If cancelled → Log warning + Exit                 │
│    - If confirmed → Continue                            │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 6. EXECUTE MAIN ACTION                                  │
│    - await actionInstance.Action(configAction, context) │
│    - Log: "Action 'copytoclipboard' finished"         │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 7. EXECUTE INNER ACTIONS (if any)                      │
│    - foreach innerAction                                │
│      - await Action(innerAction, context) [RECURSIVE]  │
│    - Log: "All inner actions completed"                │
└─────────────────────────────────────────────────────────┘
```

---

## Built-in Actions

### 1. copytoclipboard

Belirtilen değeri panoya kopyalar ve bildirim gösterir.

**Dosya**: `Contextualizer.Core/Actions/CopyToClipboard.cs`

#### Özellikler

- **Name**: `copytoclipboard`
- **Required Parameter**: `key` (context'ten hangi değer kopyalanacak)
- **Side Effects**: 
  - Clipboard'a değer yazar
  - Toast notification gösterir (5 saniye)

#### Implementation

```csharp
public class CopyToClipboard : IAction
{
    public string Name => "copytoclipboard";
    
    public Task Action(ConfigAction action, ContextWrapper context)
    {
        // 1. Get value from context
        string value = context[action.Key].ToString();
        
        // 2. Copy to clipboard
        serviceProvider.GetService<IClipboardService>().SetText(value);
        
        // 3. Show notification
        serviceProvider.GetService<IUserInteractionService>()
            .ShowNotification(
                $"{action.Key.ToUpper()} : {value} Clipboard kopyalandı.",
                LogType.Info,
                "Clipboard",
                5,
                null);
        
        return Task.CompletedTask;
    }
}
```

#### JSON Kullanımı

```json
{
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    }
  ]
}
```

#### Senaryo Örnekleri

**Regex Handler - Matched Text Kopyalama**:
```json
{
  "type": "Regex",
  "pattern": "(?<order_id>ORD-\\d{6})",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "order_id"
    }
  ]
}
```

**Database Handler - Query Result Kopyalama**:
```json
{
  "type": "Database",
  "query": "SELECT email FROM users WHERE id = @id",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "email0"
    }
  ]
}
```

**Formatted Output Kopyalama**:
```json
{
  "type": "Regex",
  "pattern": "(?<id>\\d+)",
  "output_format": "User ID: $(id) - Generated: $func:now.format(yyyy-MM-dd)",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    }
  ]
}
```

---

### 2. show_notification

Toast bildirimi gösterir.

**Dosya**: `Contextualizer.Core/Actions/ShowNotification.cs`

#### Özellikler

- **Name**: `show_notification`
- **Required Parameter**: `key` (gösterilecek mesaj)
- **Optional Context Keys**:
  - `_notification_title`: Bildirim başlığı (default: "Notification")
  - `_duration`: Süre (saniye, default: 5)

#### Implementation

```csharp
public class ShowNotification : IAction
{
    public string Name => "show_notification";
    
    public Task Action(ConfigAction action, ContextWrapper context)
    {
        // 1. Get title (with fallback)
        var titleNotification = context.TryGetValue(
            ContextKey._notification_title, 
            out var title)
                ? title
                : "Notification";
        
        // 2. Get duration (with fallback)
        var titleDuration = context.TryGetValue(
            ContextKey._duration, 
            out var duration) && int.TryParse(duration, out var parsedDuration)
                ? parsedDuration
                : 5;
        
        // 3. Show notification
        userInteractionService.ShowNotification(
            context[action.Key],
            LogType.Info,
            titleNotification,
            durationInSeconds: titleDuration,
            null);
        
        return Task.CompletedTask;
    }
}
```

#### JSON Kullanımı

**Basit Kullanım**:
```json
{
  "name": "show_notification",
  "key": "message"
}
```

**Title ve Duration ile**:
```json
{
  "name": "show_notification",
  "key": "message",
  "constant_seeder": {
    "_notification_title": "Success",
    "_duration": "10"
  }
}
```

**Dynamic Content**:
```json
{
  "name": "show_notification",
  "key": "notification_message",
  "seeder": {
    "notification_message": "Order $(order_id) has been processed at $func:now.format(HH:mm:ss)"
  }
}
```

#### Senaryo Örnekleri

**Success Notification**:
```json
{
  "actions": [
    {
      "name": "show_notification",
      "key": "success_message",
      "constant_seeder": {
        "success_message": "Operation completed successfully!",
        "_notification_title": "Success",
        "_duration": "5"
      }
    }
  ]
}
```

**Dynamic Notification**:
```json
{
  "actions": [
    {
      "name": "show_notification",
      "key": "notification_text",
      "seeder": {
        "notification_text": "User $(username) logged in at $func:now.format(HH:mm:ss)",
        "_notification_title": "User Activity",
        "_duration": "7"
      }
    }
  ]
}
```

---

### 3. show_window

Yeni tab açar ve içerik gösterir.

**Dosya**: `Contextualizer.Core/Actions/ShowWindow.cs`

#### Özellikler

- **Name**: `show_window`
- **Required Parameters**:
  - `key`: Gösterilecek içerik
  - Handler config: `screen_id`, `title`
- **Optional Handler Config**:
  - `auto_focus_tab`: Tab'a otomatik odaklanma (default: false)
  - `bring_window_to_front`: Pencereyi öne getirme (default: false)

#### Implementation

```csharp
public class ShowWindow : IAction
{
    public string Name => "show_window";
    
    public Task Action(ConfigAction action, ContextWrapper context)
    {
        // 1. Set _body key
        context[ContextKey._body] = context[action.Key];
        
        // 2. Show window
        pluginServiceProvider.GetService<IUserInteractionService>().ShowWindow(
            context._handlerConfig.ScreenId,       // Screen ID
            context._handlerConfig.Title,          // Tab title
            context,                                // Context data
            new(),                                  // Action buttons (empty)
            context._handlerConfig.AutoFocusTab,   // Auto focus
            context._handlerConfig.BringWindowToFront); // Bring to front
        
        return Task.CompletedTask;
    }
}
```

#### JSON Kullanımı

**Markdown Viewer**:
```json
{
  "type": "Database",
  "screen_id": "markdown2",
  "title": "User Details",
  "query": "SELECT name, email FROM users WHERE id = @id",
  "output_format": "# User Details\n\n**Name**: $(name0)\n**Email**: $(email0)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**JSON Formatter**:
```json
{
  "type": "Api",
  "screen_id": "json_formatter",
  "title": "API Response",
  "url": "https://api.example.com/data",
  "actions": [
    {
      "name": "show_window",
      "key": "_self"
    }
  ]
}
```

**Auto Focus Kontrolü**:
```json
{
  "screen_id": "markdown2",
  "title": "Background Report",
  "auto_focus_tab": false,
  "bring_window_to_front": false,
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Desteklenen Screen ID'ler

| Screen ID | Açıklama | Dosya |
|-----------|----------|-------|
| `markdown2` | Markdown görüntüleme | `MarkdownViewer2.xaml` |
| `json_formatter` | JSON formatting | `JsonFormatterView.xaml` |
| `xml_formatter` | XML formatting | `XmlFormatterView.xaml` |
| `plsql_editor` | PL/SQL editor | `PlSqlEditor.xaml` |
| `url_viewer` | URL içerik görüntüleme | `UrlViewer.xaml` |

---

## Action Özellikleri

### 1. Seeder

Context'e yeni değerler ekler (dinamik değerler).

#### constant_seeder

Sabit değerler için (literal).

```json
{
  "constant_seeder": {
    "app_name": "Contextualizer",
    "version": "1.0.0",
    "author": "Your Name"
  }
}
```

**Özellikler**:
- Dinamik değer işlenmez
- Directly context'e merge edilir
- Performance açısından daha hızlıdır

#### seeder

Dinamik değerler için (function, placeholder, file, config).

```json
{
  "seeder": {
    "timestamp": "$func:now.format(yyyy-MM-dd HH:mm:ss)",
    "username": "$func:username",
    "greeting": "Hello, $(username)!",
    "config_value": "$config:database.connection_string",
    "file_content": "$file:path/to/file.txt"
  }
}
```

**Resolution Order**:
1. `$file:` → File content
2. `$config:` → Configuration value
3. `$func:` → Function processor
4. `$()` → Context placeholder

### 2. User Inputs

Action çalıştırılmadan önce kullanıcıdan input alır.

```json
{
  "user_inputs": [
    {
      "key": "custom_note",
      "prompt": "Enter a note:",
      "default_value": "",
      "required": false,
      "validation_regex": "^[a-zA-Z0-9 ]+$"
    },
    {
      "key": "quantity",
      "prompt": "Enter quantity:",
      "required": true,
      "validation_regex": "^\\d+$"
    }
  ]
}
```

**Navigation**:
- **Back**: Önceki input'a dön
- **Next/OK**: Sonraki input'a geç veya tamamla
- **Cancel**: Action'ı iptal et

### 3. Conditions

Action'ın çalışıp çalışmayacağını belirler.

#### Basit Koşul

```json
{
  "conditions": {
    "key": "$(status)",
    "operator": "equals",
    "value": "success"
  }
}
```

#### Kompleks Koşul (AND/OR)

```json
{
  "conditions": {
    "operator": "and",
    "conditions": [
      {
        "key": "$(status)",
        "operator": "equals",
        "value": "success"
      },
      {
        "key": "$(count)",
        "operator": "greater_than",
        "value": "0"
      }
    ]
  }
}
```

**Desteklenen Operatörler**:
- `equals`, `not_equals`
- `greater_than`, `less_than`
- `contains`, `starts_with`, `ends_with`
- `matches_regex`
- `is_empty`, `is_not_empty`
- `and`, `or` (nested)

### 4. Requires Confirmation

Action çalıştırılmadan önce kullanıcı onayı ister.

```json
{
  "name": "copytoclipboard",
  "key": "sensitive_data",
  "requires_confirmation": true
}
```

**Dialog**:
- **Title**: "Action Confirmation"
- **Message**: "Do you want to proceed with action: {action_name}?"
- **Buttons**: Yes, No

---

## Inner Actions

Bir action tamamlandıktan sonra otomatik olarak çalışan nested action'lar.

### Basit Örnek

```json
{
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "_formatted_output",
      "inner_actions": [
        {
          "name": "show_notification",
          "key": "notification_message",
          "constant_seeder": {
            "notification_message": "Copied to clipboard!",
            "_notification_title": "Success"
          }
        }
      ]
    }
  ]
}
```

**Execution Order**:
1. Main action: `copytoclipboard`
2. Inner action: `show_notification`

### Multi-Level Nesting

```json
{
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "inner_actions": [
        {
          "name": "copytoclipboard",
          "key": "_formatted_output",
          "inner_actions": [
            {
              "name": "show_notification",
              "key": "success_msg",
              "constant_seeder": {
                "success_msg": "All actions completed!"
              }
            }
          ]
        }
      ]
    }
  ]
}
```

**Execution Order**:
1. Level 1: `show_window`
2. Level 2: `copytoclipboard`
3. Level 3: `show_notification`

### Sequential Inner Actions

```json
{
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "inner_actions": [
        {
          "name": "show_notification",
          "key": "msg1",
          "constant_seeder": { "msg1": "Step 1 complete" }
        },
        {
          "name": "show_notification",
          "key": "msg2",
          "constant_seeder": { "msg2": "Step 2 complete" }
        },
        {
          "name": "copytoclipboard",
          "key": "_formatted_output"
        }
      ]
    }
  ]
}
```

### Inner Action Error Handling

**Implementation** (ActionService.cs):
```csharp
foreach (var innerAction in configAction.InnerActions)
{
    try
    {
        await Action(innerAction, contextWrapper);
    }
    catch (Exception ex)
    {
        UserFeedback.ShowError($"Error executing inner action '{innerAction.Name}': {ex.Message}");
        // Continue with next inner action even if one fails
    }
}
```

**Behavior**:
- Bir inner action hata verse bile diğerleri çalışmaya devam eder
- Her hata loglanır ve kullanıcıya bildirilir

---

## Custom Action Geliştirme

### 1. IAction Interface

**Dosya**: `Contextualizer.PluginContracts/IAction.cs`

```csharp
public interface IAction
{
    string Name { get; }
    
    void Initialize(IPluginServiceProvider serviceProvider);
    
    Task Action(ConfigAction action, ContextWrapper context);
}
```

### 2. Örnek Custom Action

```csharp
using Contextualizer.PluginContracts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyPlugin
{
    public class SaveToFile : IAction
    {
        private IPluginServiceProvider _serviceProvider;
        
        // 1. Unique name
        public string Name => "save_to_file";
        
        // 2. Initialize
        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        // 3. Action logic
        public async Task Action(ConfigAction action, ContextWrapper context)
        {
            try
            {
                // Get content from context
                string content = context[action.Key];
                
                // Get file path (from seeder or constant_seeder)
                string filePath = context.TryGetValue("file_path", out var path) 
                    ? path 
                    : $"output_{DateTime.Now:yyyyMMddHHmmss}.txt";
                
                // Write to file
                await File.WriteAllTextAsync(filePath, content);
                
                // Show notification
                var ui = _serviceProvider.GetService<IUserInteractionService>();
                ui.ShowNotification(
                    $"Content saved to {filePath}",
                    LogType.Info,
                    "Save Complete",
                    5,
                    null);
                
                // Log
                var logger = _serviceProvider.GetService<ILoggingService>();
                logger?.LogInfo($"File saved: {filePath}", new Dictionary<string, object>
                {
                    ["file_path"] = filePath,
                    ["content_length"] = content.Length
                });
            }
            catch (Exception ex)
            {
                // Error handling
                var ui = _serviceProvider.GetService<IUserInteractionService>();
                ui.ShowNotification(
                    $"Error saving file: {ex.Message}",
                    LogType.Error,
                    "Save Error",
                    10,
                    null);
            }
        }
    }
}
```

### 3. JSON Kullanımı

```json
{
  "actions": [
    {
      "name": "save_to_file",
      "key": "_formatted_output",
      "seeder": {
        "file_path": "C:\\temp\\output_$func:now.format(yyyyMMdd_HHmmss).txt"
      }
    }
  ]
}
```

### 4. Plugin Deployment

1. Projenizi compile edin (DLL)
2. DLL'i Contextualizer klasörüne kopyalayın
3. Uygulama otomatik olarak action'ı yükler
4. `handlers.json`'da kullanın

---

## Örnekler

### Örnek 1: Copy + Notify

```json
{
  "type": "Regex",
  "pattern": "(?<email>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})",
  "output_format": "Email: $(email)",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "email",
      "inner_actions": [
        {
          "name": "show_notification",
          "key": "notification_msg",
          "constant_seeder": {
            "notification_msg": "Email copied to clipboard!",
            "_notification_title": "Success",
            "_duration": "3"
          }
        }
      ]
    }
  ]
}
```

### Örnek 2: Conditional Actions

```json
{
  "type": "Database",
  "query": "SELECT status, message FROM orders WHERE id = @id",
  "actions": [
    {
      "name": "show_notification",
      "key": "message0",
      "conditions": {
        "key": "$(status0)",
        "operator": "equals",
        "value": "success"
      },
      "constant_seeder": {
        "_notification_title": "Order Status"
      }
    },
    {
      "name": "show_notification",
      "key": "error_msg",
      "conditions": {
        "key": "$(status0)",
        "operator": "equals",
        "value": "error"
      },
      "constant_seeder": {
        "error_msg": "Order processing failed!",
        "_notification_title": "Error"
      }
    }
  ]
}
```

### Örnek 3: User Input + Dynamic Content

```json
{
  "type": "Manual",
  "title": "Generate Report",
  "actions": [
    {
      "name": "show_window",
      "key": "report_content",
      "user_inputs": [
        {
          "key": "report_title",
          "prompt": "Enter report title:",
          "required": true
        },
        {
          "key": "date_range",
          "prompt": "Enter date range (days):",
          "default_value": "7",
          "validation_regex": "^\\d+$"
        }
      ],
      "seeder": {
        "start_date": "$func:today.subtract(days,$(date_range)).format(yyyy-MM-dd)",
        "end_date": "$func:today.format(yyyy-MM-dd)",
        "report_content": "# $(report_title)\n\n**Period**: $(start_date) to $(end_date)\n\n**Generated**: $func:now.format(yyyy-MM-dd HH:mm:ss)"
      }
    }
  ]
}
```

### Örnek 4: Multi-Step Workflow

```json
{
  "type": "Api",
  "url": "https://api.example.com/users/$(user_id)",
  "method": "GET",
  "actions": [
    {
      "name": "show_window",
      "key": "user_profile",
      "seeder": {
        "user_profile": "# User Profile\n\n**Name**: $(data.name)\n**Email**: $(data.email)\n**Status**: $(data.status)"
      },
      "inner_actions": [
        {
          "name": "copytoclipboard",
          "key": "data.email",
          "requires_confirmation": true
        },
        {
          "name": "show_notification",
          "key": "completion_msg",
          "constant_seeder": {
            "completion_msg": "User profile loaded and email copied!",
            "_notification_title": "Complete"
          }
        }
      ]
    }
  ]
}
```

### Örnek 5: Kompleks Condition + Multiple Actions

```json
{
  "type": "Database",
  "query": "SELECT count, status FROM inventory WHERE product_id = @id",
  "actions": [
    {
      "name": "show_notification",
      "key": "low_stock_msg",
      "conditions": {
        "operator": "and",
        "conditions": [
          {
            "key": "$(count0)",
            "operator": "less_than",
            "value": "10"
          },
          {
            "key": "$(status0)",
            "operator": "equals",
            "value": "active"
          }
        ]
      },
      "constant_seeder": {
        "low_stock_msg": "⚠️ Low stock alert! Only $(count0) units remaining.",
        "_notification_title": "Inventory Alert",
        "_duration": "10"
      }
    },
    {
      "name": "copytoclipboard",
      "key": "_formatted_output",
      "output_format": "Product ID: $(product_id) | Stock: $(count0) | Status: $(status0)"
    }
  ]
}
```

---

## Best Practices

### ✅ Yapılması Gerekenler

1. **Action Names**: Küçük harf, underscore kullanın (`my_action`)
2. **Key Validation**: `action.Key`'in context'te olduğundan emin olun
3. **Error Handling**: Try-catch kullanın, hataları loglayın
4. **User Feedback**: İşlem sonuçlarını kullanıcıya bildirin
5. **Inner Actions**: Mantıklı sıralama kullanın (örn: copy → notify)
6. **Conditions**: Complex koşullar için `and`/`or` kullanın
7. **Confirmation**: Kritik işlemler için `requires_confirmation: true`

### ❌ Yapılmaması Gerekenler

1. **Hardcoded Values**: Seeder veya constant_seeder kullanın
2. **Unhandled Exceptions**: Her zaman error handling ekleyin
3. **Long-Running Operations**: Action'ları kısa tutun, async kullanın
4. **Direct UI Access**: `IUserInteractionService` kullanın
5. **Missing Keys**: Context'te olmayan key kullanmayın

---

## Sonraki Adımlar

✅ **Action System öğrenildi!** Artık:

1. 🔌 [Plugin Geliştirme](06-plugin-gelistirme.md) ile custom action'lar yazın
2. 🎨 [UI Özellikleri](07-ui-ozellikleri.md) ile kullanıcı arayüzünü keşfedin
3. 📚 [Örnekler](08-ornekler-ve-use-cases.md) ile gerçek senaryolara bakın

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

