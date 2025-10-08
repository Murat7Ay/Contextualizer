# 🎯 Contextualizer - Analist Ekibi Sunum Rehberi

## 📌 Bu Yazılım Ne İşe Yarar?

**Contextualizer**, panoyu (clipboard) izleyen ve kopyaladığınız içeriğe göre otomatik işlemler yapan bir akıllı asistan.

### 💡 Gerçek Hayat Örneği:

**Eskisi:**
1. Excel'de müşteri IBAN'ını gördünüz
2. IBAN'ı kopyaladınız (Ctrl+C)
3. SQL Management Studio'yu açtınız
4. Query yazdınız: `SELECT * FROM Customers WHERE IBAN = 'TR...'`
5. Sonucu beklediniz
6. **Toplam süre:** 2-3 dakika

**Contextualizer ile:**
1. Excel'de IBAN'ı kopyaladınız (Ctrl+C)
2. `Win+Shift+C` tuşuna bastınız
3. ✨ **PUFF!** Otomatik SQL sorgusu çalıştı, sonuçlar ekrana geldi
4. **Toplam süre:** 5 saniye

---

## 🏗️ Mimari (Basit Anlatım)

### 1. **CORE (Motor Bölümü)** 🔧
Bu yazılımın kalbi. 3 ana bileşeni var:

#### A) İŞLEYİCİLER (Handlers) - "Bu içeriği ben işlerim!"

**9 farklı işleyici tipi var:**

| İşleyici | Ne İşe Yarar? | Örnek Kullanım |
|----------|---------------|----------------|
| **Regex** | Metin desenini yakalar | ORDER12345 → Sipariş detayını göster |
| **Database** | SQL sorgusu çalıştırır | IBAN kopyala → Müşteri bilgisini getir |
| **API** | Internetten veri çeker | URL kopyala → API'den bilgi al |
| **File** | Dosya bilgilerini gösterir | Dosya yolu kopyala → 25+ özellik göster |
| **Lookup** | Tabloda arama yapar | Ülke kodu → Ülke ismini göster |
| **Custom** | Özel kodunuz çalışır | JSON kopyala → Geçerlilik kontrolü |
| **Manual** | Butona tıklayarak çalışır | "Rapor Al" butonu → Günlük rapor |
| **Cron** | Zamanlanmış görev | Her sabah 9'da → Satış raporu |
| **Synthetic** | Kullanıcıdan girdi alır | "Ne aramak istiyorsunuz?" → Sonuç göster |

##### 🔍 Regex Handler Nasıl Çalışır?

```
1. Siz: "ORDER12345" kopyaladınız
2. Regex Handler: "Bu benim işim! ORDER ile başlıyor"
3. Sipariş numarasını yakaladı: 12345
4. Context oluşturdu: { "order_id": "12345", "order_prefix": "ORDER" }
5. Action çalıştı: SQL sorgusu veya API çağrısı
6. Sonuç ekrana geldi
```

**Kodda Neler Oluyor?** *(Öğrenmek isteyenler için)*
- Constructor'da regex derleniyor (hızlı olması için)
- `CanHandle()`: "Bu içeriği işleyebilir miyim?"
- `CreateContext()`: "İçeriği parçala, bilgileri çıkar"
- Timeout koruması var (5 saniye) - Sonsuz döngüye girmesin diye

##### 💾 Database Handler Nasıl Çalışır?

```
1. Siz: "TR123456789..." IBAN kopyaladınız
2. Database Handler: "IBAN formatı! SQL sorgusu çalıştırayım"
3. Güvenlik kontrolü: Sadece SELECT sorgularına izin var
4. Parametreli sorgu: @p_input = "TR123..."
5. SQL çalıştı: SELECT * FROM Customers WHERE IBAN = @p_input
6. Sonuçlar Markdown tablosu olarak gösterildi
```

**Güvenlik:** 
- ❌ INSERT, UPDATE, DELETE YASAK
- ❌ DROP TABLE YASAK  
- ✅ Sadece SELECT çalışır
- ✅ Parametreli sorgular (SQL Injection koruması)

##### 🌐 API Handler Nasıl Çalışır?

