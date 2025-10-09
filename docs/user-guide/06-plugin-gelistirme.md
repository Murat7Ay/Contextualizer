# Contextualizer - Plugin Geliştirme Rehberi

## 📋 İçindekiler
- [Genel Bakış](#genel-bakış)
- [Plugin Türleri](#plugin-türleri)
- [IAction Plugin](#iaction-plugin)
- [IContextValidator Plugin](#icontextvalidator-plugin)
- [IContextProvider Plugin](#icontextprovider-plugin)
- [Plugin Deployment](#plugin-deployment)
- [Best Practices](#best-practices)
- [Örnekler](#örnekler)

---

## Genel Bakış

Contextualizer, plugin-based bir mimari ile genişletilebilir. Kendi plugin'lerinizi yazarak sisteme yeni özellikler ekleyebilirsiniz.

### Plugin Sistemi Mimarisi

```
┌─────────────────────────────────────────────────────────┐
│ YOUR PLUGIN DLL                                         │
│  - YourAction.cs (IAction)                             │
│  - YourValidator.cs (IContextValidator)                │
│  - YourContextProvider.cs (IContextProvider)           │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼ (Reference)
┌─────────────────────────────────────────────────────────┐
│ Contextualizer.PluginContracts.dll                     │
│  - IAction                                              │
│  - IContextValidator                                    │
│  - IContextProvider                                     │
│  - IPluginServiceProvider                               │
│  - Models (ClipboardContent, ContextWrapper, etc.)      │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼ (Loaded by)
┌─────────────────────────────────────────────────────────┐
│ Contextualizer.Core                                     │
│  - ActionService (Plugin discovery & loading)           │
│  - DynamicAssemblyLoader                                │
└─────────────────────────────────────────────────────────┘
```

### Plugin Discovery

**Dosya**: `Contextualizer.Core/ActionService.cs`

```csharp
private void LoadActions()
{
    // Tüm assembly'leri tara
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    
    foreach (var assembly in assemblies)
    {
        // 1. IAction implementasyonlarını bul
        var actionTypes = assembly.GetTypes()
            .Where(t => typeof(IAction).IsAssignableFrom(t) && 
                       !t.IsInterface && 
                       !t.IsAbstract);
        
        foreach (var type in actionTypes)
        {
            var instance = (IAction)Activator.CreateInstance(type);
            instance.Initialize(new PluginServiceProviderImp());
            _actions[instance.Name] = instance;
        }
        
        // 2. IContextValidator implementasyonlarını bul
        var validatorTypes = assembly.GetTypes()
            .Where(t => typeof(IContextValidator).IsAssignableFrom(t) && 
                       !t.IsInterface && 
                       !t.IsAbstract);
        
        // 3. IContextProvider implementasyonlarını bul
        var contextProviderTypes = assembly.GetTypes()
            .Where(t => typeof(IContextProvider).IsAssignableFrom(t) && 
                       !t.IsInterface && 
                       !t.IsAbstract);
    }
}
```

**Discovery Süreci**:
1. Uygulama başlarken tüm loaded assembly'ler taranır
2. Her interface implementasyonu bulunur
3. Reflection ile instance oluşturulur
4. Dictionary'lere kaydedilir (name → instance)
5. JSON config'de kullanılabilir hale gelir

---

## Plugin Türleri

### 1. IAction

Handler veya başka bir action tarafından çalıştırılan aksiyonlar.

**Dosya**: `Contextualizer.PluginContracts/IAction.cs`

```csharp
public interface IAction
{
    string Name { get; }
    void Initialize(IPluginServiceProvider serviceProvider);
    Task Action(ConfigAction action, ContextWrapper context);
}
```

**Kullanım Alanları**:
- Clipboard işlemleri
- UI etkileşimleri
- Dosya operasyonları
- API çağrıları
- Database işlemleri
- Custom business logic

### 2. IContextValidator

Custom handler'larda `CanHandle` logic'i sağlar.

**Dosya**: `Contextualizer.PluginContracts/IContextValidator.cs`

```csharp
public interface IContextValidator
{
    string Name { get; }
    Task<bool> Validate(ClipboardContent clipboardContent);
    Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig? config);
}
```

**Kullanım Alanları**:
- Format validation (JSON, XML, CSV, etc.)
- Content validation (email, phone, URL, etc.)
- Business rule validation
- Complex pattern matching

### 3. IContextProvider

Custom handler'larda `CreateContext` logic'i sağlar.

**Dosya**: `Contextualizer.PluginContracts/IContextProvider.cs`

```csharp
public interface IContextProvider
{
    string Name { get; }
    Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent);
    Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent, HandlerConfig? config);
}
```

**Kullanım Alanları**:
- Data parsing (JSON, XML, CSV, etc.)
- Data transformation
- External API calls
- Complex context generation

---

## IAction Plugin

### 1. Temel Yapı

```csharp
using Contextualizer.PluginContracts;
using System;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class MyCustomAction : IAction
    {
        private IPluginServiceProvider _serviceProvider;
        
        // 1. Unique name (lowercase, underscore)
        public string Name => "my_custom_action";
        
        // 2. Initialize
        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        // 3. Action logic
        public async Task Action(ConfigAction action, ContextWrapper context)
        {
            // Your code here
        }
    }
}
```

### 2. Service Provider Kullanımı

`IPluginServiceProvider` ile core service'lere erişim:

```csharp
public async Task Action(ConfigAction action, ContextWrapper context)
{
    // 1. Clipboard Service
    var clipboardService = _serviceProvider.GetService<IClipboardService>();
    clipboardService.SetText("Hello World");
    
    // 2. User Interaction Service
    var ui = _serviceProvider.GetService<IUserInteractionService>();
    ui.ShowNotification("Message", LogType.Info, "Title", 5, null);
    
    // 3. Logging Service
    var logger = _serviceProvider.GetService<ILoggingService>();
    logger?.LogInfo("Action executed", new Dictionary<string, object>
    {
        ["action_name"] = Name
    });
    
    // 4. Configuration Service
    var config = _serviceProvider.GetService<IConfigurationService>();
    string value = config?.GetValue("my.setting");
}
```

### 3. Context Kullanımı

```csharp
public async Task Action(ConfigAction action, ContextWrapper context)
{
    // 1. Get value from action.Key
    string mainValue = context[action.Key];
    
    // 2. TryGetValue for safe access
    if (context.TryGetValue("optional_key", out var optionalValue))
    {
        // Use optionalValue
    }
    
    // 3. Access handler config
    string screenId = context._handlerConfig.ScreenId;
    string title = context._handlerConfig.Title;
    
    // 4. Add new values to context
    context["new_key"] = "new_value";
    
    // 5. Get special keys
    string formattedOutput = context[ContextKey._formatted_output];
    string selfJson = context[ContextKey._self];
}
```

### 4. Error Handling

```csharp
public async Task Action(ConfigAction action, ContextWrapper context)
{
    try
    {
        // Your logic
    }
    catch (Exception ex)
    {
        // Log error
        var logger = _serviceProvider.GetService<ILoggingService>();
        logger?.LogError($"Error in {Name}", ex, new Dictionary<string, object>
        {
            ["action_name"] = Name,
            ["key"] = action.Key
        });
        
        // Show error to user
        var ui = _serviceProvider.GetService<IUserInteractionService>();
        ui.ShowNotification(
            $"Error: {ex.Message}",
            LogType.Error,
            "Action Failed",
            10,
            null);
    }
}
```

### 5. Tam Örnek: SaveToFile Action

```csharp
using Contextualizer.PluginContracts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class SaveToFile : IAction
    {
        private IPluginServiceProvider _serviceProvider;
        
        public string Name => "save_to_file";
        
        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public async Task Action(ConfigAction action, ContextWrapper context)
        {
            var logger = _serviceProvider.GetService<ILoggingService>();
            var ui = _serviceProvider.GetService<IUserInteractionService>();
            
            try
            {
                // 1. Get content
                string content = context[action.Key];
                
                // 2. Get file path (from seeder or use default)
                string filePath = context.TryGetValue("file_path", out var path)
                    ? path
                    : Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        $"output_{DateTime.Now:yyyyMMddHHmmss}.txt");
                
                // 3. Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 4. Write to file
                await File.WriteAllTextAsync(filePath, content);
                
                // 5. Success feedback
                ui.ShowNotification(
                    $"Content saved to:\n{filePath}",
                    LogType.Info,
                    "File Saved",
                    7,
                    null);
                
                // 6. Log
                logger?.LogInfo($"File saved successfully", new Dictionary<string, object>
                {
                    ["file_path"] = filePath,
                    ["content_length"] = content.Length,
                    ["action_name"] = Name
                });
                
                // 7. Add file path to context (for inner actions)
                context["saved_file_path"] = filePath;
            }
            catch (Exception ex)
            {
                // Error handling
                logger?.LogError($"Failed to save file", ex, new Dictionary<string, object>
                {
                    ["action_name"] = Name,
                    ["attempted_path"] = context.TryGetValue("file_path", out var p) ? p : "default"
                });
                
                ui.ShowNotification(
                    $"Error saving file:\n{ex.Message}",
                    LogType.Error,
                    "Save Failed",
                    10,
                    null);
            }
        }
    }
}
```

**JSON Kullanımı**:
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

---

## IContextValidator Plugin

### 1. Temel Yapı

```csharp
using Contextualizer.PluginContracts;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class MyValidator : IContextValidator
    {
        public string Name => "my_validator";
        
        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            // Basic validation logic
            bool isValid = /* your logic */;
            return Task.FromResult(isValid);
        }
        
        public Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig? config)
        {
            // Enhanced validation with config
            return Validate(clipboardContent);
        }
    }
}
```

### 2. Örnek: JSON Validator

**Dosya**: `Contextualizer.Core/Actions/JsonContentValidator.cs`

```csharp
using Contextualizer.PluginContracts;
using System.Text.Json;

namespace Contextualizer.Core.Actions
{
    public class JsonContentValidator : IContextValidator
    {
        public string Name => "jsonvalidator";
        
        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            // 1. Check if text content
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
            {
                return Task.FromResult(false);
            }
            
            // 2. Try parse JSON
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(clipboardContent.Text))
                {
                    return Task.FromResult(true);
                }
            }
            catch (JsonException)
            {
                return Task.FromResult(false);
            }
        }
    }
}
```

### 3. Örnek: Email Validator

```csharp
using Contextualizer.PluginContracts;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class EmailValidator : IContextValidator
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        
        public string Name => "email_validator";
        
        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            // Check if text
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
                return Task.FromResult(false);
            
            // Validate email format
            try
            {
                bool isValid = EmailRegex.IsMatch(clipboardContent.Text.Trim());
                return Task.FromResult(isValid);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}
```

### 4. Custom Handler ile Kullanım

```json
{
  "type": "Custom",
  "name": "Email Processor",
  "validator_name": "email_validator",
  "context_provider_name": "email_context_provider",
  "actions": [
    {
      "name": "copytoclipboard",
      "key": "normalized_email"
    }
  ]
}
```

---

## IContextProvider Plugin

### 1. Temel Yapı

```csharp
using Contextualizer.PluginContracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class MyContextProvider : IContextProvider
    {
        public string Name => "my_context_provider";
        
        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            
            // Add input
            context[ContextKey._input] = clipboardContent.Text;
            
            // Your processing logic
            // ...
            
            return Task.FromResult(context);
        }
        
        public Task<Dictionary<string, string>> CreateContext(
            ClipboardContent clipboardContent, 
            HandlerConfig? config)
        {
            // Enhanced version with config
            return CreateContext(clipboardContent);
        }
    }
}
```

### 2. Örnek: JSON Context Provider

**Dosya**: `Contextualizer.Core/Actions/JsonContextProvider.cs`

```csharp
using Contextualizer.PluginContracts;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Contextualizer.Core.Actions
{
    public class JsonContextProvider : IContextProvider
    {
        public string Name => "jsonvalidator";
        
        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = clipboardContent.Text;
            
            try
            {
                // Parse and format JSON
                using (JsonDocument doc = JsonDocument.Parse(clipboardContent.Text))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    
                    string formattedJson = JsonSerializer.Serialize(doc.RootElement, options);
                    context[ContextKey._formatted_output] = formattedJson;
                }
            }
            catch (JsonException ex)
            {
                context[ContextKey._error] = $"Invalid JSON: {ex.Message}";
            }
            
            return Task.FromResult(context);
        }
    }
}
```

### 3. Örnek: CSV Context Provider

```csharp
using Contextualizer.PluginContracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class CsvContextProvider : IContextProvider
    {
        public string Name => "csv_context_provider";
        
        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = clipboardContent.Text;
            
            try
            {
                // Parse CSV
                var lines = clipboardContent.Text.Split('\n')
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();
                
                if (lines.Count == 0)
                {
                    context[ContextKey._error] = "Empty CSV";
                    return Task.FromResult(context);
                }
                
                // First line as headers
                var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
                context["header_count"] = headers.Length.ToString();
                
                for (int i = 0; i < headers.Length; i++)
                {
                    context[$"header{i}"] = headers[i];
                }
                
                // Data rows
                context["row_count"] = (lines.Count - 1).ToString();
                
                for (int rowIndex = 1; rowIndex < lines.Count; rowIndex++)
                {
                    var values = lines[rowIndex].Split(',').Select(v => v.Trim()).ToArray();
                    
                    for (int colIndex = 0; colIndex < values.Length && colIndex < headers.Length; colIndex++)
                    {
                        context[$"{headers[colIndex]}{rowIndex - 1}"] = values[colIndex];
                    }
                }
                
                // Formatted output (Markdown table)
                var markdown = GenerateMarkdownTable(headers, lines.Skip(1).ToList());
                context[ContextKey._formatted_output] = markdown;
            }
            catch (Exception ex)
            {
                context[ContextKey._error] = $"CSV parsing error: {ex.Message}";
            }
            
            return Task.FromResult(context);
        }
        
        private string GenerateMarkdownTable(string[] headers, List<string> rows)
        {
            var sb = new StringBuilder();
            
            // Header row
            sb.AppendLine("| " + string.Join(" | ", headers) + " |");
            
            // Separator row
            sb.AppendLine("|" + string.Join("|", headers.Select(_ => "---")) + "|");
            
            // Data rows
            foreach (var row in rows)
            {
                var values = row.Split(',').Select(v => v.Trim()).ToArray();
                sb.AppendLine("| " + string.Join(" | ", values) + " |");
            }
            
            return sb.ToString();
        }
    }
}
```

**JSON Kullanımı**:
```json
{
  "type": "Custom",
  "validator_name": "csv_validator",
  "context_provider_name": "csv_context_provider",
  "screen_id": "markdown2",
  "title": "CSV Preview",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

---

## Plugin Deployment

### 1. Proje Oluşturma

**Visual Studio / Rider**:

```bash
# 1. Yeni Class Library projesi oluştur (.NET 8.0)
dotnet new classlib -n MyContextualizerPlugins

# 2. Reference ekle
dotnet add reference path/to/Contextualizer.PluginContracts.dll
```

**csproj örneği**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="Contextualizer.PluginContracts">
      <HintPath>..\Contextualizer.PluginContracts\bin\Release\net8.0-windows\Contextualizer.PluginContracts.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
```

### 2. Plugin Geliştirme

```
MyContextualizerPlugins/
├── MyContextualizerPlugins.csproj
├── Actions/
│   ├── SaveToFile.cs
│   ├── SendEmail.cs
│   └── LogToDatabase.cs
├── Validators/
│   ├── EmailValidator.cs
│   └── PhoneValidator.cs
└── ContextProviders/
    ├── EmailContextProvider.cs
    └── PhoneContextProvider.cs
```

### 3. Build

```bash
# Release build
dotnet build -c Release

# Output: bin/Release/net8.0-windows/MyContextualizerPlugins.dll
```

### 4. Deployment

**Option 1: DLL Kopyalama**
```bash
# DLL'i Contextualizer klasörüne kopyala
copy bin\Release\net8.0-windows\MyContextualizerPlugins.dll C:\Path\To\Contextualizer\
```

**Option 2: Post-Build Event** (Otomatik kopyalama)
```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="xcopy /Y &quot;$(TargetPath)&quot; &quot;C:\Path\To\Contextualizer\&quot;" />
</Target>
```

### 5. Verification

1. Contextualizer'ı başlat
2. Log dosyasını kontrol et: `logs/contextualizer.log`
3. Şu satırları ara:
   ```
   [DEBUG] Action loaded: my_custom_action from MyContextualizerPlugins
   [DEBUG] Validator loaded: my_validator from MyContextualizerPlugins
   ```

### 6. JSON Config'de Kullanım

```json
{
  "type": "Regex",
  "pattern": "...",
  "actions": [
    {
      "name": "my_custom_action",
      "key": "some_key"
    }
  ]
}
```

---

## Best Practices

### ✅ Yapılması Gerekenler

1. **Naming Conventions**:
   - Action names: `lowercase_with_underscores`
   - Class names: `PascalCase`
   - Namespace: `YourCompany.Contextualizer.Plugins`

2. **Error Handling**:
   - Her public method'da try-catch
   - Hataları loglayın
   - Kullanıcıya anlamlı error mesajları

3. **Async/Await**:
   - I/O operations için async kullanın
   - `Task.FromResult()` for synchronous operations

4. **Service Usage**:
   - `_serviceProvider` through `Initialize()`
   - Service'leri cache edin (performance)

5. **Context Keys**:
   - Unique key names kullanın
   - `ContextKey` constants kullanın
   - Key existence kontrolü (`TryGetValue`)

6. **Logging**:
   - Important operations'ı loglayın
   - Metadata ekleyin (action name, parameters, etc.)

7. **Documentation**:
   - XML comments ekleyin
   - README.md oluşturun
   - Usage examples verin

### ❌ Yapılmaması Gerekenler

1. **Hardcoded Paths**: Configuration veya context kullanın
2. **Blocking Operations**: Async kullanın
3. **Unhandled Exceptions**: Try-catch ekleyin
4. **Direct UI Access**: `IUserInteractionService` kullanın
5. **Static State**: Instance variables kullanın
6. **Missing Null Checks**: Nullable types ve null checks

### 📝 Code Review Checklist

- [ ] Tüm interface members implement edildi
- [ ] Error handling eklendi
- [ ] Logging eklendi
- [ ] Async/await doğru kullanıldı
- [ ] Service provider doğru kullanıldı
- [ ] Context keys unique
- [ ] XML comments eklendi
- [ ] Unit tests yazıldı (optional ama önerilen)

---

## Örnekler

### Örnek 1: HTTP Post Action

```csharp
using Contextualizer.PluginContracts;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyPlugins
{
    public class HttpPostAction : IAction
    {
        private IPluginServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient = new HttpClient();
        
        public string Name => "http_post";
        
        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public async Task Action(ConfigAction action, ContextWrapper context)
        {
            var logger = _serviceProvider.GetService<ILoggingService>();
            var ui = _serviceProvider.GetService<IUserInteractionService>();
            
            try
            {
                // Get parameters
                string url = context["api_url"];
                string payload = context[action.Key];
                
                // POST request
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                
                response.EnsureSuccessStatusCode();
                
                string responseBody = await response.Content.ReadAsStringAsync();
                
                // Add response to context
                context["http_response"] = responseBody;
                context["http_status_code"] = ((int)response.StatusCode).ToString();
                
                // Success notification
                ui.ShowNotification(
                    $"POST successful: {response.StatusCode}",
                    LogType.Info,
                    "HTTP POST",
                    5,
                    null);
                
                logger?.LogInfo("HTTP POST completed", new Dictionary<string, object>
                {
                    ["url"] = url,
                    ["status_code"] = (int)response.StatusCode
                });
            }
            catch (HttpRequestException ex)
            {
                logger?.LogError("HTTP POST failed", ex);
                ui.ShowNotification($"HTTP error: {ex.Message}", LogType.Error, "Error", 10, null);
            }
        }
    }
}
```

**JSON Kullanımı**:
```json
{
  "actions": [
    {
      "name": "http_post",
      "key": "json_payload",
      "seeder": {
        "api_url": "https://api.example.com/webhook",
        "json_payload": "$func:json.create(event,$(event_type),data,$(data))"
      }
    }
  ]
}
```

### Örnek 2: Image Validator & Context Provider

```csharp
// Validator
public class ImageValidator : IContextValidator
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    
    public string Name => "image_validator";
    
    public Task<bool> Validate(ClipboardContent clipboardContent)
    {
        if (!clipboardContent.IsFile || clipboardContent.FilePaths == null)
            return Task.FromResult(false);
        
        foreach (var filePath in clipboardContent.FilePaths)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            if (!ImageExtensions.Contains(extension))
                return Task.FromResult(false);
        }
        
        return Task.FromResult(true);
    }
}

// Context Provider
public class ImageContextProvider : IContextProvider
{
    public string Name => "image_context_provider";
    
    public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
    {
        var context = new Dictionary<string, string>();
        
        for (int i = 0; i < clipboardContent.FilePaths.Count; i++)
        {
            var filePath = clipboardContent.FilePaths[i];
            var fileInfo = new FileInfo(filePath);
            
            context[$"file_path{i}"] = filePath;
            context[$"file_name{i}"] = fileInfo.Name;
            context[$"file_size{i}"] = fileInfo.Length.ToString();
            context[$"file_extension{i}"] = fileInfo.Extension;
            
            // Read image properties (requires System.Drawing)
            using (var img = Image.FromFile(filePath))
            {
                context[$"image_width{i}"] = img.Width.ToString();
                context[$"image_height{i}"] = img.Height.ToString();
                context[$"image_format{i}"] = img.RawFormat.ToString();
            }
        }
        
        context["image_count"] = clipboardContent.FilePaths.Count.ToString();
        
        return Task.FromResult(context);
    }
}
```

---

## Sonraki Adımlar

✅ **Plugin Geliştirme öğrenildi!** Artık:

1. 🎨 [UI Özellikleri](07-ui-ozellikleri.md) ile arayüz entegrasyonunu öğrenin
2. 📚 [Örnekler](08-ornekler-ve-use-cases.md) ile gerçek senaryolara bakın
3. 🐛 [Troubleshooting](09-troubleshooting-ve-faq.md) ile sorun giderme

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

