# Contextualizer - UI Özellikleri

## 📋 İçindekiler
- [Genel Bakış](#genel-bakış)
- [Ana Pencere (MainWindow)](#ana-pencere-mainwindow)
- [Theme Sistemi](#theme-sistemi)
- [Dynamic Screens](#dynamic-screens)
- [Toast Notifications](#toast-notifications)
- [Activity Log](#activity-log)
- [User Input Dialogs](#user-input-dialogs)
- [Settings](#settings)

---

## Genel Bakış

Contextualizer, modern bir WPF UI ile Carbon Design System temalarını kullanır.

### Temel Özellikler

- **Tab-based Interface**: Chrome benzeri multi-tab UI
- **Theme Support**: Dark, Light, Dim tema desteği
- **Activity Logging**: Real-time log panel
- **Toast Notifications**: Non-intrusive bildirimler
- **Dynamic Screens**: Pluggable content viewers
- **Responsive Design**: Modern, responsive layout

---

## Ana Pencere (MainWindow)

### Yapı

```
┌────────────────────────────────────────────────────────┐
│ [Settings] [Cron] [Exchange] [Theme] [Manual Handlers]│  Toolbar
├────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────┐   │
│ │ [Tab 1] [Tab 2] [Tab 3 X] [Tab 4 X]            │   │  Tab Bar
│ ├──────────────────────────────────────────────────┤   │
│ │                                                  │   │
│ │             Content Area                         │   │  Content
│ │                                                  │   │
│ ├──────────────────────────────────────────────────┤   │
│ │ Activity Log                                     │   │
│ │ [Search] [Filter]                                │   │  Log Panel
│ │ [Log entries...]                                 │   │
│ └──────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────┘
```

### Tab Özellikleri

- **Chrome-like Tabs**: Dinamik tab açma/kapama
- **Middle-click to Close**: Mouse wheel ile tab kapatma
- **Auto-focus**: Yeni tab açıldığında otomatik odaklanma
- **Bring to Front**: Pencereyi öne getirme

### Welcome Dashboard

Tab olmadığında görünen karşılama ekranı:

```
┌────────────────────────────────────────────────────────┐
│            Welcome to Contextualizer                   │
│                                                        │
│  📊 Statistics                                         │
│     Active Handlers: 15                                │
│     Active Cron Jobs: 3                                │
│                                                        │
│  ⌨️  Keyboard Shortcut: Win+Shift+C                   │
└────────────────────────────────────────────────────────┘
```

---

## Theme Sistemi

### Theme Manager

**Dosya**: `WpfInteractionApp/Services/ThemeManager.cs`

```csharp
public class ThemeManager
{
    private static readonly Lazy<ThemeManager> _instance = new Lazy<ThemeManager>(...);
    public static ThemeManager Instance => _instance.Value;
    
    public event EventHandler<string> ThemeChanged;
    
    public void ApplyTheme(string themeName);
    public void CycleTheme(); // Dark → Light → Dim → Dark
}
```

### Desteklenen Temalar

1. **Dark** (`CarbonDark.xaml`): Varsayılan koyu tema
2. **Light** (`CarbonLight.xaml`): Açık tema
3. **Dim** (`CarbonDim.xaml`): Orta ton tema

### Theme Değiştirme

**Toolbar Button**:
```csharp
private void ToggleTheme_Click(object sender, RoutedEventArgs e)
{
    ThemeManager.Instance.CycleTheme();
}
```

**IThemeAware Interface**:
```csharp
public interface IThemeAware
{
    void OnThemeChanged(string theme);
}
```

Ekranlar `IThemeAware` implement ederse theme değişikliğinde bildirim alır.

---

## Dynamic Screens

### IDynamicScreen Interface

```csharp
public interface IDynamicScreen
{
    string ScreenId { get; }
    void SetScreenInformation(Dictionary<string, string> context);
}
```

### Built-in Screens

| Screen ID | Açıklama | Dosya |
|-----------|----------|-------|
| `markdown2` | Markdown viewer | `MarkdownViewer2.xaml` |
| `json_formatter` | JSON formatter | `JsonFormatterView.xaml` |
| `xml_formatter` | XML formatter | `XmlFormatterView.xaml` |
| `plsql_editor` | PL/SQL editor | `PlSqlEditor.xaml` |
| `url_viewer` | URL content viewer | `UrlViewer.xaml` |

### Örnek: Markdown Viewer

**XAML**:
```xaml
<UserControl x:Class="WpfInteractionApp.MarkdownViewer2"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf">
    <Grid>
        <wv2:WebView2 Name="webView" />
    </Grid>
</UserControl>
```

**Code-behind**:
```csharp
public partial class MarkdownViewer2 : UserControl, IDynamicScreen, IThemeAware
{
    public string ScreenId => "markdown2";
    
    public void SetScreenInformation(Dictionary<string, string> context)
    {
        if (context.TryGetValue(ContextKey._body, out var markdown))
        {
            var html = RenderMarkdownToHtml(markdown);
            webView.NavigateToString(html);
        }
    }
    
    public void OnThemeChanged(string theme)
    {
        // Refresh content with new theme
    }
}
```

---

## Toast Notifications

### ToastNotification Class

**Dosya**: `WpfInteractionApp/ToastNotification.xaml`

```csharp
public partial class ToastNotification : Window
{
    public ToastNotification(
        string message, 
        int durationInSeconds, 
        string title, 
        LogType type,
        ToastAction[] actions)
    {
        // Initialize UI
        // Setup auto-close timer
        // Setup actions
    }
    
    public void Show()
    {
        this.Show();
        StartFadeOutTimer();
    }
}
```

### ToastAction

```csharp
public class ToastAction
{
    public string Text { get; set; }
    public Action Action { get; set; }
    public ToastActionStyle Style { get; set; } // Primary, Secondary, Danger
}
```

### Kullanım

**Basit Notification**:
```csharp
ui.ShowNotification(
    "Operation completed",
    LogType.Info,
    "Success",
    5,
    null);
```

**Multiple Actions**:
```csharp
ui.ShowNotificationWithActions(
    "File saved. What would you like to do?",
    LogType.Info,
    "File Saved",
    10,
    new ToastAction 
    { 
        Text = "Open Folder", 
        Action = () => Process.Start("explorer", folderPath),
        Style = ToastActionStyle.Primary
    },
    new ToastAction 
    { 
        Text = "Copy Path", 
        Action = () => Clipboard.SetText(filePath),
        Style = ToastActionStyle.Secondary
    }
);
```

---

## Activity Log

### Log Panel

**Features**:
- Real-time log entries
- Search functionality
- Log level filtering (Info, Warning, Error, Debug)
- Auto-scroll to newest
- Capacity limit (50 entries)

### LogEntry Model

```csharp
public class LogEntry
{
    public LogType Type { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string AdditionalInfo { get; set; }
}
```

### Log Filtering

```csharp
private void FilterLogs()
{
    var filtered = _logs.Where(log =>
    {
        // Text search
        bool matchesSearch = string.IsNullOrEmpty(_logSearchText) ||
            log.Message.Contains(_logSearchText, StringComparison.OrdinalIgnoreCase);
        
        // Level filter
        bool matchesLevel = _selectedLogLevel == null || log.Type == _selectedLogLevel;
        
        return matchesSearch && matchesLevel;
    }).ToList();
    
    _filteredLogs.Clear();
    foreach (var log in filtered)
        _filteredLogs.Add(log);
}
```

---

## User Input Dialogs

### UserInputDialog

**Features**:
- Multi-step input collection
- Back/Next/Cancel navigation
- Regex validation
- Required field validation
- Default values

### Kullanım

```csharp
var result = await ui.GetUserInputWithNavigation(
    new List<UserInputRequest>
    {
        new UserInputRequest
        {
            Key = "name",
            Prompt = "Enter your name:",
            Required = true,
            ValidationRegex = "^[a-zA-Z ]+$"
        },
        new UserInputRequest
        {
            Key = "email",
            Prompt = "Enter your email:",
            Required = true,
            ValidationRegex = @"^[^@]+@[^@]+\.[^@]+$"
        }
    },
    context);
```

### ConfirmationDialog

```csharp
bool confirmed = await ui.ShowConfirmationAsync(
    "Delete File",
    "Are you sure you want to delete this file?");
```

---

## Settings

### SettingsWindow

**Features**:
- Keyboard shortcut configuration
- Handler configuration path
- Auto-focus tab setting
- Window bring-to-front setting
- Logging preferences

### Settings Service

```csharp
public class SettingsService : ISettingsService
{
    public T GetSetting<T>(string key, T defaultValue);
    public void SetSetting<T>(string key, T value);
    public void Save();
    public void Load();
}
```

### App Settings (appsettings.json)

```json
{
  "keyboard_shortcut": "Win+Shift+C",
  "handlers_path": "handlers.json",
  "auto_focus_tab": true,
  "bring_window_to_front": true,
  "logging": {
    "enabled": true,
    "level": "Info",
    "file_path": "logs/contextualizer.log"
  }
}
```

---

## Sonraki Adımlar

✅ **UI Özellikleri öğrenildi!** Artık:

1. 📚 [Örnekler](08-ornekler-ve-use-cases.md) ile gerçek senaryolara bakın
2. 🐛 [Troubleshooting](09-troubleshooting-ve-faq.md) ile sorun giderme

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

