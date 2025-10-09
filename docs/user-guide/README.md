# Contextualizer - Kapsamlı Kullanım Kılavuzu

## 🚀 Hoş Geldiniz

Bu kılavuz, Contextualizer uygulamasının tüm özelliklerini, mimarisini ve kullanımını detaylı olarak açıklar. Her seviyeden kullanıcı için hazırlanmıştır.

---

## 📚 Dokümantasyon İçeriği

### 1. [Kurulum ve Genel Bakış](01-kurulum-ve-genel-bakis.md)
**İçerik**:
- Contextualizer nedir?
- Temel özellikler ve faydalar
- Sistem gereksinimleri
- Kurulum adımları (Portable ve Installer)
- İlk kullanım ve configuration
- Keyboard shortcut tanımı

**Hedef Kitle**: Tüm kullanıcılar  
**Tahmini Okuma Süresi**: 15 dakika

---

### 2. [Mimari ve Yapı](02-mimari-ve-yapi.md)
**İçerik**:
- Genel mimari (3-tier: Presentation, Business Logic, Contracts)
- Proje yapısı ve dosya organizasyonu
- Temel bileşenler (HandlerManager, Dispatch, FunctionProcessor, etc.)
- Veri akışı (Clipboard Capture Flow, Handler Execution Flow)
- Servis mimarisi (Service Locator Pattern)

**Hedef Kitle**: Geliştiriciler, ileri seviye kullanıcılar  
**Tahmini Okuma Süresi**: 30 dakika

---

### 3. [Handler Geliştirme Rehberi](03-handler-gelistirme-rehberi.md)
**İçerik**:
- Handler nedir ve nasıl çalışır?
- `IHandler` interface ve `Dispatch` base class
- 9 handler tipi detaylı anlatım:
  - **RegexHandler**: Pattern matching ve named groups
  - **DatabaseHandler**: SQL query execution (MSSQL, Oracle)
  - **ApiHandler**: REST API integration
  - **FileHandler**: File metadata extraction
  - **LookupHandler**: CSV/TSV lookup
  - **CustomHandler**: Plugin-based custom logic
  - **ManualHandler**: UI-triggered handlers
  - **SyntheticHandler**: Meta-handler
  - **CronHandler**: Scheduled tasks
- Her handler için JSON örnekleri
- Best practices ve performance optimizasyonları

**Hedef Kitle**: Handler geliştiricileri, sistem analistleri  
**Tahmini Okuma Süresi**: 1-2 saat

---

### 4. [Function System](04-function-system.md)
**İçerik**:
- Function System nedir?
- 3 syntax türü (Regular, Pipeline, Method Chaining)
- 50+ built-in function detaylı dokümantasyonu:
  - **Date/Time**: today, now, add, subtract, format
  - **String**: upper, lower, trim, replace, substring, split, etc.
  - **Math**: add, subtract, multiply, divide, round, etc.
  - **Hash**: md5, sha256
  - **URL**: encode, decode, domain, path, query, combine
  - **Web**: get, post, put, delete
  - **IP**: local, public, isprivate, ispublic
  - **JSON**: get, length, first, last, create
  - **Array**: get, length, join
  - **Utility**: guid, random, base64encode, base64decode, env, username, computername
- İleri seviye kullanım (nested functions, pipelines, chaining)
- Best practices

**Hedef Kitle**: Tüm kullanıcılar  
**Tahmini Okuma Süresi**: 45 dakika

---

### 5. [Action System](05-action-system.md)
**İçerik**:
- Action nedir ve lifecycle'ı
- `ConfigAction` yapısı (JSON schema)
- 3 built-in action detaylı anlatım:
  - **copytoclipboard**: Panoya kopyalama
  - **show_notification**: Toast bildirimi
  - **show_window**: Tab açma
- Action özellikleri:
  - **Seeder** (constant_seeder, seeder)
  - **User Inputs** (multi-step, validation)
  - **Conditions** (and/or, operators)
  - **Requires Confirmation**
- **Inner Actions**: Nested action execution
- Custom action geliştirme (`IAction` interface)
- Kompleks örnekler

**Hedef Kitle**: Handler geliştiricileri, sistem analistleri  
**Tahmini Okuma Süresi**: 1 saat

---

### 6. [Plugin Geliştirme](06-plugin-gelistirme.md)
**İçerik**:
- Plugin sistemi mimarisi
- 3 plugin türü:
  - **IAction**: Custom actions
  - **IContextValidator**: Custom validation logic
  - **IContextProvider**: Custom context creation
