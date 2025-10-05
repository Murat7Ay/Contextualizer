# 🎯 Contextualizer Sunum Rehberi
## Yazılım ve Analist Ekiplerine Tanıtım (60 dakika)

---

## 📊 TOPLANTI AJANDASÍ (60 dakika)

```
[0-5 dk]   🎬 AÇILIŞ - Hook & Problem Statement
[5-20 dk]  💡 LIVE DEMO - "Sihirli Göster"  
[20-30 dk] 🧠 NASIL ÇALIŞIYOR - Temel Konseptler
[30-45 dk] 🛠️ HANDS-ON - "Siz Deneyin"
[45-55 dk] 💼 USE CASE WORKSHOP - "Sizin İşinizde Nerede?"
[55-60 dk] 📚 KAYNAKLAR & NEXT STEPS
```

---

## 🎬 AÇILIŞ (0-5 dk)

### Problem Statement
> "Günde kaç kere copy-paste yapıyorsunuz?"
> "Clipboard'a kopyaladığınız şeyi başka bir yerde aramak için kaç uygulama arasında geçiş yapıyorsunuz?"

**Pain Points:**
- IBAN kopyalayıp → Excel'de ara → SQL'de sorgula → 3 dakika
- Sipariş numarası kopyalayıp → ERP'yi aç → menüde gez → 2 dakika  
- Dosya yolunu kopyalayıp → özelliklere bak → 30 saniye
- **= Günde 50 kere × 1-3 dk = 1-2.5 saat kayıp**

**Solution:**
> "Contextualizer: Clipboard'ı izler, içeriği anlar, otomatik işler, sonucu gösterir"
> "Copy → Kısayol tuşu → 5 saniyede bitti"

---

## 💡 LIVE DEMO (5-20 dk)

### Demo #1: Regex Handler (Basit Başla)
**Senaryo:** "Sipariş numarası kopyaladım"

```
1. Metin editörde "ORDER12345" yaz → Ctrl+C
2. Win+Shift+C bas
3. ✨ PUFF! Sekme açıldı, bilgiler göründü
4. "Bu 5 saniye sürdü. Normalde?"
```

**Göster:**
- Clipboard monitoring çalıştı
- Regex pattern match oldu  
- Context oluştu (order_id, timestamp, vs)
- Markdown rapor üretildi
- Tab açıldı

**Vurgu:** "Hiçbir şey yüklemediniz, aramadınız, tıklamadınız. Sadece kopyaladınız."

---

### Demo #2: File Handler (Pratik Değer)
**Senaryo:** "Dosya yolunu kopyaladım"

```
1. Windows Explorer'da dosya seç → Shift+Sağ Tık → Copy as path
2. Win+Shift+C
3. ✨ Dosya özellikleri göründü (boyut, tarih, extension, vs)
```

**Göster:**
- 25+ özellik otomatik
- "Windows'ta: Sağ tık → Özellikler → 10 tık → scroll"
- "Contextualizer: Kopyala → Kısayol → Bitti"

---

### Demo #3: Database Handler (Advanced)
**Senaryo:** "Müşteri ID'si kopyaladım"

```
1. "CUST_12345" kopyala
2. Win+Shift+C  
3. ✨ SQL query çalıştı, sonuçlar Markdown tablo oldu
```

**Göster:**
- SQL otomatik çalıştı
- Parametre binding güvenli
- Connection pooling (hızlı)
- Markdown table formatting

**Vurgu:** "SQL Management Studio açmadınız, query yazmadınız. Sadece ID'yi kopyaladınız."

---

### Demo #4: API Handler (Entegrasyon)
**Senaryo:** "API endpoint'ten veri çektim"

```
1. Herhangi bir metin kopyala (trigger için)
2. Win+Shift+C
3. ✨ REST API çağrıldı, JSON parse edildi, gösterildi
```

**Göster:**
- HTTP request otomatik
- JSON response flattening
- Hata yönetimi
- Timeout protection

---

## 🧠 NASIL ÇALIŞIYOR (20-30 dk)

### Temel Mimari (Basit Anlat)

```
📋 CLIPBOARD MONITORING
   ↓
🎯 HANDLER MATCHING (Regex, file type, vs)
   ↓  
📦 CONTEXT CREATION (Key-value pairs)
   ↓
🔄 DYNAMIC VALUE RESOLUTION (Seeders, functions, DB, API)
   ↓
⚙️ ACTIONS (show_window, notification, copy, open_file)
   ↓
🖥️ UI (Tab, toast, vs)
```

**Kilit Kavramlar:**

1. **Handler:** "Bu içeriği ben işlerim" kuralı
   - Regex: Text pattern matching
   - File: Dosya yolu/uzantı
   - Database: SQL query
   - API: REST endpoint
   - Lookup: Key-value çevirme
   - Custom: Kendi plugin'in

2. **Context:** Handler'ın ürettiği key-value dictionary
   ```json
   {
     "order_id": "ORDER12345",
     "customer_name": "John Doe",
     "total": "1250.00"
   }
   ```

