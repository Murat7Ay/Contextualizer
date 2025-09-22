# 🎯 Contextualizer Projesi - Kullanım Kılavuzu

## 📖 İçindekiler
1. [Proje Genel Bakış](#proje-genel-bakış)
2. [Handler Türleri](#handler-türleri)
3. [UI Ekranları](#ui-ekranları)
4. [Action'lar](#actionlar)
5. [Cron İşleri](#cron-işleri)
6. [Exchange Marketplace](#exchange-marketplace)
7. [Örnek Kullanımlar](#örnek-kullanımlar)

## 🎯 Proje Genel Bakış

Contextualizer, clipboard'dan yakalanan içeriği işleyerek otomatik görevler gerçekleştiren güçlü bir WPF uygulamasıdır.

### 🏗️ Ana Bileşenler
- **HandlerManager**: Tüm handler'ları yönetir
- **KeyboardHook**: Global klavye kısayollarını yakalar
- **WPF UI**: Modern kullanıcı arayüzü
- **Plugin System**: Genişletilebilir plugin mimarisi

### 🔑 Temel Kavramlar
- **Handler**: Clipboard içeriğini işleyen sınıflar
- **Action**: Handler'ların gerçekleştirdiği işlemler
- **Screen**: Sonuçları gösteren UI ekranları
- **Context**: Handler'lar arası veri paylaşımı

## 🛠️ Handler Türleri

### 1. 📝 **Regex Handler**
Düzenli ifadelerle metin yakalama

```json
{
  "name": "Stack Trace Analysis",
  "type": "regex",
  "regex": "at (\\S+)\\((\\S+\\.java):(\\d+)\\)",
  "groups": ["class_name", "file_name", "line_number"],
  "actions": [{"name": "print_details"}]
}
```

**Kullanım**: Java stack trace'lerini yakalamak için

### 2. 🔍 **Lookup Handler** 
Dosyadan veri arama

```json
{
  "name": "Corp Lookup",
  "type": "lookup",
  "path": "C:\\Finder\\corp_data.txt",
  "delimiter": "||",
  "key_names": ["drivercode", "oid"],
  "value_names": ["drivercode", "oid", "name", "engine"],
  "screen_id": "markdown2"
}
```

**Kullanım**: Kurum kodlarından detay bilgi çekme

### 3. 🌐 **API Handler**
REST API çağrıları

```json
{
  "name": "GitHub User Info",
  "type": "Api",
  "regex": "^[a-zA-Z0-9-]+$",
  "url": "https://api.github.com/users/$(username)",
  "method": "GET",
  "screen_id": "jsonformatter"
}
```

**Kullanım**: GitHub kullanıcı bilgilerini getirme

### 4. 🗄️ **Database Handler**
SQL sorguları

```json
{
  "name": "Strategy Query",
  "type": "database",
  "connectionString": "Server=localhost\\SQLEXPRESS;Database=NorthPole;...",
  "connector": "mssql",
  "query": "SELECT * FROM [Strategy].[Parameter] WHERE Name = @_input",
  "screen_id": "markdown2"
}
```

### 5. 📁 **File Handler**
Dosya işlemleri

```json
{
  "name": "Open File",
  "type": "file",
  "file_extensions": [".html", ".css"],
  "actions": [{"name": "open_file"}, {"name": "print_details"}]
}
```

### 6. ⚡ **Manual Handler**
Kullanıcı etkileşimli

```json
{
  "name": "PL SQL Deneme",
  "type": "manual",
  "screen_id": "plsql_editor",
  "user_inputs": [...],
  "seeder": {"query": "SELECT * FROM DUAL;"}
}
```

### 7. 🔧 **Custom Handler**
Özel validatörler

```json
{
  "name": "Json formatter wpf",
  "type": "custom",
  "context_provider": "jsonvalidator",
  "validator": "jsonvalidator",
  "screen_id": "jsonformatter"
}
```

## 🖥️ UI Ekranları

### 1. 📝 **markdown2** - Markdown Viewer
- **Özellikler**: Markdown rendering, syntax highlighting
- **Kullanım**: Dökümantasyon, metin çıktıları
- **Tema desteği**: Light/Dark/Dim

### 2. 📊 **jsonformatter** - JSON Formatter
- **Özellikler**: JSON formatting, syntax highlighting
- **Butonlar**: Format/Minify toggle
- **Kullanım**: API yanıtları, JSON verileri

### 3. 🏷️ **xmlformatter** - XML Formatter  
- **Özellikler**: XML formatting, validation
- **Kullanım**: XML verileri, konfigürasyonlar

### 4. 💾 **plsql_editor** - PL/SQL Editor
- **Özellikler**: SQL syntax highlighting, ACE editor
- **Kullanım**: SQL sorguları, database işlemleri

### 5. 🌐 **url_viewer** - Web Viewer
- **Özellikler**: WebView2, shared profile
- **Kullanım**: Web sayfaları, Branch viewer

## ⚡ Action'lar

### 1. 📋 **copytoclipboard**
```json
{"name": "copytoclipboard", "key": "oid"}
```
Belirtilen key'i clipboard'a kopyalar

### 2. 📄 **simple_print_key**
```json
{"name": "simple_print_key", "key": "_formatted_output"}
```
Belirtilen key'i ekranda gösterir

### 3. 📊 **print_details**
```json
{"name": "print_details"}
```
Tüm context bilgilerini listeler

### 4. 🗂️ **open_file**
```json
{"name": "open_file"}
```
Dosyayı varsayılan uygulamayla açar

### 5. 🔔 **show_notification**
```json
{"name": "show_notification", "message": "İşlem tamamlandı"}
```
Toast notification gösterir

### 6. 🪟 **show_window**
```json
{"name": "show_window"}
```
Belirtilen screen_id ile pencere açar

## ⏰ Cron İşleri

### 📋 Cron Manager Özellikleri
- **Real-time monitoring**: Canlı durum takibi
- **Manual execution**: Manuel çalıştırma
- **Enable/Disable**: Aktif/pasif yapma
- **Execution history**: Çalışma geçmişi
- **Next run time**: Sonraki çalışma zamanı

### 📝 Cron Syntax
```
* * * * * *
│ │ │ │ │ │
│ │ │ │ │ └─ Yıl (isteğe bağlı)
│ │ │ │ └─── Haftanın günü (0-7)
│ │ │ └───── Ay (1-12)
│ │ └─────── Ayın günü (1-31)
│ └───────── Saat (0-23)
└─────────── Dakika (0-59)
```

### 🔧 Örnek Cron İşi
```json
{
  "name": "Daily Backup",
  "handler_name": "GitHub User Info", 
  "cron_expression": "0 2 * * *",
  "is_enabled": true,
  "synthetic_content": {
    "content": "octocat",
    "content_type": "text/plain"
  }
}
```

## 🛒 Exchange Marketplace

### 📦 Handler Package Yapısı
```json
{
  "name": "Lorem Ipsum API",
  "description": "Lorem ipsum text generator",
  "author": "Contextualizer Team",
  "version": "1.0.0",
  "tags": ["api", "text", "generator"],
  "handler_config": {
    "name": "Lorem Ipsum Generator",
    "type": "Api",
    "url": "https://loripsum.net/api/$(paragraphs)/$(length)",
    "method": "GET"
  }
}
```

### 🔧 Package Özellikleri
- **Install/Uninstall**: Kolay kurulum
- **Version management**: Sürüm kontrolü
- **Dependency tracking**: Bağımlılık yönetimi
- **Auto-update**: Otomatik güncelleme

## 💡 Örnek Kullanımlar

### 1. GitHub Kullanıcı Sorgulama
1. Clipboard'a GitHub username kopyala: `octocat`
2. Regex yakalanır: `^[a-zA-Z0-9-]+$`
3. API çağrısı: `https://api.github.com/users/octocat`
4. JSON formatter ekranında sonuç gösterilir

### 2. SQL Stack Trace Analizi
1. Java stack trace kopyala
2. Regex yakalanır: `at (\\S+)\\((\\S+\\.java):(\\d+)\\)`
3. Class, file, line bilgileri parse edilir
4. Markdown ekranında detaylar gösterilir

### 3. Kurum Kodu Arama
1. Kurum kodu kopyala: `ENPARA`
2. Lookup handler dosyadan arar
3. Bulunan bilgiler context'e eklenir
4. Formatted output ile gösterilir

### 4. Manuel SQL Editor
1. Manual Handlers → "PL SQL Deneme" seç
2. SQL Editor açılır
3. Query yazıp çalıştır
4. Sonuçları incele

## 🎨 Tema Sistemi

### 🌟 Mevcut Temalar
- **Light**: Açık tema
- **Dark**: Koyu tema  
- **Dim**: Orta ton tema

### 🎭 Tema Değiştirme
- Toolbar'dan tema seçici
- Otomatik kaydetme
- Tüm ekranlar senkronize

## ⚙️ Konfigürasyon

### 📋 handlers.json Yapısı
```json
{
  "handlers": [
    {
      "name": "Handler Adı",
      "type": "handler_türü", 
      "screen_id": "ekran_id",
      "actions": [...],
      "user_inputs": [...],
      "conditions": {...}
    }
  ]
}
```

### 🔧 Condition System
```json
{
  "conditions": {
    "operator": "and",
    "conditions": [
      {
        "field": "_selector_key",
        "operator": "equals", 
        "value": "drivercode"
      }
    ]
  }
}
```

## 🚀 Performance İpuçları

### ⚡ Optimization
- Log filtering için search kullanın
- Büyük dosyalar için lazy loading
- Cache kullanarak hızlandırın
- Background thread'lerde çalıştırın

### 🔍 Debugging
- Activity log'ları inceleyin
- LogLevel filter kullanın
- Manual handler'larla test edin
- Step-by-step execution

## 📞 Destek

Bu kılavuz Contextualizer projesinin temel kullanımını kapsar. Daha detaylı bilgi için:
- Proje dökümantasyonunu inceleyin
- Source kodu analiz edin
- Community forum'ları kullanın

---
**🎯 Contextualizer** - Clipboard automation made simple!
