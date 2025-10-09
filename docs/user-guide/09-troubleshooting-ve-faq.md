# Contextualizer - Troubleshooting ve FAQ

## 📋 İçindekiler
- [Genel Sorunlar](#genel-sorunlar)
- [Handler Sorunları](#handler-sorunları)
- [Function Sorunları](#function-sorunları)
- [Database Sorunları](#database-sorunları)
- [UI Sorunları](#ui-sorunları)
- [Plugin Sorunları](#plugin-sorunları)
- [Performance Sorunları](#performance-sorunları)
- [FAQ](#faq)

---

## Genel Sorunlar

### Uygulama Başlamıyor

**Semptomlar**:
- Uygulama açılmıyor
- Crash oluyor

**Çözümler**:
1. Log dosyasını kontrol edin: `logs/contextualizer.log`
2. `handlers.json` syntax'ını kontrol edin (JSON validator kullanın)
3. .NET 9.0 Runtime yüklü olduğundan emin olun
4. `appsettings.json` dosyasının olduğundan emin olun

### Keyboard Shortcut Çalışmıyor

**Semptomlar**:
- Win+Shift+C tuşları çalışmıyor

**Çözümler**:
1. Başka bir uygulamanın aynı shortcut'u kullanmadığını kontrol edin
2. Settings'ten farklı bir shortcut deneyin
3. Log'larda "KeyboardHook failed" araştırın

### Pano İçeriği Yakalanmıyor

**Semptomlar**:
- Win+Shift+C yapıyorum ama handler çalışmıyor

**Çözümler**:
1. Keyboard shortcut'u kullanın: Win+Shift+C
2. Activity log'da "No handlers matched" mesajı var mı kontrol edin
3. Ayarlardan clipboard süresini artırın.
4. Regex pattern'i test edin (regex101.com)

---

## Handler Sorunları

### Handler Yüklenmiyor

**Semptomlar**:
- `handlers.json`'daki handler çalışmıyor

**Çözümler**:
1. JSON syntax'ı kontrol edin
2. `type` değerinin doğru olduğundan emin olun:
   - `Regex`, `Database`, `Api`, `File`, `Lookup`, `Custom`, `Manual`, `Synthetic`, `Cron`
3. Log'larda "Handler loaded" mesajını arayın
4. `HandlerFactory` log'larını kontrol edin

### Regex Pattern Eşleşmiyor

**Semptomlar**:
- Regex handler hiçbir şey yakalamıyor

**Çözümler**:
1. Pattern'i https://regex101.com'da test edin
2. Named groups kullanın: `(?<name>pattern)`
3. Escape karakterleri iki kez kaçırın: `\\d` yerine `\\\\d`
4. Timeout kontrolü: 5 saniye
5. Log'larda regex hataları arayın

**Örnek**:
```json
{
  "pattern": "(?<email>[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,})"
}
```

### Database Handler Hata Veriyor

**Semptomlar**:
- "SQL query validation failed"

**Çözümler**:
1. Sadece SELECT sorguları desteklenir
2. Forbidden keywords: INSERT, UPDATE, DELETE, DROP, etc.
3. Multiple statements (`;`) yasaktır
4. Connection string'i kontrol edin (`$config:database.connection_string`)
5. SQL syntax'ını test edin

---

## Function Sorunları

### Function Çalışmıyor

**Semptomlar**:
- `$func:...` değiştirilmiyor

**Çözümler**:
1. Syntax kontrolü:
   - Regular: `$func:today`
   - Chaining: `$func:today.format(yyyy-MM-dd)`
   - Pipeline: `$func:{{ input | func1 | func2 }}`
2. Function adını kontrol edin (lowercase)
3. Parametre sayısını kontrol edin
4. Log'larda "Function processing failed" arayın

### Pipeline Function Hatası

**Semptomlar**:
- `$func:{{ }}` çalışmıyor

**Çözümler**:
1. Closing braces kontrolü: `}}`
2. Pipe (`|`) delimiter kullanın
3. İlk değer literal veya placeholder olabilir
4. Nested braces desteklenir

**Örnek**:
```json
{
  "result": "$func:{{ $(input) | string.upper | string.trim }}"
}
```

### Web Function Timeout

**Semptomlar**:
- `$func:web.get()` timeout veriyor

**Çözümler**:
1. URL'in erişilebilir olduğundan emin olun
2. Timeout: 30 saniye
3. Firewall/proxy kontrolü
4. Alternatif: API Handler kullanın (daha esnek)

---

## Database Sorunları

### Connection String Hatası

**Semptomlar**:
- "Database connection failed"

**Çözümler**:
1. `appsettings.json`'da connection string kontrolü
2. `$config:database.connection_string` syntax'ı doğru mu?
3. Database erişilebilir mi?
4. Credentials doğru mu?

**Örnek**:
```json
{
  "database": {
    "connection_string": "Server=localhost;Database=mydb;User Id=sa;Password=***;"
  }
}
```

### Oracle Connection Sorunları

**Semptomlar**:
- Oracle DB'ye bağlanamıyor

**Çözümler**:
1. Oracle.ManagedDataAccess.Core NuGet paketi yüklü mü?
2. Connection string format:
   ```
   Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=system;Password=***;
   ```
3. TNS names resolution
4. `db_type: "Oracle"` ayarlandı mı?

### Query Sonuçları Boş

**Semptomlar**:
- Query çalışıyor ama context boş

**Çözümler**:
1. Query gerçekten sonuç dönüyor mu? (SSMS'te test edin)
2. Column names doğru mu?
3. Context key format: `ColumnName0`, `ColumnName1`, etc.
4. `_table_output` Markdown table kullanımı

---

## UI Sorunları

### Tab Açılmıyor

**Semptomlar**:
- `show_window` action çalışmıyor

**Çözümler**:
1. `screen_id` doğru mu?
   - `markdown2`, `json_formatter`, `xml_formatter`, `plsql_editor`, `url_viewer`
2. `_body` key context'te var mı?
3. Log'larda "Screen not found" arayın
4. Custom screen için `IDynamicScreen` implement edilmiş mi?

### Theme Değişmiyor

**Semptomlar**:
- Theme toggle çalışmıyor

**Çözümler**:
1. `Themes/` klasöründe XAML dosyaları var mı?
2. `ThemeManager.Instance.ApplyTheme()` çağrılıyor mu?
3. `IThemeAware` implement edilmiş mi?
4. Log'larda "Theme changed" mesajı var mı?

### Toast Notification Görünmüyor

**Semptomlar**:
- `show_notification` action sessiz

**Çözümler**:
1. Windows notification settings aktif mi?
2. `_notification_title` ve `_duration` keys ayarlandı mı?
3. Log'larda "Notification failed" arayın
4. Alternative: Activity log'a bakın

---

## Plugin Sorunları

### Plugin Yüklenmiyor

**Semptomlar**:
- Custom plugin tanınmıyor

**Çözümler**:
1. DLL Contextualizer Plugins klasöründe mi?
2. .NET 9.0-windows target framework kullanılıyor mu?
3. `Contextualizer.PluginContracts.dll` reference edilmiş mi?
4. Class `public` mi?
5. Constructor parametresiz mi?
6. Log'larda "Action loaded" mesajı var mı?

### Plugin Hata Veriyor

**Semptomlar**:
- Plugin çalışıyor ama exception fırlatıyor

**Çözümler**:
1. Try-catch ekleyin
2. `IPluginServiceProvider` kullanın (service'lere erişim için)
3. Null checks ekleyin
4. Log service ile detaylı log ekleyin
5. Error mesajlarını kullanıcıya gösterin

**Örnek**:
```csharp
public async Task Action(ConfigAction action, ContextWrapper context)
{
    var logger = _serviceProvider.GetService<ILoggingService>();
    try
    {
        // Your logic
    }
    catch (Exception ex)
    {
        logger?.LogError($"Plugin error", ex);
        throw;
    }
}
```

---

## Performance Sorunları

### Uygulama Yavaş

**Semptomlar**:
- Handler execution yavaş

**Çözümler**:
1. Regex timeout kontrolü (5s)
2. Database query optimization
3. API request timeout (30s)
4. Function nesting minimization
5. Log level'i düşürün (Info → Warning)

### Memory Kullanımı Yüksek

**Semptomlar**:
- RAM kullanımı artıyor

**Çözümler**:
1. Activity log capacity limit: 50 entries
2. Tab sayısını azaltın
3. Large data'yı file'a yazın (memory'de tutmayın)
4. Cron job frequency azaltın
5. Handler dispose kontrolü

### Too Many Handlers

**Semptomlar**:
- Handler execution'lar yavaş

**Çözümler**:
1. Inactive handler'ları disable edin
2. Regex pattern'leri optimize edin (narrow matching)
3. Handler priority sistemi kullanın (first-match)
4. Parallel execution zaten var (`Task.WhenAll`)

---

## FAQ

### Genel

**Q: Hangi .NET versiyonu gerekli?**
A: .NET 9.0 Runtime (Windows)

**Q: Desteklenen OS'ler?**
A: Windows 10/11 (x64)

**Q: Keyboard shortcut değiştirebilir miyim?**
A: Evet, Settings → Keyboard Shortcut

### Handlers

**Q: Kaç handler tanımlayabilirim?**
A: Sınır yok

**Q: Handler execution sırası nedir?**
A: Parallel (`Task.WhenAll`), sıra garanti değil

**Q: Bir handler'ı geçici olarak disable edebilir miyim?**
A: Evet, `enabled: false` ekleyin

**Q: Regex ve Database handler'ı birleştirilebilir mi?**
A: Evet, Database handler optional regex destekler

### Configuration

**Q: `handlers.json` nerede?**
A: Uygulama klasöründe (Settings'ten değiştirilebilir)

**Q: Hot reload destekliyor mu?**
A: Hayır, uygulama restart gerekli

**Q: Environment variable kullanabilir miyim?**
A: Evet, `$func:env(VAR_NAME)` veya `$config:` prefix

### Database

**Q: Hangi database'ler desteklenir?**
A: MSSQL, Oracle (diğerleri için custom plugin)

**Q: Stored procedure çağırabilir miyim?**
A: Hayır, sadece SELECT queries

**Q: Transaction desteği var mı?**
A: Hayır, read-only operations

### API

**Q: Authentication desteği var mı?**
A: Evet, `headers` ile Bearer token, API key, etc.

**Q: GraphQL destekleniyor mu?**
A: POST ile body'de GraphQL query gönderebilirsiniz

**Q: Rate limiting var mı?**
A: Hayır, API provider'ın limits'lerine dikkat edin

### Functions

**Q: Custom function ekleyebilir miyim?**
A: Şu anda hayır, ancak plugin olarak action ekleyebilirsiniz

**Q: Function timeout nedir?**
A: Regex: 5s, Web requests: 30s

**Q: Function'lar asenkron mu?**
A: Web functions evet, diğerleri sync

### UI

**Q: Custom screen ekleyebilir miyim?**
A: Evet, `IDynamicScreen` implement edin

**Q: Tab limit var mı?**
A: Hayır

**Q: Theme customize edebilir miyim?**
A: Evet, XAML dosyalarını düzenleyin

### Plugins

**Q: Plugin hot reload destekliyor mu?**
A: Hayır, restart gerekli

**Q: Plugin marketplace var mı?**
A: Handler Exchange sistemi var 

**Q: Plugin debug nasıl yapılır?**
A: Visual Studio attach to process

---

## Sonraki Adımlar

✅ **Troubleshooting öğrenildi!** Artık:

1. 📖 [Ana README](README.md) ile genel bakış
2. 📚 Diğer dokümantasyon bölümlerine dönün

---

## Destek

Sorun çözemediyseniz:
1. Log dosyasını kontrol edin: `logs/contextualizer.log`
2. GitHub Issues'da arayın
3. Yeni issue açın (log excerpt ekleyin)

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

