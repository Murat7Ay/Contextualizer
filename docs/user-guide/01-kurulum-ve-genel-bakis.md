# Contextualizer - Kurulum ve Genel Bakış

## 📋 İçindekiler
- [Genel Bakış](#genel-bakış)
- [Sistem Gereksinimleri](#sistem-gereksinimleri)
- [Kurulum](#kurulum)
- [İlk Çalıştırma](#ilk-çalıştırma)
- [Temel Kavramlar](#temel-kavramlar)

---

## Genel Bakış

Contextualizer, Windows platformu için geliştirilmiş, pano (clipboard) içeriğini akıllı bir şekilde işleyen ve otomasyona dayalı bir uygulamadır. Uygulama, kullanıcının kopyaladığı içeriği analiz eder ve önceden tanımlanmış kurallara göre otomatik işlemler gerçekleştirir.

### Temel Özellikler

#### 🎯 Pano İzleme ve Otomasyon
- Global kısayol tuşu (Win+Shift+C) ile pano içeriğini yakalar
- Seçili metni/dosyaları otomatik olarak kopyalar
- İçeriğe göre uygun işleyiciyi (handler) otomatik seçer
- Birden fazla işleyiciyi paralel olarak çalıştırabilir

#### 🔧 9 Farklı İşleyici Tipi

1. **Regex Handler**: Metin desenlerine dayalı işleme
2. **Database Handler**: SQL sorguları ile veritabanı işlemleri  
3. **API Handler**: REST API istekleri ve yanıtları
4. **File Handler**: Dosya bilgileri ve özellikleri
5. **Lookup Handler**: Anahtar-değer tabanlı arama
6. **Custom Handler**: Özel plugin tabanlı işleme
7. **Manual Handler**: Kullanıcı tarafından manuel tetikleme
8. **Synthetic Handler**: Diğer işleyicileri sarmalayan meta-işleyici
9. **Cron Handler**: Zamanlama tabanlı otomatik çalışma

#### 🎨 Modern WPF Arayüzü
- **Carbon Design System** ile tutarlı tasarım
- Light/Dark/Dim tema desteği
- Chrome benzeri sekme yönetimi
- Gerçek zamanlı activity log
- Dashboard ile sistem istatistikleri
- Markdown, JSON, XML görüntüleyiciler

#### ⚡ Gelişmiş İşlevler
- **50+ yerleşik fonksiyon**: Tarih, string, matematik, JSON, hash, web vb.
- **Pipeline sözdizimi**: `$func:{{ input | function1 | function2 }}`
- **Method chaining**: `$func:today.add(days,5).format(yyyy-MM-dd)`
- **Dynamic value resolution**: `$(variableName)`, `$config:`, `$file:`
- **Condition evaluator**: Koşullu aksiyon yürütme
- **User input dialogs**: Çoklu adımlı kullanıcı girişi

---

## Sistem Gereksinimleri

### Minimum Gereksinimler
- **İşletim Sistemi**: Windows 10 (64-bit) veya üzeri
- **Framework**: .NET 9.0 Runtime
- **RAM**: 4 GB
- **Disk Alanı**: 100 MB (uygulama + veriler)

### Önerilen Gereksinimler
- **İşletim Sistemi**: Windows 11 (64-bit)
- **Framework**: .NET 9.0 Runtime
- **RAM**: 8 GB veya üzeri
- **Disk Alanı**: 500 MB (loglar ve konfigürasyon için)

### Opsiyonel Bağımlılıklar
- **Microsoft SQL Server Client**: Database Handler için MSSQL bağlantıları
- **Oracle Client**: Database Handler için Oracle bağlantıları
- **Internet Bağlantısı**: API Handler ve web fonksiyonları için

---

## Kurulum

### Seçenek 1: Portable Kurulum (Önerilen)

Portable sürüm, kurulum gerektirmez ve doğrudan çalıştırılabilir.

#### Adım 1: Dosyaları İndirin
```
Kaynak: \\ortak\cashmanagement\murat ay\contextualizer
Hedef: C:\PortableApps\Contextualizer\
```

#### Adım 2: Klasör Yapısını Oluşturun
Uygulama ilk çalıştırıldığında otomatik olarak aşağıdaki yapıyı oluşturur:

```
C:\PortableApps\Contextualizer\
├── Contextualizer.exe           # Ana uygulama
├── Contextualizer.Core.dll      # İş mantığı kütüphanesi
├── Contextualizer.PluginContracts.dll  # Plugin arayüzleri
├── WpfInteractionApp.dll        # UI kütüphanesi
├── Config\
│   ├── handlers.json            # İşleyici tanımlamaları
│   ├── appsettings.json         # Uygulama ayarları
│   └── secrets.json             # Hassas bilgiler (opsiyonel)
├── Data\
│   ├── Exchange\                # Handler marketplace
│   ├── Installed\               # Yüklü handler'lar
│   └── Logs\                    # Uygulama logları
├── Plugins\                     # Özel plugin'ler
└── Temp\                        # Geçici dosyalar
```

#### Adım 3: Uygulamayı Çalıştırın
```powershell
cd C:\PortableApps\Contextualizer
.\Contextualizer.exe
```

### Seçenek 2: Kaynak Koddan Derleme

#### Gereksinimler
- Visual Studio 2022 veya üzeri
- .NET 9.0 SDK

#### Adım 1: Repository'yi İndirin
```powershell
git clone https://github.com/Murat7Ay/Contextualizer.git
cd Contextualizer
```

#### Adım 2: NuGet Paketlerini Yükleyin
```powershell
dotnet restore Contextualizer.sln
```

#### Adım 3: Projeyi Derleyin
```powershell
# Debug build
dotnet build Contextualizer.sln --configuration Debug

# Release build
dotnet build Contextualizer.sln --configuration Release
```

#### Adım 4: Uygulamayı Çalıştırın
```powershell
cd WpfInteractionApp\bin\Release\net9.0-windows
.\WpfInteractionApp.exe
```

### Seçenek 3: PowerShell Build Script ile Portable Paket Oluşturma

```powershell
# Build ve portable paket oluşturma
.\build-release.ps1

# Çıktı: publish\Contextualizer-Portable\
```

---

## İlk Çalıştırma

### 1. Uygulamayı Başlatın

```powershell
C:\PortableApps\Contextualizer\Contextualizer.exe
```

### 2. Ana Pencere Bileşenleri

Uygulama açıldığında göreceğiniz ana bileşenler:

#### 🏠 Dashboard (Hoş Geldiniz Ekranı)
- **Handler Sayısı**: Sistemde tanımlı toplam işleyici sayısı
- **Cron Jobs**: Zamanlanmış görev sayısı
- **Quick Actions**: Hızlı erişim butonları
  - Handler Management
  - Cron Manager
  - Marketplace

#### 🔧 Toolbar (Üst Menü)
- **Home**: Dashboard'a dön
- **Settings**: Uygulama ayarları
- **Logging Settings**: Log yapılandırması
- **Handler Exchange**: Handler marketplace
- **Cron Manager**: Zamanlı görevler
- **Manual Handlers**: Manuel işleyiciler listesi
- **Theme**: Tema seçici (Light/Dark/Dim)

#### 📊 Activity Log (Alt Panel)
- Gerçek zamanlı işlem logları
- Filtreleme (metin araması, log seviyesi)
- Log seviyeleri:
  - ✅ Success (Yeşil)
  - ℹ️ Info (Mavi)
  - ⚠️ Warning (Sarı)
  - ❌ Error (Kırmızı)
  - 🔴 Critical (Koyu Kırmızı)

### 3. Kısayol Tuşunu Test Edin

#### Varsayılan Kısayol
```
Win + Shift + C
```

#### Test Adımları
1. Herhangi bir metni seçin (örn: bir URL)
2. `Win + Shift + C` tuşlarına basın
3. Metin otomatik kopyalanır
4. Eğer eşleşen bir handler varsa, işlem gerçekleşir
5. Activity Log'da sonucu görürsünüz

### 4. İlk Handler'ı Yükleyin

#### Handler Exchange'den Yükleme
1. Toolbar'da **"Handler Exchange"** butonuna tıklayın
2. Marketplace penceresinde handler'ları inceleyin
3. Bir handler seçin (örn: "Hello World")
4. **Install** butonuna tıklayın
5. Handler otomatik olarak `Data/Installed/` klasörüne yüklenir
6. Uygulama yeniden başlatılmadan aktif olur

#### Handler'ı Test Edin
```
1. Metin: "test" yazın ve seçin
2. Win + Shift + C ile kopyalayın
3. Handler çalışır ve sonucu gösterir
```

---

## Temel Kavramlar

### 1. Handler (İşleyici)

Handler, pano içeriğini işleyen temel birimdir.

#### Handler Yaşam Döngüsü
```
┌─────────────────────────────────────────────────────┐
│ 1. Clipboard Content Captured                      │
│    (Win+Shift+C tuşuna basıldı)                    │
└──────────────────┬──────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────┐
│ 2. CanHandle() - Her handler kontrol edilir        │
│    - Regex pattern match?                           │
│    - File extension match?                          │
│    - Validation geçti mi?                           │
└──────────────────┬──────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────┐
│ 3. CreateContext() - Context oluşturulur           │
│    - Regex groups yakalanır                         │
│    - API response parse edilir                      │
│    - Database query çalıştırılır                    │
│    - File properties okunur                         │
└──────────────────┬──────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────┐
│ 4. Seeder Merge - Dynamic değerler eklenir         │
│    - constant_seeder (sabitler)                     │
│    - seeder (dinamik değerler)                      │
│    - output_format işlenir                          │
└──────────────────┬──────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────┐
│ 5. Conditions Check - Koşullar kontrol edilir      │
│    - requires_confirmation?                         │
│    - action conditions?                             │
└──────────────────┬──────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────┐
│ 6. User Inputs - Kullanıcıdan veri alınır          │
│    - Modal dialoglar                                │
│    - Validation                                     │
│    - Multi-step navigation                          │
└──────────────────┬──────────────────────────────────┘
                   ▼
┌─────────────────────────────────────────────────────┐
│ 7. Actions Execute - Aksiyonlar çalıştırılır       │
│    - show_window                                    │
│    - show_notification                              │
│    - copy_to_clipboard                              │
│    - open_file                                      │
└─────────────────────────────────────────────────────┘
```

### 2. Context (Bağlam)

Context, handler'ın işlediği veri yapısıdır (Dictionary<string, string>).

#### Özel Context Anahtarları
```csharp
_input          // Orijinal clipboard metni
_match          // Regex match sonucu
_self           // Context'in JSON serileştirmesi
_formatted_output // output_format işlendikten sonraki çıktı
_selector_key   // Hangi key'in clipboard text'i match ettiği
_count          // Sonuç sayısı (database, lookup)
_error          // Hata mesajı (varsa)
```

#### Context Örneği
```json
{
  "_input": "ORDER12345",
  "_match": "ORDER12345",
  "order_id": "12345",
  "order_prefix": "ORDER",
  "_formatted_output": "Order ID: 12345"
}
```

### 3. Action (Aksiyon)

Action, context ile ne yapılacağını belirler.

#### Yerleşik Aksiyonlar
| Aksiyon | Açıklama | Parametreler |
|---------|----------|--------------|
| `show_window` | Yeni sekme açar | `screen_id`, `title`, `key` |
| `show_notification` | Toast bildirimi | `message`, `duration` |
| `copy_to_clipboard` | Panoya kopyalar | `key` |
| `open_file` | Dosya/URL açar | `value` veya `key` |
| `simple_print_key` | Context key'i yazdırır | `key` |

### 4. Dynamic Value Resolution (Dinamik Değer Çözümleme)

#### Placeholder Türleri

##### $(key) - Context Placeholder
```json
{
  "output_format": "User: $(username), Age: $(age)"
}
```

##### $config:path - Configuration Değerleri
```json
{
  "connectionString": "$config:database.connection_string"
}
```

##### $func: - Function Calls
```json
{
  "seeder": {
    "timestamp": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "next_week": "$func:today.add(days,7)",
    "uppercase_name": "$func:string.upper($(name))"
  }
}
```

##### $file:path - Dosya İçeriği
```json
{
  "output_format": "$file:C:\\Templates\\report_template.md"
}
```

#### Çözümleme Sırası
```
1. $file: - Dosya içeriği okunur
2. $config: - Configuration değerleri alınır  
3. $func: - Fonksiyonlar çalıştırılır
4. $() - Context placeholders yerleştirilir
```

### 5. Function System (İşlev Sistemi)

#### Pipeline Syntax
```
$func:{{ input | function1 | function2 | ... }}
```

#### Method Chaining
```
$func:today.add(days, 5).format(yyyy-MM-dd)
```

#### Fonksiyon Kategorileri
- **Date/Time**: `today`, `now`, `add`, `subtract`, `format`
- **String**: `upper`, `lower`, `trim`, `replace`, `substring`, `split`
- **Math**: `add`, `subtract`, `multiply`, `divide`, `round`, `min`, `max`
- **Hash**: `hash.md5`, `hash.sha256`
- **URL**: `url.encode`, `url.decode`, `url.domain`
- **Web**: `web.get`, `web.post`, `web.put`, `web.delete`
- **JSON**: `json.get`, `json.length`, `json.first`, `json.last`
- **Array**: `array.get`, `array.length`, `array.join`

### 6. Condition System (Koşul Sistemi)

#### Operatörler
| Operatör | Açıklama | Örnek |
|----------|----------|-------|
| `equals` | Eşitlik | `"status" equals "active"` |
| `not_equals` | Eşit değil | `"type" not_equals "admin"` |
| `greater_than` | Büyüktür | `"age" greater_than "18"` |
| `less_than` | Küçüktür | `"score" less_than "100"` |
| `contains` | İçerir | `"email" contains "@gmail"` |
| `starts_with` | İle başlar | `"name" starts_with "John"` |
| `ends_with` | İle biter | `"file" ends_with ".pdf"` |
| `matches_regex` | Regex match | `"phone" matches_regex "^\\d{10}$"` |
| `is_empty` | Boş mu | `"field" is_empty` |
| `is_not_empty` | Dolu mu | `"field" is_not_empty` |

#### AND/OR Logic
```json
{
  "conditions": {
    "operator": "and",
    "conditions": [
      {"field": "status", "operator": "equals", "value": "active"},
      {"field": "age", "operator": "greater_than", "value": "18"}
    ]
  }
}
```

---

## Sonraki Adımlar

✅ **Kurulum tamamlandı!** Artık:

1. 📖 [Mimari ve Yapı](02-mimari-ve-yapi.md) bölümünü okuyun
2. 🔧 [Handler Geliştirme Rehberi](03-handler-gelistirme-rehberi.md) ile kendi handler'larınızı yazın
3. 💡 [Örnekler ve Use Cases](08-ornekler-ve-use-cases.md) ile gerçek dünya senaryolarını inceleyin

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