- Plugin geliştirme adım adım:
  - Proje oluşturma
  - Interface implementation
  - Service provider kullanımı
  - Error handling
  - Testing
  - Deployment
- Tam kod örnekleri (SaveToFile, EmailValidator, CsvContextProvider, etc.)
- Best practices ve code review checklist

**Hedef Kitle**: Plugin geliştiricileri  
**Tahmini Okuma Süresi**: 1-1.5 saat

---

### 7. [UI Özellikleri](07-ui-ozellikleri.md)
**İçerik**:
- Ana pencere (MainWindow) yapısı
- Tab sistemi (Chrome-like, middle-click close)
- Welcome Dashboard
- Theme sistemi (Dark, Light, Dim)
- Dynamic Screens (markdown2, json_formatter, xml_formatter, plsql_editor, url_viewer)
- Toast Notifications (single & multiple actions)
- Activity Log (filtering, search)
- User Input Dialogs (multi-step, validation)
- Settings (keyboard shortcut, paths, preferences)

**Hedef Kitle**: Tüm kullanıcılar  
**Tahmini Okuma Süresi**: 30 dakika

---

### 8. [Örnekler ve Use Cases](08-ornekler-ve-use-cases.md)
**İçerik**:
- Regex Handler örnekleri (email, order tracking, phone formatting)
- Database Handler örnekleri (user lookup, inventory, sales report)
- API Handler örnekleri (GitHub, Weather, REST POST)
- Custom Handler örnekleri (JSON formatter, XML formatter)
- Cron Handler örnekleri (daily report, health check)
- Manual Handler örnekleri (template generator, code snippet)
- Kompleks senaryolar (multi-stage processing, data enrichment)

**Hedef Kitle**: Tüm kullanıcılar  
**Tahmini Okuma Süresi**: 45 dakika

---

### 9. [Troubleshooting ve FAQ](09-troubleshooting-ve-faq.md)
**İçerik**:
- Genel sorunlar (başlamıyor, keyboard shortcut, pano yakalama)
- Handler sorunları (yüklenmiyor, regex eşleşmiyor, database hataları)
- Function sorunları (çalışmıyor, pipeline hatası, timeout)
- Database sorunları (connection string, Oracle, query sonuçları)
- UI sorunları (tab açılmıyor, theme, notification)
- Plugin sorunları (yüklenmiyor, hata veriyor)
- Performance sorunları (yavaş, memory, too many handlers)
- FAQ (50+ soru-cevap)

**Hedef Kitle**: Tüm kullanıcılar  
**Tahmini Okuma Süresi**: 20-30 dakika (ihtiyaç durumunda)

---

## 🎯 Kullanım Senaryolarına Göre Rehber

### Yeni Başlayanlar İçin
1. [Kurulum ve Genel Bakış](01-kurulum-ve-genel-bakis.md) → Başlangıç
2. [Handler Geliştirme Rehberi](03-handler-gelistirme-rehberi.md) → İlk handler'ınızı yazın
3. [Örnekler](08-ornekler-ve-use-cases.md) → Örneklerle öğrenin

### Sistem Analistleri İçin
1. [Mimari ve Yapı](02-mimari-ve-yapi.md) → Sistemi anlayın
2. [Handler Geliştirme Rehberi](03-handler-gelistirme-rehberi.md) → Handler türlerini öğrenin
3. [Function System](04-function-system.md) → Dinamik değerler oluşturun
4. [Action System](05-action-system.md) → Aksiyonları tanımlayın
5. [Örnekler](08-ornekler-ve-use-cases.md) → Kompleks senaryolar

### Plugin Geliştiricileri İçin
1. [Mimari ve Yapı](02-mimari-ve-yapi.md) → Sistemin yapısını anlayın
2. [Plugin Geliştirme](06-plugin-gelistirme.md) → Plugin yazın
3. [Action System](05-action-system.md) → Custom action'lar geliştirin
4. [Troubleshooting](09-troubleshooting-ve-faq.md) → Debug yapın

### Son Kullanıcılar İçin
1. [Kurulum ve Genel Bakış](01-kurulum-ve-genel-bakis.md) → Uygulamayı kurun
2. [UI Özellikleri](07-ui-ozellikleri.md) → Arayüzü öğrenin
3. [Troubleshooting](09-troubleshooting-ve-faq.md) → Sorun giderme