3. **Actions:** Context ile ne yapılacak?
   - `show_window`: Sekme aç, göster
   - `show_notification`: Toast bildirimi
   - `copy_to_clipboard`: İşlenmiş veriyi geri kopyala
   - `open_file`: Dosya/URL aç

4. **Dynamic Values:** Context'i zenginleştir
   - `$(key)`: Context'ten al
   - `$config:path`: Config dosyasından oku
   - `$func:now()`: Fonksiyon çağır
   - `$file:template.txt`: Dosyadan oku

---

## 🛠️ HANDS-ON (30-45 dk)

### Adım 1: Kurulum
```
1. \\ortak\cashmanagement\murat ay\contextualizer klasörü
2. Contextualizer.exe'yi C:\PortableApps\Contextualizer'a kopyala
3. Çalıştır
4. Win+Shift+C → Çalışıyor mu test et
```

### Adım 2: İlk Handler'ı Yükle
```
1. Uygulama açık → Handler Exchange butonuna bas
2. Arama kutusuna "hello" yaz
3. "Hello World" handler kartını bul
4. "Install" butonuna bas
5. Exchange penceresini kapat
```

### Adım 3: Test Et
```
1. Metin editörde "test" yaz → Ctrl+C
2. Win+Shift+C bas
3. ✨ Sekme açıldı mı? "Hello, test!" mesajı var mı?
4. ✅ Başardın!
```

### Adım 4: Handler'ı Düzenle (İsteğe Bağlı)
```
1. C:\PortableApps\Contextualizer\Config\handlers.json aç
2. "Hello World" handler'ını bul
3. output_format'ı değiştir: "# Merhaba, $(clipboard_text)!"
4. Kaydet
5. Uygulamayı kapat-aç (reload için)
6. Tekrar test et
```

**Göster:** "JSON düzenlemesi bu kadar basit. Regex, SQL, API - hepsi aynı mantık."

---

## 💼 USE CASE WORKSHOP (45-55 dk)

### Whiteboard Session: "Sizin İşinizde Nerede Kullanılır?"

**Soru:** "Günlük işlerinizde hangi repetitive tasklar var?"

**Örnekler Topla:**
- IBAN/Müşteri ID kopyalayıp başka yerde arama
- Sipariş numarası ile durum sorgusu
- Dosya yolu kopyalayıp özelliklere bakma
- Log dosyasından error kopyalayıp Google'da arama
- Excel'den ID kopyalayıp SQL'de sorgulama

**Mapping Yap:**
```
USE CASE                          → HANDLER TİPİ
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
IBAN kopyala → müşteri bilgisi   → Database Handler (SQL query)
ORDER### → sipariş detayı        → API Handler (ERP endpoint)
Dosya yolu → özellikler           → File Handler (FileInfo)
"ERROR" içeren log → Google       → Regex Handler + open_file action
Excel ID → SQL sorgu              → Database Handler + Regex
JSON config → pretty print        → Custom Handler (JSON formatter)
```

**Sonuç:** "Bu handler'ları sizin için yazabiliriz veya siz yazabilirsiniz."

---

## 📚 KAYNAKLAR & NEXT STEPS (55-60 dk)

### Dokümantasyon
```
📖 Komple Dokümantasyon:
   C:\PortableApps\Contextualizer\docs\index.html
   (veya network share path)

🎓 İçindekiler:
   - Kurulum & Hızlı Başlangıç
   - Tüm Handler Tipleri (9 adet)
   - Execution Pipeline (nasıl çalışır)
   - Plugin Geliştirme (custom handler nasıl yazılır)
   - Loglama Sistemi
   - Performans & Troubleshooting
   - 60+ kod örneği
```

### Handler Exchange
```
🏪 Hazır Handler'lar:
   - Uygulama içinde "Handler Exchange" butonu
   - Kategoriler: Utility, Database, API, File, vs
   - Arama, filtreleme, yükleme
   - Topluluk katkıları (siz de ekleyebilirsiniz)
```

### GitHub Repo
```
💻 Kaynak Kod:
   https://github.com/Murat7Ay/Contextualizer
   
   - Tüm kod açık
   - Issue tracker
   - Pull request'ler kabul edilir
   - Plugin örnekleri: Contextualizer.Plugins/
```

### Destek Kanalları
```
💬 Yardım İçin:
   - Teams/Slack: #contextualizer kanalı (oluşturulacak)
   - Email: [senin emailin]
   - 1-on-1 session: "İlk handler'ımı yazalım" talep edin
```

---

## 🎯 NEXT STEPS (Toplantı Sonrası)

### Hemen (Bugün)
```
☐ Uygulamayı yükleyin (5 dk)
☐ Exchange'den 2-3 handler indirin
☐ Test edin
☐ Dokümantasyonu bookmark'layın
```

### Bu Hafta
```
☐ Kendi use case'inizi düşünün
☐ Basit bir handler JSON'u yazın (veya bizden isteyin)
☐ Test edin, feedback verin
☐ Teams/Slack'te paylaşın
```