```
1. Siz: URL veya anahtar kelime kopyaladınız
2. API Handler: "API çağrısı yapayım"
3. HTTP isteği gönderildi (GET/POST)
4. JSON cevap geldi
5. JSON düzleştirildi: { "data.user.name": "Ali", "data.user.age": "30" }
6. Ekrana gösterildi
```

**Performans:**
- Connection pooling var (her seferinde yeni bağlantı açılmaz)
- 30 saniye timeout
- Keep-Alive aktif (bağlantı açık kalır)

#### B) AKSİYONLAR (Actions) - "Sonuçla ne yapayım?"

Her işleyici context'i (bilgileri) oluşturduktan sonra, action'lar devreye girer:

| Action | Ne Yapar? |
|--------|-----------|
| **show_window** | Yeni sekme açar, sonuçları gösterir |
| **show_notification** | Toast bildirimi gösterir |
| **copy_to_clipboard** | İşlenmiş veriyi tekrar panoya kopyalar |
| **open_file** | Dosya veya URL'i açar |

**Akış:**
```
Handler → Context → Action → Ekran/Dosya/Pano
```

#### C) FONKSİYONLAR (Functions) - "Veriyi dönüştür!"

40+ hazır fonksiyon var:

```
$func:today.add(days, 7).format(yyyy-MM-dd)
// Bugünden 7 gün sonrasını döndürür: "2025-10-15"

$func:string.upper($(customer_name))
// Müşteri ismini büyük harfe çevirir

$func:hash.md5($(password))
// Şifreyi MD5'e çevirir
```

**Fonksiyon Kategorileri:**
- 📅 Tarih/Saat: today, now, add, subtract, format
- 📝 Metin: upper, lower, trim, replace, substring
- 🔢 Matematik: add, multiply, round
- 🌐 Web: get, post, url.encode
- 🔐 Hash: md5, sha256
- 📊 JSON/Array: get, length, first, last

---

### 2. **PLUGINCONTRACTS (Arayüz Katmanı)** 🔌

Bu kısım, yazılımın genişletilebilir olmasını sağlar. Interface'ler (sözleşmeler) tanımlanmış.

#### Ana Interface'ler:

##### **IHandler** - İşleyici Sözleşmesi
```csharp
interface IHandler {
    CanHandle(clipboard) → bool        // "Bu içeriği işleyebilir miyim?"
    Execute(clipboard) → bool          // "İşle!"
    HandlerConfig → ayarlar            // JSON'dan gelen ayarlar
}
```

##### **IAction** - Aksiyon Sözleşmesi
```csharp
interface IAction {
    Name → string                      // "show_window", "copy_to_clipboard"
    Action(context) → void             // Context'le ne yapacak?
}
```

##### **IUserInteractionService** - Kullanıcı Etkileşimi
```csharp
interface IUserInteractionService {
    ShowNotification(message)          // Toast göster
    ShowWindow(screenId, title, data)  // Sekme aç
    GetUserInput(prompt)               // Kullanıcıdan veri al
    ShowConfirmationAsync(title, msg)  // Onay iste
}
```

#### HandlerConfig (JSON Yapısı)

Her handler bir JSON dosyasından ayarlarını alır:

```json
{
  "name": "IBAN Checker",
  "type": "regex",
  "regex": "TR\\d{24}",
  "actions": [
    { "name": "show_window" }
  ],
  "output_format": "IBAN: $(clipboard_text)"
}
```