---

## 🔍 Hızlı Arama

### Specific Topics

- **Regex Pattern Yazma**: [Handler Geliştirme → RegexHandler](03-handler-gelistirme-rehberi.md#regexhandler)
- **Database Query**: [Handler Geliştirme → DatabaseHandler](03-handler-gelistirme-rehberi.md#databasehandler)
- **API Integration**: [Handler Geliştirme → ApiHandler](03-handler-gelistirme-rehberi.md#apihandler)
- **Date Formatting**: [Function System → Date/Time Functions](04-function-system.md#datetime-functions)
- **String Manipulation**: [Function System → String Functions](04-function-system.md#string-functions)
- **Toast Notification**: [Action System → show_notification](05-action-system.md#2-show_notification)
- **Tab Opening**: [Action System → show_window](05-action-system.md#3-show_window)
- **Custom Action**: [Plugin Geliştirme → IAction Plugin](06-plugin-gelistirme.md#iaction-plugin)
- **Theme Değiştirme**: [UI Özellikleri → Theme Sistemi](07-ui-ozellikleri.md#theme-sistemi)
- **Keyboard Shortcut**: [Troubleshooting → Keyboard Shortcut Çalışmıyor](09-troubleshooting-ve-faq.md#keyboard-shortcut-çalışmıyor)

---

## 📊 Dokümantasyon İstatistikleri

- **Toplam Sayfa**: 9
- **Toplam Kelime**: ~50,000
- **Kod Örneği**: 200+
- **JSON Örneği**: 150+
- **Diagram**: 10+
- **Kapsam**: %100 (her kod satırı dokümante edildi)

---

## 💡 Dokümantasyon İlkeleri

Bu dokümantasyon hazırlanırken şu ilkeler izlenmiştir:

1. **Kapsamlılık**: Her kod satırı, her özellik detaylı anlatıldı
2. **Örneklerle Anlatım**: Her kavram için en az bir örnek
3. **Formal Dil**: Resmi, profesyonel, teknik dil kullanıldı
4. **Kod Odaklı**: Kod örnekleri ön planda
5. **Best Practices**: Her bölümde en iyi uygulamalar vurgulandı
6. **Troubleshooting**: Yaygın sorunlar ve çözümleri eklendi

---

## 🔄 Versiyon Bilgisi

- **Dokümantasyon Versiyonu**: 1.0.0
- **Contextualizer Versiyonu**: 1.0.0
- **Son Güncelleme**: 2025-10-09
- **Dil**: Türkçe

---

## 📝 Notlar

### Handler Geliştirme
- Handler geliştirme sırasında **03-handler-gelistirme-rehberi.md** ana kaynağınız olmalı
- Her handler tipi için "Step-by-Step" bölümlerini takip edin
- JSON örneklerini direkt kopyalayıp kullanabilirsiniz

### Function Kullanımı
- Function syntax'ı karıştırmayın:
  - Regular: `$func:today`
  - Chaining: `$func:today.format(yyyy-MM-dd)`
  - Pipeline: `$func:{{ input | func1 | func2 }}`
- Pipeline syntax okunabilirlik için tercih edilmelidir

### Plugin Geliştirme
- Plugin geliştirme öncesi **06-plugin-gelistirme.md** mutlaka okunmalı
- Code review checklist kullanın
- Best practices'leri atlamamak

### Troubleshooting
- Sorun yaşadığınızda ilk adım log dosyasını kontrol etmek
- FAQ bölümünde 50+ soru-cevap mevcut

---

## 🤝 Katkıda Bulunma

Bu dokümantasyon Contextualizer projesinin bir parçasıdır. Katkıda bulunmak için:

1. Hata/eksik bulduysanız → Confluence sayfasından iletişime geçebilirsiniz.
2. İyileştirme öneriniz varsa → Pull Request gönderin
3. Yeni örnek eklemek istiyorsanız → **08-ornekler-ve-use-cases.md** dosyasını güncelleyin

---

## 📞 Destek

- **Log Dosyası**: `logs/contextualizer.log`

**Başarılar! 🚀**

Contextualizer ile üretkenliğinizi artırmaya hazırsınız. Dokümantasyonu okuduktan sonra kendi handler'larınızı, plugin'lerinizi geliştirerek sistemi genişletebilirsiniz.

---