### Gelecek Hafta
```
☐ 1-on-1 session (isteğe bağlı)
   → Custom handler yazalım
   → Plugin development başlayalım
☐ Success story paylaşın
   → "Şu işim 10 dakikadan 10 saniyeye düştü"
```

---

## 🎤 SUNUM İPUÇLARI

### Analistler İçin
```
✅ YAP:
   - Kullanım odaklı anlat (JSON minimal)
   - Exchange'den yükleme göster
   - "Hazır var, kullanın" mesajı ver
   - Pratik, günlük örnekler ver

❌ YAPMA:
   - Teknik detay verme (regex syntax, C# kod)
   - "JSON öğrenmeniz gerek" deme
   - Karmaşık örneklerle başlama
```

### Yazılımcılar İçin
```
✅ YAP:
   - Mimari detay ver (handler lifecycle, plugin system)
   - IAction, IContextValidator, IContextProvider göster
   - GitHub linkini vur
   - "Extend edebilirsiniz" mesajı ver
   - Performance metrics göster (regex timeout, connection pooling)

❌ YAPMA:
   - "Sadece config düzenleyin" deme (sınırlayıcı)
   - Plugin yazmanın zor olduğunu ima etme
```

---

## 📋 CHEAT SHEET (Ekiple Paylaş)

### Temel Kısayollar
```
Win+Shift+C        → Clipboard'ı işle (main shortcut)
Handler Exchange   → Hazır handler'ları yükle
Settings           → Kısayol tuşunu değiştir, paths ayarla
Activity Log       → Handler execution history
```

### İlk Handler Nasıl Yazılır (5 Adım)
```
1. C:\PortableApps\Contextualizer\Config\handlers.json aç
2. Mevcut handler'ı kopyala
3. name, regex, output_format değiştir
4. Kaydet
5. Uygulamayı restart et
```

### Handler Tipleri Hızlı Referans
```
regex       → Text pattern matching (ORDER\d+)
file        → Dosya yolu/uzantı kontrolü
database    → SQL query çalıştır
api         → REST endpoint çağır
lookup      → Key-value çevirme (IBAN → Banka ismi)
custom      → Kendi C# plugin'in
cron        → Zamanlanmış görevler
synthetic   → Diğer handler'ları wrap et
manual      → UI'dan tetikle (clipboard bağımsız)
```

### Örnek Handler JSON (Kopya-Yapıştır)
```json
{
  "name": "IBAN Checker",
  "type": "regex",
  "regex": "TR\\d{24}",
  "screen_id": "markdown2",
  "output_format": "# IBAN Bilgisi\n\n- IBAN: $(clipboard_text)\n- Tarih: $func:now()\n\n✅ Geçerli format",
  "actions": [
    { "name": "show_window", "key": "_formatted_output" }
  ]
}
```

---

## 🎁 BONUS: "Quick Win" Örnekleri

### Örnek 1: URL Kısaltıcı
```json
{
  "name": "URL Shortener",
  "type": "regex",
  "regex": "https?://.*",
  "api_url": "https://api.short.io/links",
  "api_method": "POST",
  "api_body": "{\"originalURL\": \"$(clipboard_text)\"}",
  "actions": [
    { "name": "copy_to_clipboard", "key": "shortURL" },
    { "name": "show_notification", "message": "Kısa link kopyalandı!" }
  ]
}
```

### Örnek 2: JSON Validator
```json
{
  "name": "JSON Validator",
  "type": "regex",
  "regex": "^\\{.*\\}$",
  "screen_id": "json_formatter",
  "actions": [
    { "name": "show_window", "key": "clipboard_text", "title": "JSON Viewer" }
  ]
}
```

### Örnek 3: Dosya Hasher
```json
{
  "name": "File Hash",
  "type": "file",
  "extensions": ["exe", "dll", "zip"],
  "output_format": "# Dosya Hash\n\n- Dosya: $(file_name)\n- MD5: $func:md5($(file_full_path))\n- SHA256: $func:sha256($(file_full_path))",
  "actions": [
    { "name": "show_window", "key": "_formatted_output" }
  ]
}
```

---

## 🚀 SON SÖZ

**Mesaj:**
> "Contextualizer, günlük repetitive taskları otomatikleştirir."
> "Clipboard'ı akıllı hale getirir."
> "5 saniyede yaparsınız, 5 dakika yerine."
> "Deneyiniz. Feedback veriniz. Handler yazalım."

**Call to Action:**
> "Bugün yükleyin, bu hafta 1 handler test edin, gelecek hafta kendi handler'ınızı yazın."
> "Sorularınız için #contextualizer kanalında buluşalım!"

---

## 📞 İLETİŞİM

```
📧 Email: [senin emailin]
💬 Teams/Slack: #contextualizer
📁 Docs: C:\PortableApps\Contextualizer\docs\index.html
💻 GitHub: https://github.com/Murat7Ay/Contextualizer
```

---

**Not:** Bu rehber 60 dakikalık webex toplantısı için hazırlandı. İhtiyaca göre bölümleri kısaltıp uzatabilirsin. En önemli kısım **LIVE DEMO** - oraya ağırlık ver!

**Başarılar! 🎉**