**Önemli Özellikler:**
- `name`: Handler adı (UI'da gösterilir)
- `type`: Handler tipi (regex, database, api, vb.)
- `regex`: Metin deseni (eğer regex handler ise)
- `connectionString`: Veritabanı bağlantısı (eğer database handler ise)
- `url`: API adresi (eğer api handler ise)
- `actions`: Sonuç ne olacak?
- `output_format`: Ekrana nasıl yazılacak?

---

### 3. **WPFINTERACTIONAPP (Arayüz)** 🎨

Modern WPF arayüzü. Kullanıcının gördüğü kısım.

#### Ana Bileşenler:

##### **MainWindow** - Ana Pencere
- Sekme yönetimi (Chrome gibi)
- Activity log (ne oldu, ne zaman?)
- Dashboard (istatistikler)

```
┌─────────────────────────────────────────┐
│ [⚙️ Settings] [📊 Cron] [🔄 Exchange]  │
├─────────────────────────────────────────┤
│ ┌─Tab1──┬─Tab2──┬─Tab3──┐              │
│ │                          │              │
│ │   Markdown görünümü      │              │
│ │   veya JSON formatter    │              │
│ │                          │              │
│ └──────────────────────────┘              │
├─────────────────────────────────────────┤
│ Activity Log:                            │
│ ✅ Handler 'IBAN Checker' executed      │
│ ⚠️ No handlers matched clipboard         │
└─────────────────────────────────────────┘
```

##### **Ekranlar (Screens)**
- **MarkdownViewer2**: Markdown'ı HTML'e çevirir, gösterir
- **JsonFormatterView**: JSON'u renkli, düzenli gösterir
- **XmlFormatterView**: XML'i düzenli gösterir
- **PlSqlEditor**: SQL sorgusu yazma editörü
- **UrlViewer**: Web sayfası gösterici

##### **Carbon Design System**
- Modern, tutarlı tasarım
- Light/Dark tema desteği
- Renk şeması: Mavi tonlar (#0366d6)

---

## 🎬 ÇALIŞMA AKIŞI (Adım Adım)

### Senaryo: Müşteri IBAN'ı Kopyalandı

```
1️⃣ KULLANICI:
   - Excel'de "TR123456789012345678901234" kopyaladı
   - Win+Shift+C tuşuna bastı

2️⃣ CLIPBOARD MONITORING (Pano İzleme):
   - KeyboardHook tuşa basıldığını yakaladı
   - WindowsClipboardService pano içeriğini okudu
   - ClipboardContent oluşturuldu:
     { IsText: true, Text: "TR123..." }

3️⃣ HANDLER MATCHING (Eşleştirme):
   - HandlerManager tüm handler'ları kontrol etti
   - Database Handler: "Regex pattern uyuyor! Ben işlerim"
   - CanHandle() → true

4️⃣ CONTEXT CREATION (İçerik Oluşturma):
   - Regex grupları yakalandı
   - SQL parametreleri hazırlandı:
     { p_input: "TR123...", p_match: "TR123..." }

5️⃣ SQL EXECUTION (Sorgu Çalıştırma):
   - Query: SELECT * FROM Customers WHERE IBAN = @p_input
   - Dapper ile async çalıştırıldı
   - Result: { CustomerID#1: "12345", Name#1: "Ali Veli" }

6️⃣ OUTPUT FORMATTING (Çıktı Biçimlendirme):
   - Markdown table oluşturuldu:
     | Row | CustomerID | Name |
     |-----|------------|------|
     | 1   | 12345      | Ali  |

7️⃣ ACTION EXECUTION (Aksiyon):
   - show_window action çağrıldı
   - MarkdownViewer2 ekrana geldi
   - Yeni sekme açıldı

8️⃣ UI UPDATE (Arayüz Güncellendi):
   - TabControl'e yeni tab eklendi
   - Activity Log'a "✅ Handler executed" yazıldı
   - Dashboard istatistikleri güncellendi

⏱️ TOPLAM SÜRE: 2-3 saniye
```

---

## 🔧 ANALİST EKİBİ İÇİN KULLANIM

### Hazır Handler Nasıl Yüklenir?

```
1. Uygulamayı aç
2. "Handler Exchange" butonuna bas
3. Arama kutusuna "IBAN" yaz
4. "IBAN Checker" kartını bul
5. "Install" butonuna bas
6. Kapat
7. Test et: Bir IBAN kopyala → Win+Shift+C
```

### Kendi Handler'ınızı Nasıl Yazarsınız? (Basit JSON)

**Adım 1:** `C:\PortableApps\Contextualizer\Config\handlers.json` dosyasını aç

**Adım 2:** Mevcut bir handler'ı kopyala:

```json
{
  "name": "Sipariş Numarası Sorgulama",
  "type": "regex",
  "regex": "ORDER\\d+",
  "screen_id": "markdown2",
  "actions": [
    { "name": "show_window", "key": "_formatted_output" }
  ],
  "output_format": "# Sipariş Detayı\n\n- Sipariş No: $(clipboard_text)\n- Tarih: $func:now()\n\n✅ Sipariş bulundu!"
}
```

**Adım 3:** Kaydet

**Adım 4:** Uygulamayı yeniden başlat

**Adım 5:** Test et: "ORDER12345" yaz → Kopyala → Win+Shift+C

### Database Handler Örneği (Gerçek Kullanım)

```json
{
  "name": "Müşteri IBAN Sorgulama",
  "type": "database",
  "regex": "TR\\d{24}",
  "connectionString": "$config:database.customer_db",
  "connector": "mssql",
  "query": "SELECT CustomerID, Name, Phone FROM Customers WHERE IBAN = @p_input",
  "actions": [
    { "name": "show_window", "key": "_formatted_output" }
  ]
}
```

**Açıklama:**
- **regex**: TR ile başlayan 24 haneli sayı (IBAN)
- **connectionString**: `$config:...` → Config dosyasından okunur (güvenlik)
- **connector**: `mssql` → SQL Server
- **query**: Parametreli SQL sorgusu (`@p_input` otomatik doldurulur)
- **actions**: Sonucu markdown tablosu olarak göster

### API Handler Örneği

```json
{
  "name": "GitHub User Info",
  "type": "api",
  "regex": "^[a-zA-Z0-9-]+$",
  "url": "https://api.github.com/users/$(clipboard_text)",
  "method": "GET",
  "headers": {
    "User-Agent": "Contextualizer"
  },
  "actions": [
    { "name": "show_window", "key": "_formatted_output" }
  ],
  "output_format": "# GitHub User\n\n- Name: $(name)\n- Public Repos: $(public_repos)\n- Followers: $(followers)"
}
```

**Açıklama:**
- **url**: `$(clipboard_text)` → Kopyalanan metin URL'e eklenir
- **method**: GET isteği
- **output_format**: API'den gelen JSON değerleri gösterilir

---

## 📊 PERFORMANS VE GÜVENLİK

### Performans Optimizasyonları

1. **Regex Compilation**
   - Regex pattern'ler constructor'da derlenir
   - 10-20x daha hızlı matching
   - 5 saniye timeout (ReDoS koruması)

2. **Connection Pooling**
   - Database: Her handler için ayrı pool yok, merkezi yönetim
   - API: SocketsHttpHandler ile connection reuse
   - Max 10 bağlantı/server

3. **Async/Await**
   - Tüm I/O işlemleri async
   - UI thread bloklanmaz
   - Responsive arayüz

4. **Dictionary Capacity**
   - File Handler: 25 property × file count kapasite
   - Memory reallocation önlenir

### Güvenlik Önlemleri

1. **SQL Injection Koruması**
   - ✅ Parametreli sorgular (Dapper)
   - ❌ SELECT-only enforcement
   - ❌ Forbidden keywords: INSERT, UPDATE, DELETE, DROP, EXEC, xp_, sp_

2. **ReDoS (Regex Denial of Service) Koruması**
   - 5 saniye timeout tüm regex operasyonlarında
   - RegexMatchTimeoutException yakalanır

3. **Parameter Limits**
   - Max 20 regex group (SQL parameter overflow önleme)
   - Max 4000 char per parameter (SQL varchar limit)

4. **Config Security**
   - Hassas bilgiler (connection strings, API keys) `$config:` ile ayrı dosyada
   - `secrets.ini` dosyası .gitignore'da

---

## 🎯 GERÇEK DÜNYA KULLANIM ÖRNEKLERİ

### Örnek 1: Finans Ekibi - IBAN Kontrolü

**Problem:** Günde 50 kere IBAN kopyalayıp SQL'de müşteri bakıyorlar.

**Çözüm:**
```json
{
  "name": "IBAN → Müşteri Bilgisi",
  "type": "database",
  "regex": "TR\\d{24}",
  "connectionString": "$config:db.customer",
  "connector": "mssql",
  "query": "SELECT TOP 1 * FROM vw_CustomerDetails WHERE IBAN = @p_input"
}
```

**Sonuç:** 3 dakika → 5 saniye ✅

---

### Örnek 2: Destek Ekibi - Sipariş Takibi

**Problem:** Müşteriden gelen sipariş numarasını ERP'de arıyorlar.

**Çözüm:**
```json
{
  "name": "Sipariş Detayı",
  "type": "api",
  "regex": "ORDER\\d+",
  "url": "http://erp.internal/api/orders/$(clipboard_text)",
  "method": "GET",
  "output_format": "# Sipariş $(order_id)\n\n- Durum: $(status)\n- Müşteri: $(customer_name)\n- Tutar: $(total_amount) TL"
}
```

**Sonuç:** 2 dakika → 3 saniye ✅

---

### Örnek 3: IT Ekibi - Log Analizi

**Problem:** Log dosyasında error kopyalayıp Google'da arıyorlar.

**Çözüm:**
```json
{
  "name": "Error → Google Ara",
  "type": "regex",
  "regex": "ERROR.*",
  "actions": [
    {
      "name": "open_file",
      "value": "https://www.google.com/search?q=$(clipboard_text)"
    }
  ]
}
```

**Sonuç:** 30 saniye → 2 saniye ✅

---

### Örnek 4: Analist Ekibi - Dosya Özellikleri

**Problem:** Dosya yolunu kopyalayıp Properties'e sağ tıklıyorlar.

**Çözüm:** (Zaten hazır!)
```json
{
  "name": "File Info",
  "type": "file",
  "file_extensions": ["pdf", "xlsx", "docx", "txt"],
  "output_format": "# Dosya Özellikleri\n\n- İsim: $(FileName0)\n- Boyut: $(SizeBytes0) bytes\n- Oluşturma: $(CreationDate0)"
}
```

**Sonuç:** 20 saniye → 2 saniye ✅

---

## 🚀 SUNUMDA ANLATIM ÖNERİLERİ

### Analistler İçin (Teknik Olmayan)

**YAPILACAKLAR:**
- ✅ Live demo göster (IBAN → SQL otomatik)
- ✅ "5 saniye vs 3 dakika" vurgula
- ✅ Handler Exchange'den hazır yükleme göster
- ✅ "JSON bilmene gerek yok" de

**YAPILMAYACAKLAR:**
- ❌ Kod gösterme
- ❌ Interface, class, method gibi terimler kullanma
- ❌ "Regex öğrenmeniz gerek" deme

### Yazılımcılar İçin (Teknik)

**YAPILACAKLAR:**
- ✅ Mimari diagram göster (Handler → Context → Action)
- ✅ IHandler, IAction interface'lerini açıkla
- ✅ Plugin geliştirme göster (IContextValidator, IContextProvider)
- ✅ Performance optimizations anlat (regex compilation, connection pooling)
- ✅ GitHub linkini paylaş

**YAPILMAYACAKLAR:**
- ❌ "Sadece JSON düzenleyin" deme (limiting)
- ❌ Plugin yazmanın zor olduğunu ima etme

---

## 📝 SON SÖZLER

**Core:** 9 handler + action sistemi + 40+ fonksiyon = Motor bölümü  
**PluginContracts:** Interface'ler = Genişletilebilirlik  
**WpfInteractionApp:** Modern WPF + Carbon Design = Kullanıcı arayüzü

**Toplam:** 3 proje, 50+ dosya, 10.000+ satır kod

**Amaç:** Günlük tekrarlayan işleri otomatikleştirmek

**Sonuç:** ⏱️ Zamandan tasarruf + 😊 Daha az stres

---

## 🎁 BONUS: Hızlı Başlangıç Komutları

```bash
# Kurulum
1. Dosyaları C:\PortableApps\Contextualizer'a kopyala
2. Contextualizer.exe'yi çalıştır
3. Win+Shift+C tuşunu dene

# İlk Handler Yükleme
1. "Handler Exchange" butonu → "Hello World" ara → Install

# Test
1. "test" yaz → Ctrl+C
2. Win+Shift+C
3. "Hello, test!" mesajını gör

# Kendi Handler'ınızı Yazın
1. C:\PortableApps\Contextualizer\Config\handlers.json
2. Mevcut handler'ı kopyala → Düzenle
3. Uygulamayı yeniden başlat
```

---

**Sorularınız için:** docs/index.html (komple teknik dokümantasyon)

**Başarılar! 🎉**

