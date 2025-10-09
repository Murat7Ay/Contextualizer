# Contextualizer - Mimari ve Yapı

## 📋 İçindekiler
- [Genel Mimari](#genel-mimari)
- [Proje Yapısı](#proje-yapısı)
- [Temel Bileşenler](#temel-bileşenler)
- [Veri Akışı](#veri-akışı)
- [Servis Mimarisi](#servis-mimarisi)

---

## Genel Mimari

Contextualizer, katmanlı mimari prensiplerini takip eden, modüler bir yapıya sahiptir.

### Mimari Katmanlar

```
┌────────────────────────────────────────────────────────────┐
│                    WpfInteractionApp                       │
│                    (Presentation Layer)                    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │MainWindow│ │Settings  │ │Cron Mgr  │ │Exchange  │    │
│  │          │ │Window    │ │Window    │ │Window    │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘    │
│  ┌────────────────────────────────────────────────┐      │
│  │    WpfUserInteractionService                   │      │
│  │    (IUserInteractionService Implementation)    │      │
│  └────────────────────────────────────────────────┘      │
└─────────────────────────┬──────────────────────────────────┘
                          │
┌─────────────────────────▼──────────────────────────────────┐
│                  Contextualizer.Core                       │
│                   (Business Logic Layer)                   │
│  ┌────────────────────────────────────────────────┐       │
│  │           HandlerManager                       │       │
│  │  ┌──────────────┐      ┌──────────────┐       │       │
│  │  │   Handlers   │      │KeyboardHook  │       │       │
│  │  │   (List)     │      │              │       │       │
│  │  └──────────────┘      └──────────────┘       │       │
│  └────────────────────────────────────────────────┘       │
│  ┌────────────────────────────────────────────────┐       │
│  │         Handler Types (9 types)                │       │
│  │  RegexHandler, DatabaseHandler, ApiHandler,    │       │
│  │  FileHandler, LookupHandler, CustomHandler,    │       │
│  │  ManualHandler, SyntheticHandler, CronHandler  │       │
│  └────────────────────────────────────────────────┘       │
│  ┌────────────────────────────────────────────────┐       │
│  │         Processing Components                  │       │
│  │  FunctionProcessor, HandlerContextProcessor,   │       │
│  │  ConditionEvaluator, ActionService             │       │
│  └────────────────────────────────────────────────┘       │
│  ┌────────────────────────────────────────────────┐       │
│  │              Services                          │       │
│  │  ConfigurationService, LoggingService,         │       │
│  │  CronScheduler                                 │       │
│  └────────────────────────────────────────────────┘       │
└─────────────────────────┬──────────────────────────────────┘
                          │
┌─────────────────────────▼──────────────────────────────────┐
│          Contextualizer.PluginContracts                    │
│                   (Contract Layer)                         │
│  ┌────────────────────────────────────────────────┐       │
│  │  Core Interfaces                               │       │
│  │  IHandler, IAction, IContextProvider,          │       │
│  │  IContextValidator, IUserInteractionService,   │       │
│  │  ILoggingService, ICronService                 │       │
│  └────────────────────────────────────────────────┘       │
│  ┌────────────────────────────────────────────────┐       │
│  │  Data Models                                   │       │
│  │  HandlerConfig, ConfigAction, ClipboardContent,│       │
│  │  UserInputRequest, Condition, ContextWrapper   │       │
│  └────────────────────────────────────────────────┘       │
│  ┌────────────────────────────────────────────────┐       │
│  │  Shared Utilities                              │       │
│  │  ConnectionManager, DapperRepository,          │       │
│  │  KeyboardSimulator, WindowsClipboard           │       │
│  └────────────────────────────────────────────────┘       │
└────────────────────────────────────────────────────────────┘
```

### Katman Sorumlulukları

#### 1. WpfInteractionApp (Presentation)
- **Sorumluluklar**:
  - Kullanıcı arayüzü (UI) yönetimi
  - Kullanıcı etkileşimleri
  - Veri görselleştirme
  - Theme yönetimi
- **Bağımlılıklar**: 
  - Contextualizer.Core
  - Contextualizer.PluginContracts

#### 2. Contextualizer.Core (Business Logic)
- **Sorumluluklar**:
  - İş mantığı uygulama
  - Handler lifecycle yönetimi
  - Pano izleme ve yakalama
  - Function processing
  - Condition evaluation
  - Cron scheduling
- **Bağımlılıklar**:
  - Contextualizer.PluginContracts

#### 3. Contextualizer.PluginContracts (Contracts)
- **Sorumluluklar**:
  - Interface tanımlamaları
  - Data model tanımlamaları
  - Paylaşılan yardımcı sınıflar
- **Bağımlılıklar**: 
  - Hiçbiri (base layer)

---

## Proje Yapısı

### Contextualizer.Core

```
Contextualizer.Core/
├── Handlers/
│   ├── ApiHandler.cs                 # REST API handler
│   ├── DatabaseHandler.cs            # SQL/Oracle handler
│   ├── FileHandler.cs                # Dosya properties handler
│   ├── LookupHandler.cs              # Key-value lookup handler
│   ├── RegexHandler.cs               # Pattern matching handler
│   ├── CustomHandler.cs              # Plugin-based handler
│   ├── ManualHandler.cs              # UI-triggered handler
│   ├── SyntheticHandler.cs           # Meta-handler
│   └── CronHandler.cs                # Scheduled handler
├── Actions/
│   ├── CopyToClipboard.cs            # Panoya kopyalama
│   ├── ShowNotification.cs           # Toast bildirimi
│   ├── ShowWindow.cs                 # Sekme açma
│   ├── JsonContentValidator.cs       # JSON validation
│   ├── JsonContextProvider.cs        # JSON context creation
│   ├── XmlContentValidator.cs        # XML validation
│   └── XmlContextProvider.cs         # XML context creation
├── Services/
│   ├── ConfigurationService.cs       # Config yönetimi
│   ├── LoggingService.cs             # Loglama servisi
│   ├── CronScheduler.cs              # Cron job yönetimi
│   └── ISettingsService.cs           # Settings interface
├── Processing/
│   ├── FunctionProcessor.cs          # Function çalıştırma (1817 satır!)
│   ├── HandlerContextProcessor.cs    # Context processing
│   ├── ConditionEvaluator.cs         # Koşul değerlendirme
│   └── ActionService.cs              # Action orchestration
├── Infrastructure/
│   ├── HandlerManager.cs             # Handler lifecycle
│   ├── HandlerFactory.cs             # Handler instantiation
│   ├── HandlerLoader.cs              # JSON'dan handler yükleme
│   ├── DynamicAssemblyLoader.cs      # Plugin loading
│   ├── ServiceLocator.cs             # Dependency injection
│   ├── KeyboardHook.cs               # Global shortcut
│   ├── Dispatch.cs                   # Base handler class
│   └── Dispatcher.cs                 # Action dispatcher
├── UserFeedback.cs                   # User notification helper
└── ClipboardCapturedEventArgs.cs     # Event args
```

### Contextualizer.PluginContracts

```
Contextualizer.PluginContracts/
├── Interfaces/
│   ├── IHandler.cs                   # Handler interface
│   ├── IAction.cs                    # Action interface
│   ├── IContextProvider.cs           # Context creation interface
│   ├── IContextValidator.cs          # Validation interface
│   ├── IUserInteractionService.cs    # UI interaction interface
│   ├── ILoggingService.cs            # Logging interface
│   ├── ICronService.cs               # Cron service interface
│   ├── IConfigurationService.cs      # Config service interface
│   ├── ITriggerableHandler.cs        # Manual trigger marker
│   ├── ISyntheticContent.cs          # Synthetic content creation
│   ├── IThemeAware.cs                # Theme change notification
│   ├── IHandlerExchange.cs           # Marketplace interface
│   └── IPluginServiceProvider.cs     # Service provider for plugins
├── Models/
│   ├── HandlerConfig.cs              # Handler configuration
│   ├── ConfigAction.cs               # Action configuration
│   ├── ClipboardContent.cs           # Clipboard data model
│   ├── UserInputRequest.cs           # User input config
│   ├── Condition.cs                  # Condition definition
│   ├── ContextWrapper.cs             # Context dictionary wrapper
│   ├── ContextKey.cs                 # Special context keys
│   ├── FileInfoKeys.cs               # File property keys
│   ├── ToastAction.cs                # Toast button actions
│   └── LogType.cs                    # Log level enum
├── Utilities/
│   ├── ConnectionManager.cs          # DB connection pooling
│   ├── DapperRepository.cs           # Dapper operations
│   ├── KeyboardSimulator.cs          # Keyboard simulation
│   ├── WindowsClipboard.cs           # Clipboard operations
│   └── WindowsClipboardService.cs    # Clipboard service
└── README.md
```

### WpfInteractionApp

```
WpfInteractionApp/
├── MainWindow.xaml                   # Ana pencere
├── MainWindow.xaml.cs                # Main window logic
├── App.xaml                          # Application definition
├── App.xaml.cs                       # Application startup
├── WpfUserInteractionService.cs      # IUserInteractionService impl
├── Settings/
│   ├── SettingsWindow.xaml           # Ayarlar penceresi
│   ├── SettingsWindow.xaml.cs
│   ├── LoggingSettingsWindow.xaml    # Log ayarları
│   └── LoggingSettingsWindow.xaml.cs
├── Windows/
│   ├── HandlerExchangeWindow.xaml    # Marketplace
│   ├── HandlerExchangeWindow.xaml.cs
│   ├── CronManagerWindow.xaml        # Cron yönetimi
│   ├── CronManagerWindow.xaml.cs
│   ├── UserInputDialog.xaml          # Kullanıcı girişi
│   ├── UserInputDialog.xaml.cs
│   ├── ConfirmationDialog.xaml       # Onay dialogu
│   ├── ConfirmationDialog.xaml.cs
│   ├── NetworkUpdateWindow.xaml      # Update penceresi
│   └── NetworkUpdateWindow.xaml.cs
├── Screens/
│   ├── MarkdownViewer2.xaml          # Markdown gösterici
│   ├── MarkdownViewer2.xaml.cs
│   ├── JsonFormatterView.xaml        # JSON formatter
│   ├── JsonFormatterView.xaml.cs
│   ├── XmlFormatterView.xaml         # XML formatter
│   ├── XmlFormatterView.xaml.cs
│   ├── PlSqlEditor.xaml              # PL/SQL editor
│   ├── PlSqlEditor.xaml.cs
│   ├── UrlViewer.xaml                # URL viewer
│   └── UrlViewer.xaml.cs
├── Themes/
│   ├── CarbonDark.xaml               # Dark theme
│   ├── CarbonLight.xaml              # Light theme
│   ├── CarbonDim.xaml                # Dim theme
│   └── CarbonStyles.xaml             # Shared styles
├── Converters/                       # XAML converters
├── Services/
│   ├── ThemeManager.cs               # Theme switching
│   ├── NetworkUpdateService.cs       # Update checker
│   └── FileHandlerExchange.cs        # Marketplace logic
├── Fonts/                            # Custom fonts
├── Assets/                           # Images, icons
└── IDynamicScreen.cs                 # Dynamic screen interface
```

---

## Temel Bileşenler

### 1. HandlerManager

**Dosya**: `Contextualizer.Core/HandlerManager.cs`

HandlerManager, tüm handler'ların yaşam döngüsünü yönetir.

#### Sorumluluklar
- Handler'ları JSON'dan yükler
- Keyboard hook'u başlatır
- Pano içeriğini yakalar
- Handler'ları paralel çalıştırır
- Manual handler'ları yönetir
- Cron job handler'larını execute eder

#### Önemli Metodlar

```csharp
public class HandlerManager : IDisposable
{
    // Constructor
    public HandlerManager(
        IUserInteractionService userInteractionService, 
        ISettingsService settingsService)
    {
        // Service registration
        // Handler loading
        // Keyboard hook setup
    }

    // Başlatma
    public async Task StartAsync()
    {
        // Keyboard hook'u başlat
        // Startup logları yaz
    }

    // Pano içeriği yakalandığında
    private async void OnTextCaptured(
        object? sender, 
        ClipboardCapturedEventArgs e)
    {
        // Tüm handler'ları paralel çalıştır
        var handlerTasks = _handlers.Select(h => 
            ExecuteHandlerAsync(h, clipboardContent));
        
        // Sonuçları bekle
        bool[] results = await Task.WhenAll(handlerTasks);
        
        // İstatistikleri logla
    }

    // Tek handler çalıştırma
    private async Task<bool> ExecuteHandlerAsync(
        IHandler handler, 
        ClipboardContent clipboardContent)
    {
        // CanHandle kontrolü + Execute
        // Performance tracking
        // Error handling
    }

    // Manuel handler çalıştırma
    public async Task ExecuteManualHandlerAsync(string handlerName)
    {
        // Handler'ı bul
        // Synthetic content oluştur
        // Execute
    }

    // Cron job handler çalıştırma
    public async Task<string> ExecuteHandlerConfig(HandlerConfig handlerConfig)
    {
        // Temporary handler oluştur
        // Synthetic content oluştur
        // Execute
        // Dispose
    }
}
```

### 2. Dispatch (Base Handler Class)

**Dosya**: `Contextualizer.Core/Dispatch.cs`

Tüm handler'lar için base class. Template Method Pattern uygular.

#### Template Method
```csharp
public abstract class Dispatch
{
    // Template method - final workflow
    public async Task<bool> Execute(ClipboardContent clipboardContent)
    {
        // 1. CanHandle kontrolü
        bool canHandle = await CanHandleAsync(clipboardContent);
        if (!canHandle) return false;

        // 2. Confirmation (if required)
        if (HandlerConfig.RequiresConfirmation)
        {
            bool confirmed = await ShowConfirmation();
            if (!confirmed) return false;
        }

        // 3. Context oluştur
        var context = await CreateContextAsync(clipboardContent);
        var contextWrapper = new ContextWrapper(context, HandlerConfig);

        // 4. Selector key bul
        FindSelectorKey(clipboardContent, contextWrapper);

        // 5. User inputs
        bool isUserCompleted = PromptUserInputs(contextWrapper);
        if (!isUserCompleted) return false;

        // 6. Context resolve (seeders)
        ContextResolve(contextWrapper);

        // 7. Default seeds (_self, _formatted_output)
        ContextDefaultSeed(contextWrapper);

        // 8. Actions'ları çalıştır
        DispatchAction(GetActions(), contextWrapper);

        // 9. Log success
        LogHandlerExecution();

        return true;
    }

    // Alt sınıflar implement etmeli
    protected abstract Task<bool> CanHandleAsync(ClipboardContent clipboardContent);
    protected abstract Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent);
    protected abstract List<ConfigAction> GetActions();
    protected abstract string OutputFormat { get; }
}
```

### 3. FunctionProcessor

**Dosya**: `Contextualizer.Core/FunctionProcessor.cs` (1817 satır!)

Function sisteminin kalbi. 50+ fonksiyon destekler.

#### İşleyiş
```csharp
public static class FunctionProcessor
{
    // Main entry point
    public static string ProcessFunctions(string input, Dictionary<string, string> context)
    {
        // 1. Pipeline functions ($func:{{ }})
        result = ProcessPipelineFunctions(result, context);
        
        // 2. Regular functions ($func:)
        result = ProcessRegularFunctions(result, context);
        
        return result;
    }

    // Pipeline: $func:{{ input | func1 | func2 }}
    private static string ProcessPipelineFunctions(string input, Dictionary<string, string> context)
    {
        // Pipeline başlangıcını bul
        // Kapanışını bul (nested parantez desteği)
        // Her step'i işle
        // Result'ı yerleştir
    }

    // Method chaining: $func:today.add(days,5).format(yyyy-MM-dd)
    private static string ProcessSingleFunction(string functionCall, Dictionary<string, string> context)
    {
        // Chaining var mı kontrol et
        // Parse chained call
        // Her method'u sırayla çalıştır
        // Final result
    }

    // Base functions
    private static object ProcessBaseFunction(string functionName, string[] parameters)
    {
        return functionName.ToLower() switch
        {
            "today" => DateTime.Today,
            "now" => DateTime.Now,
            "guid" => Guid.NewGuid(),
            "random" => ProcessRandomFunction(parameters),
            _ when functionName.StartsWith("hash.") => ProcessHashFunction(...),
            _ when functionName.StartsWith("url.") => ProcessUrlFunction(...),
            _ when functionName.StartsWith("web.") => ProcessWebFunction(...),
            // ... 50+ function
        };
    }
}
```

### 4. HandlerFactory

**Dosya**: `Contextualizer.Core/HandlerFactory.cs`

Handler instantiation için factory pattern.

#### Reflection-Based Discovery
```csharp
public static class HandlerFactory
{
    private static readonly Dictionary<string, Type> _handlerMap;

    static HandlerFactory()
    {
        // Tüm assembly'leri tara
        _handlerMap = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IHandler).IsAssignableFrom(t) && 
                       !t.IsInterface && 
                       !t.IsAbstract)
            .ToDictionary(
                t => (string)t.GetProperty("TypeName", BindingFlags.Public | BindingFlags.Static)
                              ?.GetValue(null)!,
                t => t,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public static IHandler? Create(HandlerConfig config)
    {
        if (_handlerMap.TryGetValue(config.Type, out var handlerType))
        {
            return (IHandler?)Activator.CreateInstance(handlerType, config);
        }
        return null;
    }
}
```

### 5. ActionService

**Dosya**: `Contextualizer.Core/ActionService.cs`

Action orchestration ve plugin management.

#### Sorumluluklar
- Action plugin'lerini yükler
- Context validator'ları yükler
- Context provider'ları yükler
- Action çalıştırır
- Inner action'ları çalıştırır
- Condition evaluation
- User input prompts

```csharp
public class ActionService : IActionService
{
    private readonly Dictionary<string, IAction> _actions = new();
    private readonly Dictionary<string, IContextValidator> _validators = new();
    private readonly Dictionary<string, IContextProvider> _contextProviders = new();

    public async Task Action(ConfigAction configAction, ContextWrapper contextWrapper)
    {
        // 1. User inputs
        PromptUserInputsAsync(configAction.UserInputs, contextWrapper);
        
        // 2. Context resolve
        ContextResolve(configAction.ConstantSeeder, configAction.Seeder, contextWrapper);
        
        // 3. Condition check
        bool isConditionSuccess = EvaluateCondition(configAction.Conditions, contextWrapper);
        if (!isConditionSuccess) return;
        
        // 4. Confirmation
        if (configAction.RequiresConfirmation)
        {
            bool confirmed = await ShowConfirmation();
            if (!confirmed) return;
        }
        
        // 5. Main action
        await _actions[configAction.Name].Action(configAction, contextWrapper);
        
        // 6. Inner actions
        if (configAction.InnerActions != null)
        {
            foreach (var innerAction in configAction.InnerActions)
            {
                await Action(innerAction, contextWrapper);
            }
        }
    }
}
```

---

## Veri Akışı

### 1. Clipboard Capture Flow

```
┌─────────────────────────────────────────────────────────┐
│ 1. USER ACTION                                          │
│    - Win+Shift+C tuşlarına basıldı                     │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 2. KEYBOARD HOOK (KeyboardHook.cs)                     │
│    - SharpHook library ile global hook                 │
│    - KeyDown event                                      │
│    - Ctrl+C tuşunu simulate et                          │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 3. CLIPBOARD MONITORING (WindowsClipboardService)      │
│    - GetText(), GetFiles()                              │
│    - ClipboardContent oluştur                           │
│      • IsText = true                                    │
│      • Text = "captured text"                           │
│    - Original clipboard'u restore et                    │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 4. EVENT RAISE (ClipboardCapturedEventArgs)            │
│    - TextCaptured event fire                            │
│    - HandlerManager.OnTextCaptured()                    │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 5. PARALLEL HANDLER EXECUTION                           │
│    - var handlerTasks = new List<Task<bool>>();        │
│    - foreach (var handler in _handlers)                 │
│      handlerTasks.Add(ExecuteHandlerAsync(handler))     │
│    - bool[] results = await Task.WhenAll(handlerTasks) │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 6. HANDLER EXECUTION (Dispatch.Execute)                │
│    - CanHandle()                                        │
│    - CreateContext()                                    │
│    - User inputs                                        │
│    - Actions                                            │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ 7. UI UPDATE                                            │
│    - Activity log entry                                 │
│    - Toast notification (opsiyonel)                     │
│    - Tab açma (opsiyonel)                               │
└─────────────────────────────────────────────────────────┘
```

### 2. Handler Execution Flow

```
┌─────────────────────────────────────────────────────────┐
│ HANDLER.Execute(clipboardContent)                       │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ CanHandleAsync()                                        │
│  ├─ RegexHandler: Regex.IsMatch()                      │
│  ├─ DatabaseHandler: Query validation + Regex          │
│  ├─ FileHandler: Extension check                        │
│  ├─ LookupHandler: _data.ContainsKey()                 │
│  └─ ApiHandler: Optional regex or always true          │
└────────────────┬────────────────────────────────────────┘
                 ▼ (if true)
┌─────────────────────────────────────────────────────────┐
│ RequiresConfirmation?                                   │
│  └─ ShowConfirmationAsync("title", "message")          │
└────────────────┬────────────────────────────────────────┘
                 ▼ (if confirmed or not required)
┌─────────────────────────────────────────────────────────┐
│ CreateContextAsync()                                    │
│  ├─ RegexHandler: Extract groups                       │
│  ├─ DatabaseHandler: Execute SQL query                 │
│  ├─ FileHandler: Read file properties                  │
│  ├─ LookupHandler: Get values from _data              │
│  └─ ApiHandler: HTTP request + JSON flatten            │
│  Result: Dictionary<string, string> context            │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ ContextWrapper oluştur                                  │
│  - ReadOnlyDictionary wrapper                           │
│  - HandlerConfig reference                              │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ FindSelectorKey()                                       │
│  - Context'te clipboard text'e eşit değer ara          │
│  - _selector_key = "bulunan_key_adı"                   │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ PromptUserInputsWithNavigation()                       │
│  - Her user_input için dialog göster                   │
│  - Back/Next/Cancel navigation                          │
│  - Validation (regex, required)                         │
│  - Context'e ekle: context[input.Key] = userInput      │
└────────────────┬────────────────────────────────────────┘
                 ▼ (if completed)
┌─────────────────────────────────────────────────────────┐
│ ContextResolve()                                        │
│  ├─ constant_seeder → context'e merge                  │
│  ├─ seeder → ReplaceDynamicValues() → context'e merge  │
│  └─ Tüm context values → ReplaceDynamicValues()        │
│      Resolution order:                                  │
│      1. $file: → File.ReadAllText()                    │
│      2. $config: → ConfigService.GetValue()            │
│      3. $func: → FunctionProcessor.ProcessFunctions()  │
│      4. $() → Context placeholder replacement           │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ ContextDefaultSeed()                                    │
│  ├─ _self = JsonSerializer.Serialize(context)          │
│  └─ _formatted_output = ReplaceDynamicValues(          │
│                           HandlerConfig.OutputFormat)   │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ DispatchAction(GetActions(), context)                  │
│  └─ Dispatcher.DispatchAction(action, context)         │
│      └─ ActionService.Action(action, context)          │
│          ├─ User inputs (action level)                  │
│          ├─ Seeder (action level)                       │
│          ├─ Condition check                             │
│          ├─ Confirmation (action level)                 │
│          ├─ Main action execute                         │
│          └─ Inner actions (recursive)                   │
└────────────────┬────────────────────────────────────────┘
                 ▼
┌─────────────────────────────────────────────────────────┐
│ LogHandlerExecution()                                   │
│  - ILoggingService.LogHandlerExecution()                │
│  - Duration, success, metadata                          │
└─────────────────────────────────────────────────────────┘
```

---

## Servis Mimarisi

### Service Locator Pattern

**Dosya**: `Contextualizer.Core/ServiceLocator.cs`

Basit bir dependency injection container.

```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    // Register service
    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    // Get service (throws if not found)
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }

    // Safe get (returns null if not found)
    public static T? SafeGet<T>() where T : class
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
    }
}
```

### Kayıtlı Servisler

#### Başlangıçta (App.xaml.cs)
```csharp
// Settings
ServiceLocator.Register<ISettingsService>(settingsService);

// Logging
ServiceLocator.Register<ILoggingService>(loggingService);

// Configuration
ServiceLocator.Register<IConfigurationService>(configurationService);

// Network update
ServiceLocator.Register<NetworkUpdateService>(networkUpdateService);

// Cron scheduler
ServiceLocator.Register<ICronService>(cronScheduler);

// UI interaction (after MainWindow created)
ServiceLocator.Register<IUserInteractionService>(userInteractionService);

// Handler manager (after initialization)
ServiceLocator.Register<HandlerManager>(handlerManager);
```

#### HandlerManager Constructor'da
```csharp
// Action service
ServiceLocator.Register<IActionService>(actionService);

// Clipboard service
ServiceLocator.Register<IClipboardService>(new WindowsClipboardService());
```

### Servis Kullanımı

```csharp
// Handler içinde
var logger = ServiceLocator.SafeGet<ILoggingService>();
logger?.LogInfo("Handler executed");

// UI interaction
var ui = ServiceLocator.Get<IUserInteractionService>();
ui.ShowWindow("markdown2", "Title", context);

// Configuration
var config = ServiceLocator.SafeGet<IConfigurationService>();
string value = config?.GetValue("database.connection_string");
```

---

## Sonraki Adımlar

✅ **Mimari öğrenildi!** Artık:

1. 🔧 [Handler Geliştirme Rehberi](03-handler-gelistirme-rehberi.md) ile kendi handler'larınızı yazın
2. ⚡ [Function System](04-function-system.md) ile dinamik değerler oluşturun
3. 🎯 [Action System](05-action-system.md) ile aksiyonlar tanımlayın

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

