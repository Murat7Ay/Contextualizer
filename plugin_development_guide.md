# 🔧 Contextualizer Plugin Geliştirme Kılavuzu

## 📖 İçindekiler
1. [Plugin Mimarisi](#plugin-mimarisi)
2. [Handler Geliştirme](#handler-geliştirme)
3. [Action Geliştirme](#action-geliştirme)
4. [UI Screen Geliştirme](#ui-screen-geliştirme)
5. [Context Provider](#context-provider)
6. [Content Validator](#content-validator)
7. [Exchange Package](#exchange-package)
8. [Test ve Deployment](#test-ve-deployment)

## 🏗️ Plugin Mimarisi

### 📦 Temel Yapı
```
MyPlugin/
├── MyPlugin.csproj
├── Handlers/
│   └── MyCustomHandler.cs
├── Actions/
│   └── MyCustomAction.cs
├── Validators/
│   └── MyContentValidator.cs
├── Providers/
│   └── MyContextProvider.cs
└── Package/
    └── my-plugin-package.json
```

### 🔗 Gerekli Referanslar
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="../Contextualizer.PluginContracts/Contextualizer.PluginContracts.csproj" />
  </ItemGroup>
</Project>
```

## 🎯 Handler Geliştirme

### 1. 📝 Temel Handler Template
```csharp
using Contextualizer.PluginContracts;

namespace MyPlugin.Handlers
{
    public class MyCustomHandler : Dispatch, IHandler
    {
        public MyCustomHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
        }

        // Handler türünü belirtir (handlers.json'da kullanılır)
        public static string TypeName => "my_custom";

        public HandlerConfig HandlerConfig => base.HandlerConfig;

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        // İçeriği handle edip edemeyeceğini kontrol eder
        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            // Özel mantığınızı burada implement edin
            return clipboardContent.IsText && 
                   !string.IsNullOrEmpty(clipboardContent.Text);
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }

        // Context'i oluşturur (verileri işler)
        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            
            // Input'u context'e ekle
            context[ContextKey._input] = clipboardContent.Text;
            
            // Özel işlemlerinizi burada yapın
            await ProcessCustomLogic(clipboardContent.Text, context);
            
            return context;
        }

        // Çalıştırılacak action'ları döner
        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }

        private async Task ProcessCustomLogic(string input, Dictionary<string, string> context)
        {
            // Özel iş mantığınız
            context["processed_data"] = input.ToUpper();
            context["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
```

### 2. 🌐 API Handler Örneği
```csharp
public class WeatherApiHandler : Dispatch, IHandler
{
    private readonly HttpClient _httpClient;

    public WeatherApiHandler(HandlerConfig handlerConfig) : base(handlerConfig)
    {
        _httpClient = new HttpClient();
    }

    public static string TypeName => "weather_api";

    protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
    {
        // Şehir ismi formatını kontrol et
        return Regex.IsMatch(clipboardContent.Text, @"^[a-zA-Z\s]{2,50}$");
    }

    protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
    {
        var context = new Dictionary<string, string>();
        var cityName = clipboardContent.Text.Trim();
        
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={HandlerConfig.ApiKey}");
            
            var weatherData = JsonSerializer.Deserialize<WeatherResponse>(response);
            
            context[ContextKey._input] = cityName;
            context["temperature"] = weatherData.Main.Temp.ToString();
            context["description"] = weatherData.Weather[0].Description;
            context["humidity"] = weatherData.Main.Humidity.ToString();
        }
        catch (Exception ex)
        {
            context["error"] = ex.Message;
        }
        
        return context;
    }
}
```

### 3. 🗄️ Database Handler Örneği
```csharp
public class CustomDbHandler : Dispatch, IHandler
{
    public static string TypeName => "custom_db";

    protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
    {
        var context = new Dictionary<string, string>();
        
        using var connection = new SqlConnection(HandlerConfig.ConnectionString);
        await connection.OpenAsync();
        
        using var command = new SqlCommand(HandlerConfig.Query, connection);
        command.Parameters.AddWithValue("@input", clipboardContent.Text);
        
        using var reader = await command.ExecuteReaderAsync();
        var results = new List<Dictionary<string, object>>();
        
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }
        
        context["results"] = JsonSerializer.Serialize(results);
        context["row_count"] = results.Count.ToString();
        
        return context;
    }
}
```

## ⚡ Action Geliştirme

### 1. 📋 Temel Action Template
```csharp
using Contextualizer.PluginContracts;

namespace MyPlugin.Actions
{
    public class MyCustomAction : IAction
    {
        private IPluginServiceProvider _pluginServiceProvider;

        // Action adı (handlers.json'da kullanılır)
        public string Name => "my_custom_action";

        // Action işlemini gerçekleştirir
        public Task Action(ConfigAction action, ContextWrapper context)
        {
            // Action mantığınızı burada implement edin
            var userInteraction = _pluginServiceProvider.GetService<IUserInteractionService>();
            var data = context[action.Key];
            
            // Özel işlemleriniz
            ProcessData(data);
            
            // Kullanıcıya bildirim
            userInteraction.ShowNotification($"İşlem tamamlandı: {data}", LogType.Success);
            
            return Task.CompletedTask;
        }

        // Plugin servisleri ile başlatma
        public void Initialize(IPluginServiceProvider serviceProvider)
        {
            _pluginServiceProvider = serviceProvider;
        }

        private void ProcessData(string data)
        {
            // Özel veri işleme mantığınız
        }
    }
}
```

### 2. 📊 Excel Export Action
```csharp
public class ExcelExportAction : IAction
{
    private IPluginServiceProvider _pluginServiceProvider;
    
    public string Name => "export_to_excel";

    public async Task Action(ConfigAction action, ContextWrapper context)
    {
        var data = context[action.Key];
        var exportPath = action.Parameters?.GetValueOrDefault("export_path", @"C:\temp\export.xlsx");
        
        try
        {
            await ExportToExcel(data, exportPath);
            
            _pluginServiceProvider.GetService<IUserInteractionService>()
                .ShowNotification($"Excel export completed: {exportPath}", LogType.Success);
        }
        catch (Exception ex)
        {
            _pluginServiceProvider.GetService<IUserInteractionService>()
                .ShowNotification($"Export failed: {ex.Message}", LogType.Error);
        }
    }

    public void Initialize(IPluginServiceProvider serviceProvider)
    {
        _pluginServiceProvider = serviceProvider;
    }

    private async Task ExportToExcel(string jsonData, string filePath)
    {
        // Excel export mantığı
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        
        var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData);
        
        // Headers
        if (data.Count > 0)
        {
            var headers = data[0].Keys.ToArray();
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }
            
            // Data rows
            for (int row = 0; row < data.Count; row++)
            {
                var values = data[row].Values.ToArray();
                for (int col = 0; col < values.Length; col++)
                {
                    worksheet.Cell(row + 2, col + 1).Value = values[col]?.ToString();
                }
            }
        }
        
        workbook.SaveAs(filePath);
    }
}
```

### 3. 🔔 Advanced Notification Action
```csharp
public class AdvancedNotificationAction : IAction
{
    private IPluginServiceProvider _pluginServiceProvider;
    
    public string Name => "advanced_notification";

    public Task Action(ConfigAction action, ContextWrapper context)
    {
        var message = context[action.Key];
        var title = action.Parameters?.GetValueOrDefault("title", "Notification");
        var duration = int.Parse(action.Parameters?.GetValueOrDefault("duration", "5"));
        var type = Enum.Parse<LogType>(action.Parameters?.GetValueOrDefault("type", "Info"));
        
        // Action buttons
        var actions = new List<KeyValuePair<string, Action<Dictionary<string, string>>>>();
        
        if (action.Parameters?.ContainsKey("show_details") == true)
        {
            actions.Add(new KeyValuePair<string, Action<Dictionary<string, string>>>(
                "Details", _ => ShowDetails(context)));
        }
        
        if (action.Parameters?.ContainsKey("copy_to_clipboard") == true)
        {
            actions.Add(new KeyValuePair<string, Action<Dictionary<string, string>>>(
                "Copy", _ => CopyToClipboard(message)));
        }
        
        _pluginServiceProvider.GetService<IUserInteractionService>()
            .ShowNotificationWithActions(message, type, title, duration, actions);
        
        return Task.CompletedTask;
    }

    public void Initialize(IPluginServiceProvider serviceProvider)
    {
        _pluginServiceProvider = serviceProvider;
    }

    private void ShowDetails(ContextWrapper context)
    {
        var userInteraction = _pluginServiceProvider.GetService<IUserInteractionService>();
        userInteraction.ShowWindow("markdown2", "Context Details", context);
    }

    private void CopyToClipboard(string text)
    {
        _pluginServiceProvider.GetService<IClipboardService>()
            .SetText(text);
    }
}
```

## 🖥️ UI Screen Geliştirme

### 1. 📝 Temel Screen Template
```csharp
using System.Collections.Generic;
using System.Windows.Controls;

namespace MyPlugin.Screens
{
    public partial class MyCustomScreen : UserControl, IDynamicScreen
    {
        public MyCustomScreen()
        {
            InitializeComponent();
        }

        // Screen ID (handlers.json'da kullanılır)
        public string ScreenId => "my_custom_screen";

        // Context verilerini alır ve UI'yi günceller
        public void SetScreenInformation(Dictionary<string, string> context)
        {
            // Context verilerini UI elementlerine bağla
            if (context.ContainsKey("title"))
            {
                TitleTextBlock.Text = context["title"];
            }
            
            if (context.ContainsKey("data"))
            {
                DataTextBox.Text = context["data"];
            }
            
            // Custom UI logic
            UpdateCustomElements(context);
        }

        private void UpdateCustomElements(Dictionary<string, string> context)
        {
            // Özel UI güncelleme mantığınız
        }
    }
}
```

### 2. 📊 Chart Viewer Screen
```csharp
public partial class ChartViewerScreen : UserControl, IDynamicScreen, IThemeAware
{
    private Chart _chart;
    
    public string ScreenId => "chart_viewer";

    public void SetScreenInformation(Dictionary<string, string> context)
    {
        var chartData = JsonSerializer.Deserialize<ChartData>(context["chart_data"]);
        
        _chart.Series.Clear();
        
        var series = new LineSeries
        {
            Title = chartData.Title,
            Values = new ChartValues<double>(chartData.Values)
        };
        
        _chart.Series.Add(series);
    }

    public void OnThemeChanged(string theme)
    {
        // Tema değişikliklerini handle et
        switch (theme.ToLower())
        {
            case "dark":
                _chart.Background = Brushes.Black;
                _chart.Foreground = Brushes.White;
                break;
            case "light":
                _chart.Background = Brushes.White;
                _chart.Foreground = Brushes.Black;
                break;
        }
    }
}
```

### 3. 🌐 WebView Screen
```csharp
public partial class CustomWebViewScreen : UserControl, IDynamicScreen
{
    private bool _isWebViewInitialized;
    
    public string ScreenId => "custom_webview";

    public async void SetScreenInformation(Dictionary<string, string> context)
    {
        if (!_isWebViewInitialized)
        {
            await InitializeWebView();
        }
        
        if (context.ContainsKey("html_content"))
        {
            await WebView.NavigateToString(context["html_content"]);
        }
        else if (context.ContainsKey("url"))
        {
            await WebView.CoreWebView2.NavigateAsync(context["url"]);
        }
    }

    private async Task InitializeWebView()
    {
        await WebView.EnsureCoreWebView2Async();
        _isWebViewInitialized = true;
        
        // JavaScript bridge
        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        await WebView.CoreWebView2.AddWebResourceRequestedFilterAsync("*", CoreWebView2WebResourceContext.All);
    }

    private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        // Handle JavaScript messages
    }
}
```

## 🔧 Context Provider Geliştirme

### 1. 📝 CSV Context Provider
```csharp
using Contextualizer.PluginContracts;

namespace MyPlugin.Providers
{
    public class CsvContextProvider : IContextProvider
    {
        public string Name => "csv_provider";

        public Dictionary<string, string> CreateContext(HandlerConfig config, ClipboardContent clipboardContent)
        {
            var context = new Dictionary<string, string>();
            
            try
            {
                var lines = clipboardContent.Text.Split('\n');
                var headers = lines[0].Split(',');
                
                context["headers"] = string.Join("|", headers);
                context["row_count"] = (lines.Length - 1).ToString();
                
                for (int i = 1; i < lines.Length && i <= 10; i++) // İlk 10 satır
                {
                    context[$"row_{i}"] = lines[i];
                }
                
                // CSV'yi JSON'a çevir
                var jsonData = ConvertCsvToJson(lines);
                context["json_data"] = jsonData;
            }
            catch (Exception ex)
            {
                context["error"] = ex.Message;
            }
            
            return context;
        }

        private string ConvertCsvToJson(string[] lines)
        {
            if (lines.Length < 2) return "[]";
            
            var headers = lines[0].Split(',');
            var rows = new List<Dictionary<string, string>>();
            
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var row = new Dictionary<string, string>();
                
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    row[headers[j].Trim()] = values[j].Trim();
                }
                
                rows.Add(row);
            }
            
            return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
```

### 2. 🔍 Regex Context Provider
```csharp
public class RegexContextProvider : IContextProvider
{
    public string Name => "regex_provider";

    public Dictionary<string, string> CreateContext(HandlerConfig config, ClipboardContent clipboardContent)
    {
        var context = new Dictionary<string, string>();
        
        if (string.IsNullOrEmpty(config.Regex))
        {
            context["error"] = "Regex pattern not specified";
            return context;
        }
        
        try
        {
            var regex = new Regex(config.Regex, RegexOptions.Compiled);
            var matches = regex.Matches(clipboardContent.Text);
            
            context["match_count"] = matches.Count.ToString();
            
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                context[$"match_{i}"] = match.Value;
                
                // Named groups
                foreach (string groupName in regex.GetGroupNames())
                {
                    if (groupName != "0") // Skip full match group
                    {
                        context[$"match_{i}_{groupName}"] = match.Groups[groupName].Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            context["error"] = ex.Message;
        }
        
        return context;
    }
}
```

## ✅ Content Validator Geliştirme

### 1. 📧 Email Validator
```csharp
using Contextualizer.PluginContracts;

namespace MyPlugin.Validators
{
    public class EmailContentValidator : IContextValidator
    {
        public string Name => "email_validator";

        public bool IsValid(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
                return false;
            
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(clipboardContent.Text.Trim(), emailPattern);
        }

        public ValidationResult ValidateDetailed(ClipboardContent clipboardContent)
        {
            var result = new ValidationResult();
            
            if (!clipboardContent.IsText)
            {
                result.IsValid = false;
                result.ErrorMessage = "Content is not text";
                return result;
            }
            
            var email = clipboardContent.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(email))
            {
                result.IsValid = false;
                result.ErrorMessage = "Email is empty";
                return result;
            }
            
            if (email.Length > 254)
            {
                result.IsValid = false;
                result.ErrorMessage = "Email is too long";
                return result;
            }
            
            var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            if (!Regex.IsMatch(email, emailPattern))
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid email format";
                return result;
            }
            
            result.IsValid = true;
            result.ParsedData = new Dictionary<string, string>
            {
                ["local_part"] = email.Split('@')[0],
                ["domain"] = email.Split('@')[1],
                ["normalized"] = email.ToLowerInvariant()
            };
            
            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, string> ParsedData { get; set; } = new();
    }
}
```

### 2. 📞 Phone Number Validator
```csharp
public class PhoneNumberValidator : IContextValidator
{
    public string Name => "phone_validator";
    
    private readonly string[] _validCountryCodes = { "+90", "+1", "+44", "+49", "+33" };

    public bool IsValid(ClipboardContent clipboardContent)
    {
        if (!clipboardContent.IsText) return false;
        
        var phone = NormalizePhoneNumber(clipboardContent.Text);
        return IsValidPhoneFormat(phone);
    }

    public ValidationResult ValidateDetailed(ClipboardContent clipboardContent)
    {
        var result = new ValidationResult();
        var phone = clipboardContent.Text?.Trim() ?? "";
        
        if (string.IsNullOrWhiteSpace(phone))
        {
            result.IsValid = false;
            result.ErrorMessage = "Phone number is empty";
            return result;
        }
        
        var normalized = NormalizePhoneNumber(phone);
        
        if (!IsValidPhoneFormat(normalized))
        {
            result.IsValid = false;
            result.ErrorMessage = "Invalid phone number format";
            return result;
        }
        
        result.IsValid = true;
        result.ParsedData = new Dictionary<string, string>
        {
            ["original"] = phone,
            ["normalized"] = normalized,
            ["country_code"] = ExtractCountryCode(normalized),
            ["national_number"] = ExtractNationalNumber(normalized)
        };
        
        return result;
    }

    private string NormalizePhoneNumber(string phone)
    {
        // Remove all non-digit characters except +
        return Regex.Replace(phone, @"[^\d+]", "");
    }

    private bool IsValidPhoneFormat(string phone)
    {
        // Basic validation for international format
        return Regex.IsMatch(phone, @"^\+\d{10,15}$");
    }

    private string ExtractCountryCode(string phone)
    {
        var match = Regex.Match(phone, @"^(\+\d{1,4})");
        return match.Success ? match.Groups[1].Value : "";
    }

    private string ExtractNationalNumber(string phone)
    {
        var countryCode = ExtractCountryCode(phone);
        return phone.Substring(countryCode.Length);
    }
}
```

## 📦 Exchange Package Oluşturma

### 1. 📋 Package Manifest
```json
{
  "name": "my-awesome-plugin",
  "display_name": "My Awesome Plugin",
  "description": "A comprehensive plugin for awesome functionality",
  "author": "Your Name",
  "version": "1.0.0",
  "contextualizer_version": ">=2.0.0",
  "tags": ["productivity", "automation", "api"],
  "category": "Utilities",
  "license": "MIT",
  "homepage": "https://github.com/yourname/my-awesome-plugin",
  "repository": "https://github.com/yourname/my-awesome-plugin.git",
  "dependencies": {
    "Newtonsoft.Json": "13.0.3",
    "System.Data.SqlClient": "4.8.5"
  },
  "files": [
    {
      "path": "MyAwesomePlugin.dll",
      "type": "assembly"
    },
    {
      "path": "config/handlers.json", 
      "type": "config"
    },
    {
      "path": "templates/output_template.txt",
      "type": "template"
    }
  ],
  "handler_configs": [
    {
      "name": "Weather Info",
      "type": "weather_api",
      "description": "Get weather information for any city",
      "screen_id": "markdown2",
      "regex": "^[a-zA-Z\\s]{2,50}$",
      "actions": [
        {
          "name": "simple_print_key",
          "key": "_formatted_output"
        }
      ],
      "output_format": "Weather in $(city):\nTemperature: $(temperature)°C\nDescription: $(description)\nHumidity: $(humidity)%"
    }
  ],
  "installation": {
    "pre_install_script": "scripts/pre_install.ps1",
    "post_install_script": "scripts/post_install.ps1",
    "uninstall_script": "scripts/uninstall.ps1"
  },
  "permissions": [
    "network_access",
    "file_system_read",
    "clipboard_access"
  ]
}
```

### 2. 🔧 Installation Scripts

**pre_install.ps1**:
```powershell
# Pre-installation checks
Write-Host "Checking prerequisites for My Awesome Plugin..."

# Check .NET version
$dotnetVersion = dotnet --version
if ($dotnetVersion -lt "9.0") {
    Write-Error ".NET 9.0 or higher is required"
    exit 1
}

# Create necessary directories
$pluginDir = "$env:USERPROFILE\Contextualizer\Plugins\MyAwesomePlugin"
if (!(Test-Path $pluginDir)) {
    New-Item -ItemType Directory -Path $pluginDir -Force
}

Write-Host "Prerequisites check completed successfully"
```

**post_install.ps1**:
```powershell
# Post-installation configuration
Write-Host "Configuring My Awesome Plugin..."

# Copy configuration files
$configSource = "config\handlers.json"
$configDest = "$env:USERPROFILE\Contextualizer\Config\MyAwesome_handlers.json"
Copy-Item $configSource $configDest -Force

# Register plugin
$registryPath = "HKCU:\Software\Contextualizer\Plugins"
if (!(Test-Path $registryPath)) {
    New-Item -Path $registryPath -Force
}
Set-ItemProperty -Path $registryPath -Name "MyAwesomePlugin" -Value "1.0.0"

Write-Host "Plugin installed successfully!"
```

### 3. 📝 README Template
```markdown
# My Awesome Plugin

A powerful Contextualizer plugin that provides awesome functionality.

## Features

- 🌤️ Weather information lookup
- 📊 Data visualization
- 🔔 Advanced notifications
- 📱 Multi-format support

## Installation

1. Download the plugin package
2. Open Contextualizer Exchange
3. Click "Install Package"
4. Select the downloaded file

## Configuration

Configure your API keys in the plugin settings:

```json
{
  "weather_api_key": "your_api_key_here",
  "notification_settings": {
    "default_duration": 5,
    "show_icons": true
  }
}
```

## Usage

### Weather Lookup
1. Copy a city name to clipboard
2. The weather handler will automatically trigger
3. View results in the markdown viewer

### Custom Actions
- Use `weather_export` to export weather data
- Use `advanced_notification` for rich notifications

## Support

- GitHub Issues: https://github.com/yourname/my-awesome-plugin/issues
- Documentation: https://github.com/yourname/my-awesome-plugin/wiki
- Email: support@yourplugin.com

## License

MIT License - see LICENSE file for details
```

## 🧪 Test ve Deployment

### 1. 🔬 Unit Testing
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contextualizer.PluginContracts;

[TestClass]
public class MyCustomHandlerTests
{
    [TestMethod]
    public async Task CanHandle_ValidInput_ReturnsTrue()
    {
        // Arrange
        var config = new HandlerConfig { /* test config */ };
        var handler = new MyCustomHandler(config);
        var clipboardContent = new ClipboardContent 
        { 
            Text = "test input", 
            IsText = true 
        };

        // Act
        var result = await handler.CanHandle(clipboardContent);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CreateContext_ValidInput_ReturnsExpectedContext()
    {
        // Arrange & Act & Assert
        // Test context creation logic
    }
}
```

### 2. 📋 Integration Testing
```csharp
[TestClass]
public class PluginIntegrationTests
{
    private HandlerManager _handlerManager;
    private TestUserInteractionService _testUI;

    [TestInitialize]
    public void Setup()
    {
        _testUI = new TestUserInteractionService();
        // Setup test environment
    }

    [TestMethod]
    public void FullPluginWorkflow_EndToEnd_Success()
    {
        // Test complete plugin workflow
        // From clipboard content to UI display
    }
}

public class TestUserInteractionService : IUserInteractionService
{
    public List<NotificationCall> Notifications { get; } = new();
    
    public void ShowNotification(string message, LogType type = LogType.Info, 
        string title = null, int durationInSeconds = 5, string additionalInfo = null)
    {
        Notifications.Add(new NotificationCall { Message = message, Type = type });
    }
    
    // Implement other interface methods for testing
}
```

### 3. 🚀 Deployment Checklist

- [ ] ✅ Tüm unit testler geçiyor
- [ ] 🔧 Integration testler başarılı
- [ ] 📚 Dökümantasyon tamamlandı
- [ ] 🔒 Güvenlik review yapıldı
- [ ] 📦 Package manifest doğru
- [ ] 🎯 Performance testleri geçti
- [ ] 🌐 Multi-platform compatibility
- [ ] 📋 Version numbering uygun
- [ ] 🔄 CI/CD pipeline hazır
- [ ] 📞 Support channels aktif

### 4. 📊 Performance Optimization

```csharp
// Async patterns kullanın
public async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent content)
{
    // Asynchronous operations
    var result = await ProcessDataAsync(content.Text);
    return result;
}

// Caching implement edin
private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions
{
    SizeLimit = 100
});

// Resource cleanup
public void Dispose()
{
    _httpClient?.Dispose();
    _cache?.Dispose();
}
```

## 🎯 Best Practices

### ✅ Do's
- ✨ Async/await patterns kullanın
- 🔒 Exception handling implement edin
- 📝 Comprehensive logging ekleyin
- 🧪 Unit testler yazın
- 📚 Dökümantasyon oluşturun
- 🔧 Configuration validation yapın
- 🎨 Consistent naming kullanın
- ⚡ Performance optimize edin

### ❌ Don'ts
- 🚫 UI thread'i block etmeyin
- 🚫 Hard-coded values kullanmayın
- 🚫 Memory leaks oluşturmayın
- 🚫 Exception'ları swallow etmeyin
- 🚫 Sensitive data log etmeyin
- 🚫 Synchronous I/O kullanmayın
- 🚫 Global state'e depend etmeyin

## 📞 Destek ve Kaynaklar

- 📖 **API Documentation**: Detaylı interface dökümantasyonu
- 🌐 **Sample Projects**: GitHub'da örnek projeler
- 💬 **Community Forum**: Geliştirici topluluğu
- 🐛 **Bug Reports**: GitHub Issues
- 📧 **Direct Support**: developer@contextualizer.com

---
**🔧 Happy Plugin Development!** 🚀
