# Contextualizer - Handler Geliştirme Rehberi

## 📋 İçindekiler
- [Handler Temelleri](#handler-temelleri)
- [Regex Handler](#1-regex-handler)
- [Database Handler](#2-database-handler)
- [API Handler](#3-api-handler)
- [File Handler](#4-file-handler)
- [Lookup Handler](#5-lookup-handler)
- [Custom Handler](#6-custom-handler)
- [Manual Handler](#7-manual-handler)
- [Synthetic Handler](#8-synthetic-handler)
- [Cron Handler](#9-cron-handler)
- [Best Practices](#best-practices)

---

## Handler Temelleri

### IHandler Interface

Tüm handler'lar `IHandler` interface'ini implement eder:

```csharp
public interface IHandler
{
    static virtual string TypeName => throw new NotImplementedException();
    Task<bool> CanHandle(ClipboardContent clipboardContent);
    Task<bool> Execute(ClipboardContent clipboardContent);
    HandlerConfig HandlerConfig { get; }
}
```

### Base Class: Dispatch

Pratik olarak, tüm handler'lar `Dispatch` abstract class'ını extend eder:

```csharp
public abstract class Dispatch
{
    // Template Method - Final execution workflow
    public async Task<bool> Execute(ClipboardContent clipboardContent);
    
    // Alt sınıflar implement etmeli
    protected abstract Task<bool> CanHandleAsync(ClipboardContent clipboardContent);
    protected abstract Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent);
    protected abstract List<ConfigAction> GetActions();
    protected abstract string OutputFormat { get; }
}
```

### Handler JSON Yapısı

Temel handler JSON template:

```json
{
  "name": "Handler Adı",
  "description": "Handler açıklaması",
  "type": "handler_tipi",
  "screen_id": "markdown2",
  "title": "Sonuç Penceresi Başlığı",
  
  "regex": "\\d+",
  "groups": ["group1", "group2"],
  
  "requires_confirmation": false,
  "auto_focus_tab": false,
  "bring_window_to_front": false,
  
  "constant_seeder": {
    "static_key": "static_value"
  },
  "seeder": {
    "dynamic_key": "$func:now()"
  },
  "user_inputs": [
    {
      "key": "user_data",
      "title": "Veri Gir",
      "message": "Lütfen veri giriniz:",
      "is_required": true
    }
  ],
  "output_format": "# Sonuç\n\n$(key)",
  
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### Ortak Özellikler

| Özellik | Tip | Açıklama |
|---------|-----|----------|
| `name` | string | Handler'ın benzersiz adı |
| `description` | string | Handler'ın açıklaması |
| `type` | string | Handler tipi (regex, database, api, vb.) |
| `screen_id` | string | Hangi ekranda gösterilecek (markdown2, json_formatter, xml_formatter, url_viewer) |
| `title` | string | Sekme başlığı |
| `requires_confirmation` | bool | Çalıştırmadan önce onay iste |
| `auto_focus_tab` | bool | Sekmeyi otomatik aktif et |
| `bring_window_to_front` | bool | Pencereyi öne getir |
| `constant_seeder` | object | Statik key-value çiftleri |
| `seeder` | object | Dinamik key-value çiftleri |
| `user_inputs` | array | Kullanıcıdan alınacak girişler |
| `output_format` | string | Çıktı şablonu |
| `actions` | array | Çalıştırılacak aksiyonlar |

---

## 1. Regex Handler

**Dosya**: `Contextualizer.Core/RegexHandler.cs`  
**Type**: `"Regex"`

Metin desenlerine (pattern) dayalı işleme yapar. En sık kullanılan handler tipidir.

### Teknik Detaylar

#### Constructor
```csharp
public RegexHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    // Regex'i compile et (10-20x performance)
    _compiledRegex = new Regex(
        handlerConfig.Regex, 
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(5) // ReDoS protection
    );
}
```

**Not**: Regex compile edildiği için çok hızlıdır. Ancak 5 saniye timeout ile ReDoS saldırılarına karşı korunur.

#### CanHandle Implementation
```csharp
protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
{
    if (clipboardContent?.Text == null) return false;

    try
    {
        return _compiledRegex.IsMatch(clipboardContent.Text);
    }
    catch (RegexMatchTimeoutException ex)
    {
        // Timeout durumunda false dön
        return false;
    }
}
```

#### CreateContext Implementation
```csharp
protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
{
    var context = new Dictionary<string, string>
    {
        [ContextKey._input] = clipboardContent.Text
    };

    var match = _compiledRegex.Match(clipboardContent.Text);
    
    if (match.Success)
    {
        // Full match
        context[ContextKey._match] = match.Value;
        
        // Named/indexed groups
        if (HandlerConfig.Groups != null)
        {
            for (int i = 0; i < HandlerConfig.Groups.Count; i++)
            {
                var groupName = HandlerConfig.Groups[i];
                
                // Try named group first
                var namedGroup = match.Groups[groupName];
                if (namedGroup.Success)
                {
                    context[groupName] = namedGroup.Value;
                }
                else
                {
                    // Fall back to indexed group
                    var groupIndex = i + 1;
                    context[groupName] = match.Groups.Count > groupIndex 
                        ? match.Groups[groupIndex].Value 
                        : string.Empty;
                }
            }
        }
        else
        {
            // Auto-discover all groups
            for (int i = 1; i < match.Groups.Count; i++)
            {
                context[$"group_{i}"] = match.Groups[i].Value;
            }
        }
    }
    
    return context;
}
```

### Kullanım Örnekleri

#### Örnek 1: Basit Pattern Matching - Sipariş Numarası

```json
{
  "name": "Sipariş Numarası Yakalama",
  "type": "Regex",
  "regex": "ORDER\\d{5}",
  "screen_id": "markdown2",
  "output_format": "# Sipariş Detayı\n\n- Sipariş No: $(_match)\n- Yakalanan: $(_input)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "Müşteri ORDER12345 için ödeme bekleniyor"
Sonuç: _match = "ORDER12345"
```

#### Örnek 2: Named Groups - Email Parse

```json
{
  "name": "Email Parse",
  "type": "Regex",
  "regex": "(?<username>[a-zA-Z0-9._%+-]+)@(?<domain>[a-zA-Z0-9.-]+)\\.(?<tld>[a-zA-Z]{2,})",
  "groups": ["username", "domain", "tld"],
  "screen_id": "markdown2",
  "output_format": "# Email Bilgileri\n\n- Username: $(username)\n- Domain: $(domain)\n- TLD: $(tld)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "john.doe@example.com"
Sonuç: 
  username = "john.doe"
  domain = "example"
  tld = "com"
```

#### Örnek 3: Indexed Groups - Telefon Numarası

```json
{
  "name": "Telefon Parse",
  "type": "Regex",
  "regex": "\\+(\\d{2})\\s*\\((\\d{3})\\)\\s*(\\d{3})\\s*(\\d{2})\\s*(\\d{2})",
  "groups": ["country", "area", "part1", "part2", "part3"],
  "screen_id": "markdown2",
  "output_format": "# Telefon\n\n- Ülke: +$(country)\n- Alan: $(area)\n- Numara: $(part1)-$(part2)-$(part3)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "+90 (532) 123 45 67"
Sonuç:
  country = "90"
  area = "532"
  part1 = "123"
  part2 = "45"
  part3 = "67"
```

#### Örnek 4: URL Parse

```json
{
  "name": "URL Parse",
  "type": "Regex",
  "regex": "^(?<protocol>https?://)?(?<subdomain>[^./]+\\.)?(?<domain>[^./]+)\\.(?<tld>[^/]+)(?<path>/.*)?$",
  "groups": ["protocol", "subdomain", "domain", "tld", "path"],
  "screen_id": "markdown2",
  "seeder": {
    "full_domain": "$(subdomain)$(domain).$(tld)",
    "protocol_display": "$func:{{ $(protocol) | string.trim | string.replace(://,) | string.upper }}"
  },
  "output_format": "# URL Analizi\n\n- Protocol: $(protocol_display)\n- Domain: $(full_domain)\n- Path: $(path)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 5: IBAN Validator

```json
{
  "name": "IBAN Validator",
  "type": "Regex",
  "regex": "^(?<country>[A-Z]{2})(?<check>\\d{2})(?<bank>\\d{5})(?<branch>\\d{1})(?<account>\\d{16})$",
  "groups": ["country", "check", "bank", "branch", "account"],
  "screen_id": "markdown2",
  "seeder": {
    "bank_code": "$(bank)-$(branch)",
    "validation_status": "✅ Geçerli IBAN Formatı"
  },
  "output_format": "# IBAN Bilgileri\n\n$(validation_status)\n\n- Ülke: $(country)\n- Check Digit: $(check)\n- Banka: $(bank_code)\n- Hesap: $(account)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    },
    {
      "name": "copytoclipboard",
      "key": "account"
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **Regex compile edin**: Handler constructor'da compile edilir, performans için önemli
- **Named groups kullanın**: Okunabilir ve bakımı kolay
- **Timeout ekleyin**: ReDoS saldırılarına karşı koruma (otomatik 5 saniye)
- **Specific patterns yazın**: Mümkün olduğunca dar pattern

#### ❌ DON'T
- **Aşırı complex regex**: Test edin, basit tutun
- **Greedy quantifiers (.*)**: Performans problemi
- **Context varsayımları**: CanHandle false dönebilir

---

## 2. Database Handler

**Dosya**: `Contextualizer.Core/DatabaseHandler.cs`  
**Type**: `"Database"`

SQL Server veya Oracle veritabanlarına sorgu çalıştırır.

### Teknik Detaylar

#### Constructor
```csharp
public DatabaseHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    // Optional regex for pre-filtering
    if (!string.IsNullOrEmpty(handlerConfig.Regex))
    {
        _optionalRegex = new Regex(
            handlerConfig.Regex,
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(5)
        );
    }
}
```

#### Güvenlik: SQL Injection Prevention
```csharp
public bool IsSafeSqlQuery(string query)
{
    var lowerQuery = query.ToLowerInvariant().AsSpan().Trim();
    
    // Sadece SELECT izin ver
    if (!lowerQuery.StartsWith("select ")) return false;
    
    // Yasak keyword'ler
    var forbidden = new[] {
        "insert ", "update ", "delete ", "drop ", "alter ", "create ",
        "exec ", "execute ", "truncate ", "merge ", "grant ", "revoke ",
        "shutdown", "--", "/*", "*/", "xp_", "sp_", ";"
    };
    
    foreach (var keyword in forbidden)
    {
        if (query.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            return false;
    }
    
    return true;
}
```

**Önemli**: Database Handler **sadece SELECT sorgularına** izin verir. INSERT, UPDATE, DELETE vb. kesinlikle engellenmiştir.

#### CanHandle Implementation
```csharp
protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
{
    // Basic validation
    if (!clipboardContent.IsText || 
        string.IsNullOrEmpty(clipboardContent.Text) ||
        string.IsNullOrEmpty(HandlerConfig.Query) ||
        string.IsNullOrEmpty(HandlerConfig.ConnectionString))
        return false;
    
    // SQL safety check
    string resolvedQuery = HandlerContextProcessor.ReplaceDynamicValues(
        HandlerConfig.Query, 
        new Dictionary<string, string>()
    );
    
    if (!IsSafeSqlQuery(resolvedQuery)) return false;
    
    // Optional regex filtering
    if (_optionalRegex != null)
    {
        if (!_optionalRegex.IsMatch(clipboardContent.Text))
            return false;
        
        // Extract regex groups as parameters
        var match = _optionalRegex.Match(clipboardContent.Text);
        string safeInput = clipboardContent.Text.Length > 4000 
            ? clipboardContent.Text.Substring(0, 4000) 
            : clipboardContent.Text;
        
        parameters["p_input"] = safeInput;
        parameters["p_match"] = match.Value;
        
        // Process groups (named or indexed)
        // Max 20 groups to prevent SQL parameter overflow
        // Max 4000 chars per parameter (SQL varchar limit)
    }
    
    return true;
}
```

#### CreateContext Implementation
```csharp
protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
{
    resultSet = new Dictionary<string, string>();
    
    // Create connection (pooled via ConnectionManager)
    using IDbConnection connection = CreateConnection();
    
    // Create parameters
    DynamicParameters dynamicParameters = CreateDynamicParameters();
    
    // Resolve $file: and $config: in query
    string resolvedQuery = HandlerContextProcessor.ReplaceDynamicValues(
        HandlerConfig.Query, 
        new Dictionary<string, string>()
    );
    
    // Execute query (Dapper)
    int commandTimeout = HandlerConfig.CommandTimeoutSeconds ?? 30;
    var queryResults = await connection.QueryAsync(
        resolvedQuery, 
        dynamicParameters, 
        commandTimeout: commandTimeout
    );
    
    int rowCount = queryResults.Count();
    resultSet[ContextKey._count] = rowCount.ToString();
    
    // Flatten results: ColumnName#RowNumber
    int rowNumber = 1;
    foreach (var row in queryResults)
    {
        foreach (var property in row)
        {
            string key = $"{property.Key}#{rowNumber}";
            resultSet[key] = property.Value?.ToString() ?? string.Empty;
        }
        rowNumber++;
    }
    
    return resultSet;
}
```

#### Auto Markdown Table Generation
```csharp
private string GenerateMarkdownTable(Dictionary<string, string> resultSet)
{
    if (resultSet[ContextKey._count] == "0")
        return "No data available.";
    
    // Extract headers
    var headers = resultSet.Keys
        .Where(key => key.Contains("#"))
        .Select(key => key.Split('#')[0])
        .Distinct()
        .ToList();
    
    // Extract row numbers
    var rowNumbers = resultSet.Keys
        .Where(key => key.Contains("#"))
        .Select(key => int.Parse(key.Split('#')[1]))
        .Distinct()
        .OrderBy(row => row)
        .ToList();
    
    var markdownBuilder = new StringBuilder();
    
    // Header row
    markdownBuilder.Append("| Row | ");
    markdownBuilder.Append(string.Join(" | ", headers));
    markdownBuilder.AppendLine(" |");
    
    // Separator row
    markdownBuilder.Append("|---|");
    markdownBuilder.Append(string.Join("|", headers.Select(_ => "---")));
    markdownBuilder.AppendLine("|");
    
    // Data rows
    foreach (var rowNumber in rowNumbers)
    {
        markdownBuilder.Append($"| {rowNumber} | ");
        var rowValues = headers.Select(header =>
        {
            var key = $"{header}#{rowNumber}";
            return resultSet.ContainsKey(key) ? resultSet[key] : "";
        });
        markdownBuilder.Append(string.Join(" | ", rowValues));
        markdownBuilder.AppendLine(" |");
    }
    
    return markdownBuilder.ToString();
}
```

### Kullanım Örnekleri

#### Örnek 1: IBAN ile Müşteri Sorgu

```json
{
  "name": "IBAN Müşteri Sorgu",
  "type": "Database",
  "regex": "TR\\d{24}",
  "connectionString": "$config:database.customer_db",
  "connector": "mssql",
  "query": "SELECT TOP 10 CustomerID, Name, Phone, Email, Balance FROM Customers WHERE IBAN = @p_input ORDER BY Balance DESC",
  "screen_id": "markdown2",
  "output_format": "# Müşteri Bilgileri\n\n**IBAN**: $(_input)\n\n$(_formatted_output)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "TR123456789012345678901234"
SQL: SELECT TOP 10 ... WHERE IBAN = @p_input
Parameters: @p_input = "TR123456789012345678901234"
Sonuç: Markdown tablo otomatik oluşturulur
```

**Context**:
```
_input = "TR123456789012345678901234"
_count = "3"
CustomerID#1 = "12345"
Name#1 = "Ali Yılmaz"
Phone#1 = "+90 532 123 45 67"
CustomerID#2 = "67890"
Name#2 = "Ayşe Demir"
...
_formatted_output = "| Row | CustomerID | Name | ... |
                     |-----|------------|------|-----|
                     | 1   | 12345      | Ali  | ... |"
```

#### Örnek 2: Müşteri ID ile Full Profile

```json
{
  "name": "Müşteri Profile",
  "type": "Database",
  "regex": "CUST_(\\d+)",
  "groups": ["customer_id"],
  "connectionString": "$config:database.customer_db",
  "connector": "mssql",
  "query": "SELECT c.*, a.Street, a.City, a.Country, t.TotalTransactions, t.TotalAmount FROM Customers c LEFT JOIN Addresses a ON c.AddressID = a.ID LEFT JOIN (SELECT CustomerID, COUNT(*) as TotalTransactions, SUM(Amount) as TotalAmount FROM Transactions GROUP BY CustomerID) t ON c.CustomerID = t.CustomerID WHERE c.CustomerID = @p_customer_id",
  "screen_id": "markdown2",
  "seeder": {
    "timestamp": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "query_by": "$func:username()"
  },
  "output_format": "# Müşteri Profili - $(Name#1)\n\n**Customer ID**: $(customer_id)  \n**Query Time**: $(timestamp)  \n**Queried By**: $(query_by)\n\n## Detaylar\n\n$(_formatted_output)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 3: Dinamik Query - User Input ile Filtering

```json
{
  "name": "Müşteri Arama",
  "type": "Database",
  "connectionString": "$config:database.customer_db",
  "connector": "mssql",
  "user_inputs": [
    {
      "key": "search_field",
      "title": "Arama Alanı",
      "message": "Hangi alanda arama yapmak istiyorsunuz?",
      "is_required": true,
      "is_selection_list": true,
      "selection_items": [
        {"value": "Name", "display": "İsim"},
        {"value": "Email", "display": "Email"},
        {"value": "Phone", "display": "Telefon"},
        {"value": "IBAN", "display": "IBAN"}
      ]
    },
    {
      "key": "search_value",
      "title": "Arama Değeri",
      "message": "Aramak istediğiniz değeri girin:",
      "is_required": true
    }
  ],
  "seeder": {
    "safe_search_field": "$func:string.replace($(search_field), ', )",
    "like_pattern": "%$(search_value)%"
  },
  "query": "SELECT TOP 20 CustomerID, Name, Phone, Email FROM Customers WHERE $(safe_search_field) LIKE @p_like_pattern",
  "screen_id": "markdown2",
  "output_format": "# Arama Sonuçları\n\n**Arama**: $(search_field) LIKE '$(like_pattern)'\n\n$(_formatted_output)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**UYARI**: Yukarıdaki örnekte SQL injection riski vardır. Gerçek uygulamada column adı validasyonu yapılmalıdır.

#### Örnek 4: Oracle Database - PL/SQL

```json
{
  "name": "Oracle Employee Lookup",
  "type": "Database",
  "regex": "EMP\\d{5}",
  "connectionString": "$config:database.oracle_hr",
  "connector": "plsql",
  "query": "SELECT e.EMPLOYEE_ID, e.FIRST_NAME, e.LAST_NAME, e.EMAIL, d.DEPARTMENT_NAME, j.JOB_TITLE, e.SALARY FROM HR.EMPLOYEES e LEFT JOIN HR.DEPARTMENTS d ON e.DEPARTMENT_ID = d.DEPARTMENT_ID LEFT JOIN HR.JOBS j ON e.JOB_ID = j.JOB_ID WHERE e.EMPLOYEE_ID = :p_match",
  "screen_id": "markdown2",
  "output_format": "# Employee Information\n\n$(_formatted_output)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Not**: Oracle için parameter prefix `:` kullanılır (MSSQL'de `@`).

#### Örnek 5: Connection Pooling Ayarları

```json
{
  "name": "Performance Optimized Query",
  "type": "Database",
  "connectionString": "$config:database.customer_db",
  "connector": "mssql",
  "command_timeout_seconds": 60,
  "connection_timeout_seconds": 15,
  "max_pool_size": 100,
  "min_pool_size": 5,
  "disable_pooling": false,
  "regex": "ORDER\\d+",
  "query": "SELECT * FROM Orders WHERE OrderID = @p_match",
  "screen_id": "markdown2",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **$config: kullanın**: Connection string'i secrets.json'da saklayın
- **Parameterized queries**: SQL injection'a karşı korunur (otomatik)
- **Connection pooling**: ConnectionManager otomatik yönetir
- **SELECT only**: Güvenlik için sadece okuma
- **Timeout ayarlayın**: `command_timeout_seconds` belirtin
- **Regex pre-filter**: Gereksiz DB çağrılarını önleyin

#### ❌ DON'T
- **Direct string concatenation**: ASLA! SQL injection riski
- **INSERT/UPDATE/DELETE**: Engellenmiştir
- **Dynamic column names**: SQL injection riski
- **Long running queries**: Timeout ayarlayın
- **Hardcode connection strings**: $config: kullanın

---

## 3. API Handler

**Dosya**: `Contextualizer.Core/ApiHandler.cs`  
**Type**: `"Api"`

REST API'lere HTTP request gönderir ve response'u işler.

### Teknik Detaylar

#### Constructor - Optimized HttpClient
```csharp
public ApiHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    // Optional regex pre-filtering
    if (!string.IsNullOrWhiteSpace(handlerConfig.Regex))
    {
        _optionalRegex = new Regex(
            handlerConfig.Regex,
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(5)
        );
    }
    
    // Optimized HttpClient for long-running apps
    _httpClient = CreateOptimizedHttpClient(handlerConfig);
}

private HttpClient CreateOptimizedHttpClient(HandlerConfig handlerConfig)
{
    var handler = new SocketsHttpHandler()
    {
        MaxConnectionsPerServer = 10,
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
    };
    
    var httpClient = new HttpClient(handler);
    
    // Timeout
    httpClient.Timeout = handlerConfig.TimeoutSeconds.HasValue 
        ? TimeSpan.FromSeconds(handlerConfig.TimeoutSeconds.Value)
        : TimeSpan.FromSeconds(30);
    
    // Keep-Alive
    httpClient.DefaultRequestHeaders.ConnectionClose = false;
    httpClient.DefaultRequestHeaders.Add("Keep-Alive", "timeout=300, max=1000");
    
    // Custom headers
    if (handlerConfig.Headers != null)
    {
        foreach (var header in handlerConfig.Headers)
        {
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }
    
    return httpClient;
}
```

**Performans**: Connection pooling sayesinde her request için yeni connection açılmaz.

#### CreateContext - JSON Flattening
```csharp
protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
{
    var context = new Dictionary<string, string>();
    context[ContextKey._input] = clipboardContent.Text;
    
    // Optional regex groups
    if (_optionalRegex != null)
    {
        var match = _optionalRegex.Match(clipboardContent.Text);
        if (match.Success)
        {
            context[ContextKey._match] = match.Value;
            // Extract groups...
        }
    }
    
    // Resolve dynamic values in URL
    string url = HandlerContextProcessor.ReplaceDynamicValues(HandlerConfig.Url, context);
    
    // Create request
    var request = new HttpRequestMessage(new HttpMethod(HandlerConfig.Method ?? "GET"), url);
    
    // Request body
    if (HandlerConfig.RequestBody.HasValue)
    {
        string jsonString = HandlerConfig.RequestBody.Value.GetRawText();
        string body = HandlerContextProcessor.ReplaceDynamicValues(jsonString, context);
        request.Content = new StringContent(body, Encoding.UTF8, HandlerConfig.ContentType ?? "application/json");
    }
    
    // Send request
    var response = await _httpClient.SendAsync(request);
    context["StatusCode"] = ((int)response.StatusCode).ToString();
    context["IsSuccessful"] = response.IsSuccessStatusCode.ToString();
    
    string responseContent = await response.Content.ReadAsStringAsync();
    
    // Parse JSON response
    if (response.Content.Headers.ContentType?.MediaType?.Contains("json") == true)
    {
        var jsonDoc = JsonDocument.Parse(responseContent);
        context["RawResponse"] = responseContent;
        FlattenJsonToContext(jsonDoc.RootElement, "", context);
    }
    else
    {
        context["RawResponse"] = responseContent;
    }
    
    return context;
}
```

#### JSON Flattening Algoritması
```csharp
private void FlattenJsonToContext(JsonElement element, string prefix, Dictionary<string, string> context)
{
    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            foreach (var property in element.EnumerateObject())
            {
                string newPrefix = string.IsNullOrEmpty(prefix) 
                    ? property.Name 
                    : $"{prefix}.{property.Name}";
                FlattenJsonToContext(property.Value, newPrefix, context);
            }
            break;
        
        case JsonValueKind.Array:
            int index = 0;
            foreach (var item in element.EnumerateArray())
            {
                string newPrefix = string.IsNullOrEmpty(prefix) 
                    ? index.ToString() 
                    : $"{prefix}[{index}]";
                FlattenJsonToContext(item, newPrefix, context);
                index++;
            }
            break;
        
        default:
            if (!string.IsNullOrEmpty(prefix))
            {
                context[prefix] = element.ToString();
            }
            break;
    }
}
```

**Örnek JSON Flattening**:
```json
{
  "user": {
    "id": 123,
    "name": "John",
    "emails": ["john@example.com", "jdoe@example.com"]
  }
}
```

**Flatten edilmiş context**:
```
user.id = "123"
user.name = "John"
user.emails[0] = "john@example.com"
user.emails[1] = "jdoe@example.com"
```

### Kullanım Örnekleri

#### Örnek 1: GitHub User API

```json
{
  "name": "GitHub User Info",
  "type": "Api",
  "regex": "^[a-zA-Z0-9-]+$",
  "url": "https://api.github.com/users/$(clipboard_text)",
  "method": "GET",
  "headers": {
    "User-Agent": "Contextualizer",
    "Accept": "application/vnd.github.v3+json"
  },
  "screen_id": "markdown2",
  "output_format": "# GitHub User: $(login)\n\n- **Name**: $(name)\n- **Bio**: $(bio)\n- **Public Repos**: $(public_repos)\n- **Followers**: $(followers)\n- **Following**: $(following)\n- **Created**: $(created_at)\n- **Profile**: $(html_url)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "octocat"
URL: https://api.github.com/users/octocat
Method: GET
Response flatten:
  login = "octocat"
  name = "The Octocat"
  bio = "GitHub mascot"
  public_repos = "8"
  followers = "9527"
  ...
```

#### Örnek 2: POST Request - Slack Notification

```json
{
  "name": "Slack Notification",
  "type": "Api",
  "url": "$config:slack.webhook_url",
  "method": "POST",
  "content_type": "application/json",
  "request_body": {
    "text": "$(clipboard_text)",
    "username": "Contextualizer Bot",
    "icon_emoji": ":clipboard:"
  },
  "user_inputs": [
    {
      "key": "channel",
      "title": "Slack Channel",
      "message": "Hangi kanala göndermek istiyorsunuz?",
      "is_required": true,
      "is_selection_list": true,
      "selection_items": [
        {"value": "#general", "display": "General"},
        {"value": "#dev", "display": "Development"},
        {"value": "#alerts", "display": "Alerts"}
      ]
    }
  ],
  "seeder": {
    "timestamp": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "sender": "$func:username()"
  },
  "output_format": "# Slack Message Sent\n\n- Channel: $(channel)\n- Message: $(clipboard_text)\n- Sent by: $(sender)\n- Time: $(timestamp)\n- Status: $(StatusCode)",
  "actions": [
    {
      "name": "show_notification",
      "message": "Slack mesajı gönderildi!"
    }
  ]
}
```

#### Örnek 3: Weather API with Geocoding

```json
{
  "name": "Weather Lookup",
  "type": "Api",
  "regex": "^[a-zA-Z\\s,]+$",
  "url": "https://api.openweathermap.org/data/2.5/weather?q=$(clipboard_text)&appid=$config:openweather.api_key&units=metric&lang=tr",
  "method": "GET",
  "timeout_seconds": 10,
  "screen_id": "markdown2",
  "seeder": {
    "temp_formatted": "$(main.temp)°C",
    "feels_like_formatted": "$(main.feels_like)°C",
    "humidity_formatted": "$(main.humidity)%",
    "wind_speed_formatted": "$(wind.speed) m/s"
  },
  "output_format": "# Hava Durumu: $(name), $(sys.country)\n\n## Genel\n- **Durum**: $(weather[0].description)\n- **Sıcaklık**: $(temp_formatted)\n- **Hissedilen**: $(feels_like_formatted)\n\n## Detaylar\n- **Nem**: $(humidity_formatted)\n- **Basınç**: $(main.pressure) hPa\n- **Rüzgar**: $(wind_speed_formatted)\n- **Bulutluluk**: $(clouds.all)%",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 4: REST API with Authentication

```json
{
  "name": "Internal API Call",
  "type": "Api",
  "url": "https://api.internal.com/v1/customers/$(customer_id)",
  "method": "GET",
  "headers": {
    "Authorization": "Bearer $config:api.access_token",
    "X-API-Key": "$config:api.key",
    "Accept": "application/json"
  },
  "user_inputs": [
    {
      "key": "customer_id",
      "title": "Customer ID",
      "message": "Enter customer ID:",
      "is_required": true,
      "validation_regex": "^\\d+$"
    }
  ],
  "screen_id": "json_formatter",
  "actions": [
    {
      "name": "show_window",
      "key": "RawResponse",
      "title": "API Response"
    }
  ]
}
```

#### Örnek 5: GraphQL Query

```json
{
  "name": "GraphQL User Query",
  "type": "Api",
  "url": "https://api.example.com/graphql",
  "method": "POST",
  "content_type": "application/json",
  "headers": {
    "Authorization": "Bearer $config:graphql.token"
  },
  "request_body": {
    "query": "query GetUser($id: ID!) { user(id: $id) { id name email posts { title createdAt } } }",
    "variables": {
      "id": "$(user_id)"
    }
  },
  "user_inputs": [
    {
      "key": "user_id",
      "title": "User ID",
      "message": "Enter GraphQL user ID:",
      "is_required": true
    }
  ],
  "screen_id": "json_formatter",
  "actions": [
    {
      "name": "show_window",
      "key": "RawResponse"
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **$config: for secrets**: API keys, tokens hardcode etmeyin
- **Connection pooling**: Otomatik yönetilir
- **Timeout ayarlayın**: `timeout_seconds` belirtin
- **Error handling**: `StatusCode` ve `IsSuccessful` kontrol edin
- **Keep-Alive**: Otomatik aktif
- **Rate limiting**: API limitlerini gözetin

#### ❌ DON'T
- **Hardcode API keys**: $config: kullanın
- **Ignore status codes**: Her zaman kontrol edin
- **Synchronous calls**: Otomatik async
- **No timeout**: Uzun süren requestler uygulamayı bloklar

---

## 4. File Handler

**Dosya**: `Contextualizer.Core/FileHandler.cs`  
**Type**: `"File"`

Dosya/klasör yolu kopyalandığında 25+ özelliği otomatik okur.

### Teknik Detaylar

#### CreateContext - 25+ Properties
```csharp
protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
{
    // Kapasiteyi önceden ayarla (performance)
    var context = new Dictionary<string, string>(clipboardContent.Files.Length * 25 + 1);
    
    for (int i = 0; i < clipboardContent.Files.Length; i++)
    {
        var fileProperties = GetFullFileInfoDictionary(clipboardContent.Files[i], i);
        
        foreach (var kvp in fileProperties)
        {
            context[kvp.Key] = kvp.Value;
        }
    }
    
    context[ContextKey._count] = clipboardContent.Files.Length.ToString();
    
    return context;
}

private static Dictionary<string, string> GetFullFileInfoDictionary(string filePath, int fileIndex)
{
    var fileInfoDictionary = new Dictionary<string, string>(25);
    
    if (!File.Exists(filePath))
    {
        fileInfoDictionary.Add(nameof(FileInfoKeys.NotFound), "File not found");
        return fileInfoDictionary;
    }
    
    var fileInfo = new FileInfo(filePath);
    var attributes = fileInfo.Attributes;
    
    // Basic info
    fileInfoDictionary.Add(nameof(FileInfoKeys.FileName) + fileIndex, fileInfo.Name);
    fileInfoDictionary.Add(nameof(FileInfoKeys.FullPath) + fileIndex, fileInfo.FullName);
    fileInfoDictionary.Add(nameof(FileInfoKeys.Extension) + fileIndex, fileInfo.Extension);
    fileInfoDictionary.Add(nameof(FileInfoKeys.SizeBytes) + fileIndex, fileInfo.Length.ToString());
    
    // Date/time
    fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDate) + fileIndex, fileInfo.CreationTime.ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDateUtc) + fileIndex, fileInfo.CreationTimeUtc.ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccess) + fileIndex, fileInfo.LastAccessTime.ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccessUtc) + fileIndex, fileInfo.LastAccessTimeUtc.ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.LastWrite) + fileIndex, fileInfo.LastWriteTime.ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.LastWriteUtc) + fileIndex, fileInfo.LastWriteTimeUtc.ToString());
    
    // Properties
    fileInfoDictionary.Add(nameof(FileInfoKeys.ReadOnly) + fileIndex, fileInfo.IsReadOnly.ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Exists) + fileIndex, fileInfo.Exists.ToString());
    
    // Attributes (via HasFlag)
    fileInfoDictionary.Add(nameof(FileInfoKeys.Hidden) + fileIndex, attributes.HasFlag(FileAttributes.Hidden).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.System) + fileIndex, attributes.HasFlag(FileAttributes.System).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Archive) + fileIndex, attributes.HasFlag(FileAttributes.Archive).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Compressed) + fileIndex, attributes.HasFlag(FileAttributes.Compressed).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Temporary) + fileIndex, attributes.HasFlag(FileAttributes.Temporary).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Offline) + fileIndex, attributes.HasFlag(FileAttributes.Offline).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Encrypted) + fileIndex, attributes.HasFlag(FileAttributes.Encrypted).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryOnly) + fileIndex, attributes.HasFlag(FileAttributes.Directory).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.ReparsePoint) + fileIndex, attributes.HasFlag(FileAttributes.ReparsePoint).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Sparse) + fileIndex, attributes.HasFlag(FileAttributes.SparseFile).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Device) + fileIndex, attributes.HasFlag(FileAttributes.Device).ToString());
    fileInfoDictionary.Add(nameof(FileInfoKeys.Normal) + fileIndex, attributes.HasFlag(FileAttributes.Normal).ToString());
    
    // Directory
    fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryPath) + fileIndex, fileInfo.DirectoryName ?? "N/A");
    fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryObject) + fileIndex, fileInfo.Directory?.FullName ?? "N/A");
    
    return fileInfoDictionary;
}
```

#### File Property Keys (25+ Property)

| Key | Açıklama | Örnek Değer |
|-----|----------|-------------|
| `FileName0` | Dosya adı (extension ile) | `document.pdf` |
| `FullPath0` | Tam dosya yolu | `C:\Users\...\document.pdf` |
| `Extension0` | Dosya uzantısı | `.pdf` |
| `SizeBytes0` | Boyut (byte) | `1048576` |
| `CreationDate0` | Oluşturma tarihi (local) | `2025-10-09 14:30:25` |
| `CreationDateUtc0` | Oluşturma tarihi (UTC) | `2025-10-09 11:30:25` |
| `LastAccess0` | Son erişim (local) | `2025-10-09 15:45:10` |
| `LastAccessUtc0` | Son erişim (UTC) | `2025-10-09 12:45:10` |
| `LastWrite0` | Son değiştirme (local) | `2025-10-09 14:35:00` |
| `LastWriteUtc0` | Son değiştirme (UTC) | `2025-10-09 11:35:00` |
| `ReadOnly0` | Salt okunur mu? | `True` / `False` |
| `Exists0` | Dosya var mı? | `True` / `False` |
| `Hidden0` | Gizli mi? | `True` / `False` |
| `System0` | Sistem dosyası mı? | `True` / `False` |
| `Archive0` | Archive bayrağı | `True` / `False` |
| `Compressed0` | Sıkıştırılmış mı? | `True` / `False` |
| `Temporary0` | Geçici dosya mı? | `True` / `False` |
| `Offline0` | Offline mı? | `True` / `False` |
| `Encrypted0` | Şifrelenmiş mi? | `True` / `False` |
| `DirectoryOnly0` | Klasör mü? | `True` / `False` |
| `ReparsePoint0` | Symbolic link mi? | `True` / `False` |
| `Sparse0` | Sparse file mi? | `True` / `False` |
| `Device0` | Device file mi? | `True` / `False` |
| `Normal0` | Normal file mi? | `True` / `False` |
| `DirectoryPath0` | Klasör yolu | `C:\Users\murat\Documents` |
| `DirectoryObject0` | Klasör tam yolu | `C:\Users\murat\Documents` |

**Not**: Birden fazla dosya için index artar: `FileName1`, `FileName2`, vb.

### Kullanım Örnekleri

#### Örnek 1: Basit File Info

```json
{
  "name": "File Info",
  "type": "File",
  "file_extensions": ["pdf", "docx", "xlsx", "txt", "json", "xml"],
  "screen_id": "markdown2",
  "output_format": "# Dosya Bilgileri\n\n- **Adı**: $(FileName0)\n- **Yol**: $(FullPath0)\n- **Boyut**: $(SizeBytes0) bytes\n- **Oluşturma**: $(CreationDate0)\n- **Son Değişiklik**: $(LastWrite0)\n- **Salt Okunur**: $(ReadOnly0)\n- **Gizli**: $(Hidden0)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 2: File Size Calculator

```json
{
  "name": "File Size Formatter",
  "type": "File",
  "file_extensions": ["*"],
  "screen_id": "markdown2",
  "seeder": {
    "size_kb": "$func:math.divide($(SizeBytes0), 1024)",
    "size_mb": "$func:math.divide($(size_kb), 1024)",
    "size_gb": "$func:math.divide($(size_mb), 1024)",
    "size_kb_rounded": "$func:math.round($(size_kb), 2)",
    "size_mb_rounded": "$func:math.round($(size_mb), 2)",
    "size_gb_rounded": "$func:math.round($(size_gb), 2)"
  },
  "output_format": "# Dosya Boyutu\n\n**Dosya**: $(FileName0)\n\n- Bytes: $(SizeBytes0)\n- KB: $(size_kb_rounded)\n- MB: $(size_mb_rounded)\n- GB: $(size_gb_rounded)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 3: Multi-File Summary

```json
{
  "name": "Multi-File Info",
  "type": "File",
  "file_extensions": ["*"],
  "screen_id": "markdown2",
  "output_format": "$file:C:\\Templates\\multi_file_template.md",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Template dosyası** (`C:\Templates\multi_file_template.md`):
```markdown
# Dosya Listesi (Toplam: $(_count))

## Dosya 1
- **Adı**: $(FileName0)
- **Yol**: $(FullPath0)
- **Boyut**: $(SizeBytes0) bytes
- **Değiştirilme**: $(LastWrite0)

## Dosya 2
- **Adı**: $(FileName1)
- **Yol**: $(FullPath1)
- **Boyut**: $(SizeBytes1) bytes
- **Değiştirilme**: $(LastWrite1)

## Dosya 3
- **Adı**: $(FileName2)
- **Yol**: $(FullPath2)
- **Boyut**: $(SizeBytes2) bytes
- **Değiştirilme**: $(LastWrite2)
```

#### Örnek 4: File Hash Calculator

```json
{
  "name": "File Hash",
  "type": "File",
  "file_extensions": ["exe", "dll", "zip", "msi"],
  "screen_id": "markdown2",
  "seeder": {
    "md5": "$func:hash.md5($(FullPath0))",
    "sha256": "$func:hash.sha256($(FullPath0))"
  },
  "output_format": "# File Hash\n\n**File**: $(FileName0)\n\n## Hashes\n- **MD5**: $(md5)\n- **SHA256**: $(sha256)\n\n## Info\n- **Size**: $(SizeBytes0) bytes\n- **Modified**: $(LastWrite0)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    },
    {
      "name": "copytoclipboard",
      "key": "sha256"
    }
  ]
}
```

**NOT**: `$func:hash.md5()` ve `$func:hash.sha256()` dosya içeriğini okur, bu örnekte yol yerine içerik hash'lenir.

#### Örnek 5: File Attributes Report

```json
{
  "name": "File Attributes",
  "type": "File",
  "file_extensions": ["*"],
  "screen_id": "markdown2",
  "seeder": {
    "attribute_list": "$func:{{ $(Hidden0) | string.equals(True) | string.upper }}",
    "is_hidden_icon": "$func:{{ $(Hidden0) | string.equals(True) }} ? '🔒' : '📄'",
    "is_readonly_icon": "$func:{{ $(ReadOnly0) | string.equals(True) }} ? '🔐' : '✏️'",
    "is_system_icon": "$func:{{ $(System0) | string.equals(True) }} ? '⚙️' : ''"
  },
  "output_format": "# File Attributes $(is_hidden_icon)\n\n**File**: $(FileName0)\n\n## Attributes\n- $(is_hidden_icon) Hidden: $(Hidden0)\n- $(is_readonly_icon) Read-Only: $(ReadOnly0)\n- $(is_system_icon) System: $(System0)\n- 📦 Archive: $(Archive0)\n- 🗜️ Compressed: $(Compressed0)\n- 🔐 Encrypted: $(Encrypted0)\n- 🔗 Reparse Point: $(ReparsePoint0)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **Extension filtering**: `file_extensions` ile gereksiz çağrıları önleyin
- **Multi-file support**: `_count` kontrol edin
- **Template files**: Büyük output'lar için `$file:` kullanın
- **Seeder functions**: Hesaplamalar için seeder kullanın
- **Error handling**: `NotFound` key'ini kontrol edin

#### ❌ DON'T
- **Large file reading**: File Handler dosya *içeriğini* okumaz (sadece properties)
- **Extension wildcard**: `["*"]` performans sorunu yaratabilir
- **Assume single file**: `_count` kontrol edin

---

## 5. Lookup Handler

**Dosya**: `Contextualizer.Core/LookupHandler.cs`  
**Type**: `"Lookup"`

Yerel CSV/TSV/TXT dosyalarından hızlı key-value lookup yapar. Statik veri setleri için idealdir.

### Teknik Detaylar

#### Constructor - Data Loading
```csharp
public LookupHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    _data = LoadData();
}

private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadData()
{
    var data = new Dictionary<string, Dictionary<string, string>>();
    
    // Resolve config patterns in path
    var resolvedPath = HandlerContextProcessor.ReplaceDynamicValues(
        base.HandlerConfig.Path, 
        new Dictionary<string, string>()
    );
    
    // Validate file exists
    if (!File.Exists(resolvedPath))
    {
        UserFeedback.ShowError($"Lookup file not found: {resolvedPath}");
        return ReadOnlyDictionary...;
    }
    
    using var reader = new StreamReader(resolvedPath);
    string? line;
    
    while ((line = reader.ReadLine()) != null)
    {
        // Skip empty lines and comments
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            continue;
        
        ProcessLine(line, data);
    }
    
    // Convert to readonly collections for thread safety
    return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
        data.ToDictionary(
            kvp => kvp.Key, 
            kvp => (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(kvp.Value)
        )
    );
}
```

**Özellikler**:
- **Constructor'da load**: Handler create edildiğinde data bellekte
- **Thread-safe**: `ReadOnlyDictionary` kullanımı
- **Error handling**: File I/O hatalarını yakalayıp UserFeedback gösterir
- **Comment support**: `#` ile başlayan satırlar skip edilir

#### Line Processing
```csharp
private bool ProcessLine(string line, Dictionary<string, Dictionary<string, string>> data)
{
    // Split by delimiter
    var parts = line.Split(new[] { base.HandlerConfig.Delimiter }, StringSplitOptions.None);
    
    if (parts.Length != base.HandlerConfig.ValueNames.Count)
        return false; // Invalid line
    
    // Process newline replacements
    for (int i = 0; i < parts.Length; i++)
    {
        parts[i] = parts[i].Replace("{{NEWLINE}}", Environment.NewLine);
    }
    
    // Create values dictionary
    var values = new Dictionary<string, string>();
    for (int i = 0; i < base.HandlerConfig.ValueNames.Count; i++)
    {
        values[base.HandlerConfig.ValueNames[i]] = parts[i];
    }
    
    // Add entries for each key name
    foreach (var keyName in base.HandlerConfig.KeyNames.Where(values.ContainsKey))
    {
        var keyValue = values[keyName];
        if (!string.IsNullOrEmpty(keyValue))
        {
            data[keyValue] = values;
        }
    }
    
    return true;
}
```

**Önemli**: `{{NEWLINE}}` string'i otomatik olarak `Environment.NewLine` ile değiştirilir. Bu sayede multi-line değerler desteklenir.

#### CanHandle Implementation
```csharp
protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
{
    if (clipboardContent?.Text == null)
        return false;
    
    return _data.ContainsKey(clipboardContent.Text);
}
```

**Performans**: Dictionary lookup O(1) kompleksitesinde, çok hızlı.

#### CreateContext Implementation
```csharp
protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
{
    string input = clipboardContent.Text;
    
    if (!_data.TryGetValue(input, out var lookupData))
    {
        return new Dictionary<string, string>
        {
            [ContextKey._input] = input,
            [ContextKey._error] = $"Lookup key not found: {input}"
        };
    }
    
    // Create a mutable copy of the readonly dictionary
    var context = new Dictionary<string, string>(lookupData);
    context[ContextKey._input] = input;
    
    return context;
}
```

### Kullanım Örnekleri

#### Örnek 1: Müşteri Kodu Lookup

**Lookup dosyası**: `C:\Data\customers.tsv`
```
CUST001	Ali Yılmaz	+90 532 123 45 67	ali@example.com	Premium
CUST002	Ayşe Demir	+90 533 987 65 43	ayse@example.com	Standard
CUST003	Mehmet Kaya	+90 535 111 22 33	mehmet@example.com	Gold
```

**Handler config**:
```json
{
  "name": "Customer Lookup",
  "type": "Lookup",
  "path": "C:\\Data\\customers.tsv",
  "delimiter": "\t",
  "key_names": ["CustomerCode"],
  "value_names": ["CustomerCode", "Name", "Phone", "Email", "Tier"],
  "screen_id": "markdown2",
  "output_format": "# Müşteri Bilgileri\n\n- **Kod**: $(CustomerCode)\n- **Ad**: $(Name)\n- **Telefon**: $(Phone)\n- **Email**: $(Email)\n- **Seviye**: $(Tier)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "CUST001"
Sonuç:
  CustomerCode = "CUST001"
  Name = "Ali Yılmaz"
  Phone = "+90 532 123 45 67"
  Email = "ali@example.com"
  Tier = "Premium"
```

#### Örnek 2: Multi-Key Lookup (Email veya ID)

**Lookup dosyası**: `C:\Data\employees.csv`
```
EMP001,john.doe@company.com,John Doe,Engineering,Senior Developer
EMP002,jane.smith@company.com,Jane Smith,Marketing,Manager
EMP003,bob.wilson@company.com,Bob Wilson,Sales,Director
```

**Handler config**:
```json
{
  "name": "Employee Lookup (Multi-Key)",
  "type": "Lookup",
  "path": "C:\\Data\\employees.csv",
  "delimiter": ",",
  "key_names": ["EmployeeID", "Email"],
  "value_names": ["EmployeeID", "Email", "FullName", "Department", "JobTitle"],
  "screen_id": "markdown2",
  "output_format": "# Employee Details\n\n- **ID**: $(EmployeeID)\n- **Name**: $(FullName)\n- **Email**: $(Email)\n- **Department**: $(Department)\n- **Job Title**: $(JobTitle)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test (iki farklı key ile lookup)**:
```
Kopyala: "EMP001"  -> Bulur
Kopyala: "john.doe@company.com"  -> Aynı data'yı bulur
```

**Not**: `key_names` array'inde hem `EmployeeID` hem `Email` olduğu için her iki value da key olarak kullanılabilir.

#### Örnek 3: Error Code Lookup with Multi-line Messages

**Lookup dosyası**: `C:\Data\error_codes.txt`
```
ERR_AUTH_001|Authentication Failed|User credentials are invalid.{{NEWLINE}}Please check your username and password.{{NEWLINE}}Contact IT support if issue persists.
ERR_DB_002|Database Connection Error|Unable to connect to database server.{{NEWLINE}}Check network connectivity.{{NEWLINE}}Verify database server is running.
ERR_API_003|API Rate Limit|Too many requests.{{NEWLINE}}Wait 60 seconds and try again.
```

**Handler config**:
```json
{
  "name": "Error Code Lookup",
  "type": "Lookup",
  "path": "C:\\Data\\error_codes.txt",
  "delimiter": "|",
  "key_names": ["ErrorCode"],
  "value_names": ["ErrorCode", "Title", "Description"],
  "screen_id": "markdown2",
  "output_format": "# Error: $(ErrorCode)\n\n## $(Title)\n\n$(Description)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "ERR_AUTH_001"
Sonuç (multi-line):
  ErrorCode = "ERR_AUTH_001"
  Title = "Authentication Failed"
  Description = "User credentials are invalid.\nPlease check your username and password.\nContact IT support if issue persists."
```

#### Örnek 4: Config Path with Dynamic Resolution

**Handler config**:
```json
{
  "name": "Product Lookup",
  "type": "Lookup",
  "path": "$config:data.product_catalog_path",
  "delimiter": ",",
  "key_names": ["ProductCode", "SKU"],
  "value_names": ["ProductCode", "SKU", "ProductName", "Price", "Stock"],
  "screen_id": "markdown2",
  "seeder": {
    "price_formatted": "$(Price) TL",
    "stock_status": "$func:{{ $(Stock) | math.greater_than(0) }} ? 'In Stock' : 'Out of Stock'"
  },
  "output_format": "# Product: $(ProductName)\n\n- **Code**: $(ProductCode)\n- **SKU**: $(SKU)\n- **Price**: $(price_formatted)\n- **Stock**: $(Stock) units\n- **Status**: $(stock_status)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**appsettings.json**:
```json
{
  "data": {
    "product_catalog_path": "C:\\Data\\products.csv"
  }
}
```

#### Örnek 5: Complex Lookup with Additional API Call

**Lookup dosyası**: `C:\Data\api_endpoints.txt`
```
USER_API|https://api.internal.com/v1/users|Bearer $config:api.user_token
ORDER_API|https://api.internal.com/v1/orders|Bearer $config:api.order_token
PRODUCT_API|https://api.internal.com/v1/products|Bearer $config:api.product_token
```

**Handler config**:
```json
{
  "name": "API Endpoint Lookup + Call",
  "type": "Lookup",
  "path": "C:\\Data\\api_endpoints.txt",
  "delimiter": "|",
  "key_names": ["EndpointKey"],
  "value_names": ["EndpointKey", "Url", "AuthHeader"],
  "screen_id": "markdown2",
  "user_inputs": [
    {
      "key": "resource_id",
      "title": "Resource ID",
      "message": "Enter resource ID to query:",
      "is_required": true
    }
  ],
  "seeder": {
    "full_url": "$(Url)/$(resource_id)",
    "api_response": "$func:web.get($(full_url), Authorization=$(AuthHeader))"
  },
  "output_format": "# API Response\n\n**Endpoint**: $(EndpointKey)\n**URL**: $(full_url)\n\n```json\n$(api_response)\n```",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
```
Kopyala: "USER_API"
Prompt: Enter resource ID -> "12345"
Sonuç:
  EndpointKey = "USER_API"
  Url = "https://api.internal.com/v1/users"
  AuthHeader = "Bearer <token>"
  resource_id = "12345"
  full_url = "https://api.internal.com/v1/users/12345"
  api_response = "{ ... }"  (API'den dönen JSON)
```

### Best Practices

#### ✅ DO
- **Constructor load**: Data handler create edildiğinde yüklenir (hızlı lookup)
- **ReadOnly collections**: Thread-safe veri erişimi
- **Comment support**: `#` ile dosyaya açıklama ekleyin
- **Multi-key support**: Birden fazla key ile lookup yapılabilir
- **$config: for paths**: Dosya yolunu secrets'ta saklayın
- **{{NEWLINE}}**: Multi-line değerler için kullanın
- **Error handling**: `_error` key'ini kontrol edin

#### ❌ DON'T
- **Large files**: Tüm data bellekte, çok büyük dosyalar için Database Handler kullanın
- **Dynamic data**: Lookup static data içindir, değişen data için Database/API Handler
- **Complex parsing**: CSV/TSV dışı formatlar için Custom Handler
- **Missing delimiter**: `delimiter` property zorunlu
- **Inconsistent columns**: Her satırda aynı sayıda column olmalı

---

## 6. Custom Handler

**Dosya**: `Contextualizer.Core/CustomHandler.cs`  
**Type**: `"Custom"`

Tamamen custom logic için plugin-based handler. `IContextValidator` ve `IContextProvider` plugin'leri kullanarak `CanHandle` ve `CreateContext` mantığını dışarıya taşır.

### Teknik Detaylar

#### Constructor - Plugin Caching
```csharp
public CustomHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    _actionService = ServiceLocator.Get<IActionService>();
    
    // Cache plugins at construction time for better performance
    if (!string.IsNullOrWhiteSpace(handlerConfig.Validator))
    {
        _cachedValidator = _actionService.GetContextValidator(handlerConfig.Validator);
        if (_cachedValidator == null)
        {
            UserFeedback.ShowError($"Validator '{handlerConfig.Validator}' not found");
        }
    }
    
    if (!string.IsNullOrWhiteSpace(handlerConfig.ContextProvider))
    {
        _cachedContextProvider = _actionService.GetContextProvider(handlerConfig.ContextProvider);
        if (_cachedContextProvider == null)
        {
            UserFeedback.ShowError($"Context provider '{handlerConfig.ContextProvider}' not found");
        }
    }
}
```

**Performans**: Plugin'ler constructor'da cache'lenir, her çağrıda lookup yapılmaz.

#### CanHandle - Validator Chain
```csharp
protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
{
    // Validation chain with early returns
    if (!IsValidClipboardContent(clipboardContent)) return false;
    if (!IsValidatorConfigured()) return false;
    if (!IsValidatorAvailable()) return false;
    
    return await _cachedValidator!.Validate(clipboardContent, HandlerConfig);
}

private bool IsValidClipboardContent(ClipboardContent clipboardContent)
{
    if (clipboardContent == null || !clipboardContent.Success)
    {
        UserFeedback.ShowError($"Invalid clipboard content");
        return false;
    }
    return true;
}

private bool IsValidatorConfigured()
{
    if (string.IsNullOrWhiteSpace(HandlerConfig.Validator))
    {
        UserFeedback.ShowError($"No validator configured");
        return false;
    }
    return true;
}

private bool IsValidatorAvailable()
{
    if (_cachedValidator == null)
    {
        UserFeedback.ShowError($"Validator '{HandlerConfig.Validator}' not available");
        return false;
    }
    return true;
}
```

**Early Return Pattern**: Her validasyon adımında early return ile performans ve okunabilirlik artırılır.

#### CreateContext - Provider Chain
```csharp
protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
{
    // Validation chain for context creation
    if (!IsValidClipboardContent(clipboardContent)) 
        return new Dictionary<string, string>();
    
    if (!IsContextProviderConfigured()) 
        return new Dictionary<string, string>();
    
    if (!IsContextProviderAvailable()) 
        return new Dictionary<string, string>();
    
    try
    {
        return await _cachedContextProvider!.CreateContext(clipboardContent, HandlerConfig);
    }
    catch (Exception ex)
    {
        UserFeedback.ShowError($"Error creating context: {ex.Message}");
        return new Dictionary<string, string>();
    }
}
```

### Plugin Interfaces

#### IContextValidator
```csharp
public interface IContextValidator
{
    string Name { get; }
    void Initialize();
    Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig config);
}
```

#### IContextProvider
```csharp
public interface IContextProvider
{
    string Name { get; }
    void Initialize();
    Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent, HandlerConfig config);
}
```

### Kullanım Örnekleri

#### Örnek 1: JSON Validator + Parser

**Plugin implementation** (`JsonValidator.cs`):
```csharp
public class JsonValidator : IContextValidator
{
    public string Name => "JsonValidator";
    
    public void Initialize() { }
    
    public async Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig config)
    {
        if (!clipboardContent.IsText) return false;
        
        try
        {
            JsonDocument.Parse(clipboardContent.Text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
```

**Plugin implementation** (`JsonContextProvider.cs`):
```csharp
public class JsonContextProvider : IContextProvider
{
    public string Name => "JsonContextProvider";
    
    public void Initialize() { }
    
    public async Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent, HandlerConfig config)
    {
        var context = new Dictionary<string, string>();
        
        try
        {
            var jsonDoc = JsonDocument.Parse(clipboardContent.Text);
            FlattenJson(jsonDoc.RootElement, "", context);
        }
        catch (JsonException ex)
        {
            context["_error"] = ex.Message;
        }
        
        return context;
    }
    
    private void FlattenJson(JsonElement element, string prefix, Dictionary<string, string> context)
    {
        // Same as ApiHandler's FlattenJsonToContext
        // ... (implementation)
    }
}
```

**Handler config**:
```json
{
  "name": "JSON Parser",
  "type": "Custom",
  "validator": "JsonValidator",
  "context_provider": "JsonContextProvider",
  "screen_id": "json_formatter",
  "output_format": "# JSON Data\n\n```json\n$(_input)\n```",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 2: Credit Card Validator

**Plugin implementation** (`CreditCardValidator.cs`):
```csharp
public class CreditCardValidator : IContextValidator
{
    public string Name => "CreditCardValidator";
    
    public void Initialize() { }
    
    public async Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig config)
    {
        if (!clipboardContent.IsText) return false;
        
        var cardNumber = clipboardContent.Text.Replace(" ", "").Replace("-", "");
        
        // Luhn algorithm
        if (cardNumber.Length < 13 || cardNumber.Length > 19) return false;
        if (!cardNumber.All(char.IsDigit)) return false;
        
        return LuhnCheck(cardNumber);
    }
    
    private bool LuhnCheck(string cardNumber)
    {
        int sum = 0;
        bool alternate = false;
        
        for (int i = cardNumber.Length - 1; i >= 0; i--)
        {
            int n = int.Parse(cardNumber[i].ToString());
            
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            
            sum += n;
            alternate = !alternate;
        }
        
        return sum % 10 == 0;
    }
}
```

**Plugin implementation** (`CreditCardContextProvider.cs`):
```csharp
public class CreditCardContextProvider : IContextProvider
{
    public string Name => "CreditCardContextProvider";
    
    public void Initialize() { }
    
    public async Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent, HandlerConfig config)
    {
        var context = new Dictionary<string, string>();
        var cardNumber = clipboardContent.Text.Replace(" ", "").Replace("-", "");
        
        context["CardNumber"] = cardNumber;
        context["CardNumberMasked"] = MaskCardNumber(cardNumber);
        context["CardType"] = GetCardType(cardNumber);
        context["IsValid"] = "✅ Valid";
        
        return context;
    }
    
    private string MaskCardNumber(string cardNumber)
    {
        if (cardNumber.Length <= 4) return cardNumber;
        var lastFour = cardNumber.Substring(cardNumber.Length - 4);
        return $"****-****-****-{lastFour}";
    }
    
    private string GetCardType(string cardNumber)
    {
        if (cardNumber.StartsWith("4")) return "Visa";
        if (cardNumber.StartsWith("5")) return "MasterCard";
        if (cardNumber.StartsWith("3")) return "American Express";
        return "Unknown";
    }
}
```

**Handler config**:
```json
{
  "name": "Credit Card Validator",
  "type": "Custom",
  "validator": "CreditCardValidator",
  "context_provider": "CreditCardContextProvider",
  "screen_id": "markdown2",
  "output_format": "# Credit Card Info\n\n- **Card Number**: $(CardNumberMasked)\n- **Type**: $(CardType)\n- **Status**: $(IsValid)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 3: Complex Business Rule Validator

**Senaryo**: Müşteri ID'si belirli bir formatta olmalı ve veritabanında bulunmalı.

**Plugin implementation** (`CustomerIdValidator.cs`):
```csharp
public class CustomerIdValidator : IContextValidator
{
    public string Name => "CustomerIdValidator";
    private IDbConnection? _connection;
    
    public void Initialize()
    {
        // Connection setup can be done here
    }
    
    public async Task<bool> Validate(ClipboardContent clipboardContent, HandlerConfig config)
    {
        if (!clipboardContent.IsText) return false;
        
        var customerId = clipboardContent.Text.Trim();
        
        // Format validation: CUST_XXXXX
        if (!Regex.IsMatch(customerId, @"^CUST_\d{5}$"))
            return false;
        
        // Database validation
        var connectionString = config.ConnectionString;
        if (string.IsNullOrEmpty(connectionString)) return false;
        
        using var connection = new SqlConnection(connectionString);
        var exists = await connection.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(1) FROM Customers WHERE CustomerID = @id",
            new { id = customerId }
        );
        
        return exists > 0;
    }
}
```

**Handler config**:
```json
{
  "name": "Customer ID Validator",
  "type": "Custom",
  "validator": "CustomerIdValidator",
  "context_provider": "CustomerContextProvider",
  "connectionString": "$config:database.customer_db",
  "screen_id": "markdown2",
  "output_format": "# Customer: $(Name)\n\n- **ID**: $(CustomerID)\n- **Email**: $(Email)\n- **Status**: $(Status)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **Plugin caching**: Constructor'da plugin'leri cache'leyin
- **Early return validation**: Validation chain ile performans artırın
- **Error handling**: Try-catch ile plugin hatalarını yakalayın
- **UserFeedback**: Plugin hatalarını kullanıcıya bildirin
- **Reusable plugins**: Generic plugin'ler yazın
- **Initialize method**: Plugin initialization için `Initialize()` kullanın

#### ❌ DON'T
- **Heavy construction**: Constructor'da ağır işlemler yapmayın
- **Missing validation**: Null checks yapın
- **Silent failures**: Hataları loglayın
- **Tight coupling**: Plugin'ler handler'dan bağımsız olmalı
- **No error context**: Hangi plugin'de hata oluştuğunu belirtin

---

## 7. Manual Handler

**Dosya**: `Contextualizer.Core/ManualHandler.cs`  
**Type**: `"Manual"`

Clipboard monitoring olmadan, sadece manual trigger ile çalışan handler. UI'dan "Execute" butonuna basıldığında çalıştırılır.

### Teknik Detaylar

#### Simple Implementation
```csharp
public class ManualHandler : Dispatch, IHandler, ITriggerableHandler
{
    public static string TypeName => "Manual";
    
    public ManualHandler(HandlerConfig handlerConfig) : base(handlerConfig)
    {
    }
    
    protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
    {
        return true; // Always true - manual trigger
    }
    
    protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
    {
        return new Dictionary<string, string>(); // Empty context - relies on seeder
    }
    
    protected override List<ConfigAction> GetActions()
    {
        return base.HandlerConfig.Actions;
    }
}
```

**Özellikler**:
- **Always CanHandle**: `CanHandle()` her zaman `true` döner
- **Empty Context**: `CreateContext()` boş dictionary döner
- **Seeder/ConstantSeeder**: Initial context için `seeder` veya `constant_seeder` kullanılır
- **ITriggerableHandler**: UI'dan manuel çalıştırılabilir

#### ITriggerableHandler Interface
```csharp
public interface ITriggerableHandler
{
    // No methods - marker interface for UI to identify manually triggerable handlers
}
```

### Context Initialization

Manual Handler boş context döndüğü için, initial data için şu yöntemler kullanılır:

#### 1. Constant Seeder
```json
{
  "constant_seeder": {
    "message": "Hello World",
    "timestamp": "2025-10-09"
  }
}
```

#### 2. Dynamic Seeder
```json
{
  "seeder": {
    "current_time": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "user": "$func:username()",
    "guid": "$func:guid()"
  }
}
```

#### 3. User Inputs
```json
{
  "user_inputs": [
    {
      "key": "task_name",
      "title": "Task Name",
      "message": "Enter task name:",
      "is_required": true
    }
  ]
}
```

### Kullanım Örnekleri

#### Örnek 1: Daily Report Generator

```json
{
  "name": "Generate Daily Report",
  "type": "Manual",
  "description": "Generates a daily report manually",
  "screen_id": "markdown2",
  "seeder": {
    "today": "$func:today().format(yyyy-MM-dd)",
    "report_id": "$func:guid()",
    "generated_by": "$func:username()",
    "computer": "$func:computername()"
  },
  "user_inputs": [
    {
      "key": "report_type",
      "title": "Report Type",
      "message": "Select report type:",
      "is_required": true,
      "is_selection_list": true,
      "selection_items": [
        {"value": "sales", "display": "Sales Report"},
        {"value": "inventory", "display": "Inventory Report"},
        {"value": "customer", "display": "Customer Report"}
      ]
    }
  ],
  "output_format": "# Daily Report\n\n- **Date**: $(today)\n- **Type**: $(report_type)\n- **Report ID**: $(report_id)\n- **Generated By**: $(generated_by)\n- **Computer**: $(computer)\n\n---\n\n*Report content would be generated here*",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "title": "Daily Report - $(today)"
    }
  ]
}
```

**Kullanım**:
1. UI'da "Generate Daily Report" handler'ını bul
2. "Execute" butonuna bas
3. Prompt: "Select report type" -> Sales Report seç
4. Rapor otomatik oluşturulur ve gösterilir

#### Örnek 2: Quick Note Creator

```json
{
  "name": "Quick Note",
  "type": "Manual",
  "description": "Create a quick note with timestamp",
  "screen_id": "markdown2",
  "seeder": {
    "timestamp": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "user": "$func:username()"
  },
  "user_inputs": [
    {
      "key": "note_title",
      "title": "Note Title",
      "message": "Enter note title:",
      "is_required": true
    },
    {
      "key": "note_content",
      "title": "Note Content",
      "message": "Enter note content:",
      "is_required": true,
      "is_multiline": true
    }
  ],
  "output_format": "# $(note_title)\n\n**Created**: $(timestamp)  \n**Author**: $(user)\n\n---\n\n$(note_content)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "title": "Note: $(note_title)"
    },
    {
      "name": "copytoclipboard",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 3: System Info Viewer

```json
{
  "name": "System Information",
  "type": "Manual",
  "description": "Display current system information",
  "screen_id": "markdown2",
  "seeder": {
    "timestamp": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "username": "$func:username()",
    "computername": "$func:computername()",
    "local_ip": "$func:ip.local()",
    "public_ip": "$func:ip.public()",
    "temp_path": "$func:env(TEMP)",
    "user_profile": "$func:env(USERPROFILE)"
  },
  "output_format": "# System Information\n\n**Query Time**: $(timestamp)\n\n## User\n- **Username**: $(username)\n- **Computer**: $(computername)\n- **User Profile**: $(user_profile)\n\n## Network\n- **Local IP**: $(local_ip)\n- **Public IP**: $(public_ip)\n\n## Paths\n- **Temp**: $(temp_path)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "title": "System Info"
    }
  ]
}
```

#### Örnek 4: Database Backup Trigger

```json
{
  "name": "Trigger Database Backup",
  "type": "Manual",
  "description": "Manually trigger a database backup",
  "seeder": {
    "backup_id": "$func:guid()",
    "backup_time": "$func:now().format(yyyy-MM-dd_HH-mm-ss)",
    "initiated_by": "$func:username()"
  },
  "user_inputs": [
    {
      "key": "database_name",
      "title": "Database Name",
      "message": "Select database to backup:",
      "is_required": true,
      "is_selection_list": true,
      "selection_items": [
        {"value": "CustomerDB", "display": "Customer Database"},
        {"value": "OrderDB", "display": "Order Database"},
        {"value": "InventoryDB", "display": "Inventory Database"}
      ]
    }
  ],
  "requires_confirmation": true,
  "confirmation_message": "Are you sure you want to backup $(database_name)?",
  "actions": [
    {
      "name": "custom_backup_action",
      "seeder": {
        "backup_path": "C:\\Backups\\$(database_name)_$(backup_time).bak"
      }
    },
    {
      "name": "show_notification",
      "message": "Backup initiated for $(database_name)\nBackup ID: $(backup_id)"
    }
  ]
}
```

#### Örnek 5: Manual Handler with API Call

```json
{
  "name": "Check Server Status",
  "type": "Manual",
  "description": "Manually check server status",
  "screen_id": "markdown2",
  "seeder": {
    "check_time": "$func:now().format(yyyy-MM-dd HH:mm:ss)",
    "server_health": "$func:web.get(https://api.internal.com/health)",
    "server_version": "$func:web.get(https://api.internal.com/version)"
  },
  "output_format": "# Server Status Check\n\n**Check Time**: $(check_time)\n\n## Health\n```json\n$(server_health)\n```\n\n## Version\n```json\n$(server_version)\n```",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "title": "Server Status"
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **Clear description**: Handler'ın ne yaptığını açıklayın
- **User inputs**: Kullanıcıdan gerekli bilgileri alın
- **Seeder functions**: Dynamic değerler için seeder kullanın
- **Confirmation**: Kritik işlemler için `requires_confirmation: true`
- **Descriptive titles**: Action title'larında context değerleri kullanın
- **ITriggerableHandler**: Interface'i implement edin

#### ❌ DON'T
- **Clipboard dependency**: Manual handler clipboard'a bağımlı olmamalı
- **CanHandle logic**: `CanHandleAsync()` her zaman `true` dönmeli
- **Heavy CreateContext**: `CreateContextAsync()` boş olmalı, seeder kullanın
- **Silent execution**: Kullanıcıya ne olduğunu bildirin (notification, window)

---

## 8. Synthetic Handler

**Dosya**: `Contextualizer.Core/SyntheticHandler.cs`  
**Type**: `"Synthetic"`

Meta-handler: Başka bir handler'ı wrap edebilir veya synthetic (yapay) clipboard content oluşturabilir. Cron job'lar ve test senaryoları için kullanılır.

### Teknik Detaylar

#### Constructor - Actual Handler Creation
```csharp
public SyntheticHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    if (!string.IsNullOrEmpty(handlerConfig.ActualType))
    {
        try
        {
            // Create a copy of the config with the actual type
            handlerConfig.Type = handlerConfig.ActualType;
            _actualHandler = HandlerFactory.Create(handlerConfig);
            
            if (_actualHandler == null)
            {
                UserFeedback.ShowWarning($"Failed to create actual handler of type '{handlerConfig.ActualType}'");
            }
        }
        catch (Exception ex)
        {
            UserFeedback.ShowError($"Error creating actual handler - {ex.Message}");
        }
    }
}
```

**Özellikler**:
- **ActualType**: Wrap edilecek handler type'ı (örn: "Regex", "Database")
- **_actualHandler**: Embed edilmiş handler instance
- **ReferenceHandler**: Mevcut bir handler'a isimle referans

#### Execute - Three Scenarios
```csharp
async Task<bool> IHandler.Execute(ClipboardContent clipboardContent)
{
    // Scenario 1: ActualType - Use embedded _actualHandler
    if (_actualHandler != null)
    {
        UserFeedback.ShowActivity(LogType.Info, $"Executing actual handler '{_actualHandler.HandlerConfig.Name}'");
        return await _actualHandler.Execute(clipboardContent);
    }
    
    // Scenario 2: ReferenceHandler - Find and execute existing handler
    if (!string.IsNullOrEmpty(HandlerConfig.ReferenceHandler))
    {
        var handlerManager = ServiceLocator.Get<HandlerManager>();
        var referenceHandler = handlerManager.GetHandlerByName(HandlerConfig.ReferenceHandler);
        
        if (referenceHandler != null)
        {
            UserFeedback.ShowActivity(LogType.Info, $"Executing reference handler '{referenceHandler.HandlerConfig.Name}'");
            return await referenceHandler.Execute(clipboardContent);
        }
        else
        {
            UserFeedback.ShowWarning($"Reference handler not found: {HandlerConfig.ReferenceHandler}");
            return false;
        }
    }
    
    // Scenario 3: Fallback to base Dispatch execution
    UserFeedback.ShowActivity(LogType.Info, $"No ActualType or ReferenceHandler, using base execution");
    return await base.Execute(clipboardContent);
}
```

#### Create Synthetic Content
```csharp
public ClipboardContent CreateSyntheticContent(UserInputRequest? userInputRequest)
{
    if (userInputRequest is null)
    {
        UserFeedback.ShowError("User input request was null or invalid");
        return new ClipboardContent { Success = false };
    }
    
    var userInput = ServiceLocator.Get<IUserInteractionService>().GetUserInput(userInputRequest);
    if (string.IsNullOrEmpty(userInput))
    {
        UserFeedback.ShowError("User input was null or invalid");
        return new ClipboardContent { Success = false };
    }
    
    // File picker scenario
    if (userInputRequest.IsFilePicker)
    {
        return new ClipboardContent
        {
            Success = true,
            IsFile = true,
            Files = new[] { userInput }
        };
    }
    
    // Text input scenario
    return new ClipboardContent
    {
        Success = true,
        IsText = true,
        Text = userInput
    };
}
```

#### Dispose Pattern
```csharp
public void Dispose()
{
    Dispose(true);
    GC.SuppressFinalize(this);
}

protected virtual void Dispose(bool disposing)
{
    if (!_disposed && disposing)
    {
        if (_actualHandler is IDisposable disposableHandler)
        {
            disposableHandler.Dispose();
        }
        _disposed = true;
    }
}
```

### Kullanım Örnekleri

#### Örnek 1: Synthetic Regex Handler (ActualType)

```json
{
  "name": "Synthetic Order Lookup",
  "type": "Synthetic",
  "actual_type": "Regex",
  "regex": "ORDER\\d{5}",
  "groups": ["order_id"],
  "synthetic_input": {
    "key": "order_input",
    "title": "Order Lookup",
    "message": "Enter order number:",
    "is_required": true,
    "validation_regex": "ORDER\\d{5}"
  },
  "screen_id": "markdown2",
  "output_format": "# Order: $(_match)\n\n**Order ID**: $(order_id)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

**Test**:
1. UI'dan "Execute" butonuna bas
2. Prompt: "Enter order number" -> "ORDER12345" gir
3. Synthetic ClipboardContent oluşturulur: `{ Text: "ORDER12345", IsText: true }`
4. Embedded Regex handler çalıştırılır
5. Output gösterilir

#### Örnek 2: Reference Handler

```json
{
  "name": "Synthetic IBAN Lookup Trigger",
  "type": "Synthetic",
  "reference_handler": "IBAN Müşteri Sorgu",
  "synthetic_input": {
    "key": "iban_input",
    "title": "IBAN Lookup",
    "message": "Enter IBAN:",
    "is_required": true,
    "validation_regex": "TR\\d{24}"
  }
}
```

**Not**: "IBAN Müşteri Sorgu" adında bir Database Handler olmalı. Synthetic handler bu handler'ı bulup çalıştırır.

#### Örnek 3: File Handler with Synthetic File Picker

```json
{
  "name": "Synthetic File Info",
  "type": "Synthetic",
  "actual_type": "File",
  "file_extensions": ["pdf", "docx", "xlsx"],
  "synthetic_input": {
    "key": "file_path",
    "title": "Select File",
    "message": "Choose a file to analyze:",
    "is_required": true,
    "is_file_picker": true
  },
  "screen_id": "markdown2",
  "output_format": "# File: $(FileName0)\n\n- **Size**: $(SizeBytes0) bytes\n- **Created**: $(CreationDate0)\n- **Modified**: $(LastWrite0)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 4: Database Handler with Pre-filled Synthetic Input

```json
{
  "name": "Synthetic Customer Search",
  "type": "Synthetic",
  "actual_type": "Database",
  "connectionString": "$config:database.customer_db",
  "connector": "mssql",
  "regex": "CUST\\d{5}",
  "query": "SELECT * FROM Customers WHERE CustomerID = @p_match",
  "synthetic_input": {
    "key": "customer_id",
    "title": "Customer Search",
    "message": "Enter customer ID (CUSTXXXXX):",
    "is_required": true,
    "validation_regex": "CUST\\d{5}",
    "default_value": "CUST00001"
  },
  "screen_id": "markdown2",
  "output_format": "$(_formatted_output)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output"
    }
  ]
}
```

#### Örnek 5: Chained Synthetic + Manual Actions

```json
{
  "name": "Complex Workflow",
  "type": "Synthetic",
  "actual_type": "Api",
  "url": "https://api.example.com/users/$(user_id)",
  "method": "GET",
  "synthetic_input": {
    "key": "user_id",
    "title": "User ID",
    "message": "Enter user ID to fetch:",
    "is_required": true
  },
  "screen_id": "json_formatter",
  "actions": [
    {
      "name": "show_window",
      "key": "RawResponse",
      "title": "User API Response"
    },
    {
      "name": "copytoclipboard",
      "key": "user.email",
      "conditions": {
        "left": "$(StatusCode)",
        "operator": "equals",
        "right": "200"
      }
    },
    {
      "name": "show_notification",
      "message": "User email copied: $(user.email)",
      "conditions": {
        "left": "$(StatusCode)",
        "operator": "equals",
        "right": "200"
      }
    }
  ]
}
```

### Best Practices

#### ✅ DO
- **ActualType**: Embed edilecek handler type'ını belirtin
- **ReferenceHandler**: Mevcut handler'a isim ile referans verin
- **synthetic_input**: Kullanıcıdan alınacak input'u tanımlayın
- **Validation**: `validation_regex` ile input'u validate edin
- **Dispose**: IDisposable implement edin, embedded handler'ı dispose edin
- **Error handling**: Synthetic content oluşturma hatalarını yakalayın

#### ❌ DON'T
- **Both ActualType and ReferenceHandler**: İkisini birden kullanmayın (ActualType öncelikli)
- **Missing input**: `synthetic_input` olmadan kullanıcı nasıl veri girecek?
- **Memory leaks**: Dispose pattern'i implement etmeyi unutmayın
- **Invalid references**: ReferenceHandler var mı kontrol edin

---

## 9. Cron Handler

**Dosya**: `Contextualizer.Core/CronHandler.cs`  
**Type**: `"Cron"`

Zamanlanmış (scheduled) görevler için handler. Cron expression'ı ile belirli zamanlarda otomatik çalışır. `SyntheticHandler`'ı extend eder.

### Teknik Detaylar

#### Constructor - Auto Registration
```csharp
public CronHandler(HandlerConfig handlerConfig) : base(handlerConfig)
{
    UserFeedback.ShowActivity(LogType.Info, $"CronHandler created for: {HandlerConfig.Name}");
    
    // Register this handler with the cron scheduler
    if (!string.IsNullOrEmpty(HandlerConfig.CronExpression))
    {
        UserFeedback.ShowActivity(LogType.Info, 
            $"Registering cron job for: {HandlerConfig.Name} with expression: {HandlerConfig.CronExpression}");
        RegisterCronJob();
    }
    else
    {
        UserFeedback.ShowWarning($"CronHandler {HandlerConfig.Name} has no cron expression");
    }
}
```

**Önemli**: Handler create edildiğinde otomatik olarak cron job register edilir.

#### Cron Job Registration
```csharp
private void RegisterCronJob()
{
    try
    {
        var cronService = ServiceLocator.Get<ICronService>();
        var jobId = !string.IsNullOrEmpty(HandlerConfig.CronJobId) 
            ? HandlerConfig.CronJobId 
            : $"cron_{HandlerConfig.Name.Replace(" ", "_").ToLower()}";
        
        // Create a modified config for the actual handler type
        var actualConfig = CreateActualHandlerConfig();
        
        var success = cronService.ScheduleJob(
            jobId, 
            HandlerConfig.CronExpression!, 
            actualConfig, 
            HandlerConfig.CronTimezone
        );
        
        if (success)
        {
            UserFeedback.ShowSuccess($"Cron job scheduled: {HandlerConfig.Name} (ID: {jobId})");
        }
        else
        {
            UserFeedback.ShowError($"Failed to schedule cron job: {HandlerConfig.Name}");
        }
    }
    catch (Exception ex)
    {
        UserFeedback.ShowError($"Error registering cron job: {ex.Message}");
    }
}
```

#### Create Actual Handler Config
```csharp
private HandlerConfig CreateActualHandlerConfig()
{
    // Create a new config based on the actual handler type
    var actualConfig = new HandlerConfig
    {
        Name = HandlerConfig.Name,
        Type = HandlerConfig.ActualType ?? "synthetic",
        Description = HandlerConfig.Description,
        Title = HandlerConfig.Title,
        ScreenId = HandlerConfig.ScreenId,
        
        // Copy all properties for different handler types
        Regex = HandlerConfig.Regex,
        Groups = HandlerConfig.Groups,
        ConnectionString = HandlerConfig.ConnectionString,
        Query = HandlerConfig.Query,
        Connector = HandlerConfig.Connector,
        Url = HandlerConfig.Url,
        Method = HandlerConfig.Method,
        Headers = HandlerConfig.Headers,
        RequestBody = HandlerConfig.RequestBody,
        // ... (tüm properties kopyalanır)
    };
    
    return actualConfig;
}
```

**Önemli**: Embedded handler'ın tüm config'i kopyalanır, böylece cron job çalıştığında handler'ın full config'ine sahip olur.

#### Manual Execution
```csharp
public void ExecuteNow()
{
    try
    {
        var cronService = ServiceLocator.Get<ICronService>();
        var jobId = !string.IsNullOrEmpty(HandlerConfig.CronJobId) 
            ? HandlerConfig.CronJobId 
            : $"cron_{HandlerConfig.Name.Replace(" ", "_").ToLower()}";
        
        var success = cronService.TriggerJob(jobId);
        if (success)
        {
            UserFeedback.ShowActivity(LogType.Info, $"Manually triggered cron job: {HandlerConfig.Name}");
        }
        else
        {
            UserFeedback.ShowWarning($"Cron job not found: {HandlerConfig.Name}");
        }
    }
    catch (Exception ex)
    {
        UserFeedback.ShowError($"Error manually executing cron job: {ex.Message}");
    }
}
```

#### Enable/Disable
```csharp
public void SetEnabled(bool enabled)
{
    try
    {
        var cronService = ServiceLocator.Get<ICronService>();
        var jobId = !string.IsNullOrEmpty(HandlerConfig.CronJobId) 
            ? HandlerConfig.CronJobId 
            : $"cron_{HandlerConfig.Name.Replace(" ", "_").ToLower()}";
        
        var success = cronService.SetJobEnabled(jobId, enabled);
        if (success)
        {
            UserFeedback.ShowActivity(LogType.Info, 
                $"Cron job {(enabled ? "enabled" : "disabled")}: {HandlerConfig.Name}");
        }
        else
        {
            UserFeedback.ShowWarning($"Cron job not found: {HandlerConfig.Name}");
        }
    }
    catch (Exception ex)
    {
        UserFeedback.ShowError($"Error setting cron job enabled state: {ex.Message}");
    }
}
```

### Cron Expression Format

Quartz.NET cron expression formatı kullanılır:

```
┌─────────── second (0 - 59)
│ ┌───────── minute (0 - 59)
│ │ ┌─────── hour (0 - 23)
│ │ │ ┌───── day of month (1 - 31)
│ │ │ │ ┌─── month (1 - 12) or JAN-DEC
│ │ │ │ │ ┌─ day of week (0 - 7) or SUN-SAT (0 or 7 is Sunday)
│ │ │ │ │ │
* * * * * *
```

**Özel karakterler**:
- `*` : Any value
- `?` : No specific value (day of month / day of week için)
- `-` : Range (örn: `10-12`)
- `,` : List (örn: `MON,WED,FRI`)
- `/` : Increment (örn: `0/5` = her 5 dakikada bir)

### Kullanım Örnekleri

#### Örnek 1: Daily Database Backup (Her gün saat 02:00)

```json
{
  "name": "Daily DB Backup",
  "type": "Cron",
  "cron_expression": "0 0 2 * * ?",
  "cron_timezone": "Turkey Standard Time",
  "cron_job_id": "daily_backup",
  "actual_type": "Manual",
  "description": "Runs database backup every day at 2:00 AM",
  "seeder": {
    "backup_time": "$func:now().format(yyyy-MM-dd_HH-mm-ss)",
    "backup_id": "$func:guid()"
  },
  "actions": [
    {
      "name": "custom_backup_action",
      "seeder": {
        "backup_path": "C:\\Backups\\DB_$(backup_time).bak"
      }
    },
    {
      "name": "show_notification",
      "message": "Database backup completed\nBackup ID: $(backup_id)"
    }
  ]
}
```

**Cron**: `0 0 2 * * ?` = Her gün saat 02:00:00

#### Örnek 2: Hourly API Health Check

```json
{
  "name": "Hourly Health Check",
  "type": "Cron",
  "cron_expression": "0 0 * * * ?",
  "actual_type": "Api",
  "url": "https://api.internal.com/health",
  "method": "GET",
  "screen_id": "json_formatter",
  "seeder": {
    "check_time": "$func:now().format(yyyy-MM-dd HH:mm:ss)"
  },
  "actions": [
    {
      "name": "show_notification",
      "message": "Health check completed at $(check_time)",
      "conditions": {
        "left": "$(StatusCode)",
        "operator": "equals",
        "right": "200"
      }
    },
    {
      "name": "show_notification",
      "message": "⚠️ Health check FAILED at $(check_time)\nStatus: $(StatusCode)",
      "conditions": {
        "left": "$(StatusCode)",
        "operator": "not_equals",
        "right": "200"
      }
    }
  ]
}
```

**Cron**: `0 0 * * * ?` = Her saat başı (XX:00:00)

#### Örnek 3: Weekly Report (Her Pazartesi 09:00)

```json
{
  "name": "Weekly Sales Report",
  "type": "Cron",
  "cron_expression": "0 0 9 ? * MON",
  "cron_timezone": "Turkey Standard Time",
  "actual_type": "Database",
  "connectionString": "$config:database.sales_db",
  "connector": "mssql",
  "query": "SELECT ProductName, SUM(Quantity) as TotalSold, SUM(Amount) as TotalRevenue FROM Sales WHERE SaleDate >= DATEADD(day, -7, GETDATE()) GROUP BY ProductName ORDER BY TotalRevenue DESC",
  "screen_id": "markdown2",
  "seeder": {
    "report_date": "$func:today().format(yyyy-MM-dd)",
    "week_start": "$func:today().add(days,-7).format(yyyy-MM-dd)"
  },
  "output_format": "# Weekly Sales Report\n\n**Period**: $(week_start) to $(report_date)\n\n$(_formatted_output)",
  "actions": [
    {
      "name": "show_window",
      "key": "_formatted_output",
      "title": "Weekly Sales - $(report_date)"
    },
    {
      "name": "show_notification",
      "message": "Weekly sales report generated for $(week_start) to $(report_date)"
    }
  ]
}
```

**Cron**: `0 0 9 ? * MON` = Her Pazartesi 09:00:00

#### Örnek 4: Every 5 Minutes Monitoring

```json
{
  "name": "5-Minute Server Monitor",
  "type": "Cron",
  "cron_expression": "0 0/5 * * * ?",
  "actual_type": "Api",
  "url": "$config:monitoring.server_url",
  "method": "GET",
  "headers": {
    "Authorization": "Bearer $config:monitoring.token"
  },
  "seeder": {
    "check_time": "$func:now().format(HH:mm:ss)"
  },
  "actions": [
    {
      "name": "show_notification",
      "message": "Server check OK at $(check_time)",
      "conditions": {
        "left": "$(StatusCode)",
        "operator": "equals",
        "right": "200"
      }
    }
  ]
}
```

**Cron**: `0 0/5 * * * ?` = Her 5 dakikada bir (0, 5, 10, 15, ...)

#### Örnek 5: Complex Schedule - Business Hours Only

```json
{
  "name": "Business Hours Data Sync",
  "type": "Cron",
  "cron_expression": "0 0/30 9-17 ? * MON-FRI",
  "cron_timezone": "Turkey Standard Time",
  "actual_type": "Database",
  "connectionString": "$config:database.sync_db",
  "connector": "mssql",
  "query": "EXEC SyncProcedure",
  "description": "Syncs data every 30 minutes during business hours (9AM-5PM, Mon-Fri)",
  "actions": [
    {
      "name": "show_notification",
      "message": "Data sync completed at $func:now().format(HH:mm)"
    }
  ]
}
```

**Cron**: `0 0/30 9-17 ? * MON-FRI` = Hafta içi (Pazartesi-Cuma), 09:00-17:00 arası, her 30 dakikada bir

### Cron Expression Örnekleri

| Expression | Açıklama |
|------------|----------|
| `0 0 12 * * ?` | Her gün öğlen 12:00 |
| `0 15 10 * * ?` | Her gün 10:15 |
| `0 0/5 * * * ?` | Her 5 dakikada bir |
| `0 0 0 1 * ?` | Her ayın 1. günü gece yarısı |
| `0 0 8 ? * MON-FRI` | Hafta içi her gün 08:00 |
| `0 30 23 ? * SAT,SUN` | Hafta sonu 23:30 |
| `0 0 0,12 * * ?` | Her gün gece yarısı ve öğlen |
| `0 0 9-17 * * ?` | Her gün 09:00-17:00 arası her saat başı |

### Timezone Support

**appsettings.json**:
```json
{
  "cron": {
    "default_timezone": "Turkey Standard Time"
  }
}
```

**Handler config'de override**:
```json
{
  "cron_timezone": "UTC"
}
```

**Desteklenen timezone'lar**: `TimeZoneInfo.GetSystemTimeZones()` ile listelenir.

### Best Practices

#### ✅ DO
- **Specific cron_job_id**: Her cron handler için unique ID verin
- **Timezone belirtin**: Belirsizlik olmaması için timezone set edin
- **Error notifications**: Cron job başarısız olursa notification gönderin
- **Logging**: Cron job'ların çalışma zamanlarını loglayın
- **Manual testing**: `ExecuteNow()` ile test edin
- **Off-hours scheduling**: Yoğun işleri gece saatlerine planlayın
- **Idempotent actions**: Cron job birden fazla çalışsa bile safe olmalı

#### ❌ DON'T
- **Too frequent**: `0/1 * * * * ?` (her saniye) gibi çok sık schedule'lar
- **Overlapping jobs**: Bir job bitmeden diğeri başlamasın
- **Missing timezone**: Timezone belirsizliği sorun yaratır
- **Heavy UI operations**: Cron job'lar background'da çalışır, UI operasyonları minimal olmalı
- **No error handling**: Cron job başarısız olursa ne olacak?

---

## Best Practices (Genel)

### Handler Geliştirme İlkeleri

#### 1. Performance
- **Regex compile**: Constructor'da compile edin
- **Connection pooling**: Database ve API handler'lar için
- **Async/await**: Tüm I/O işlemleri async
- **Early return**: Gereksiz işlemleri skip edin
- **Resource disposal**: IDisposable implement edin

#### 2. Security
- **Parameterized queries**: SQL injection'a karşı
- **Input validation**: Kullanıcı girişlerini validate edin
- **Secrets management**: $config: ile secret'ları saklayın
- **Timeout**: ReDoS ve long-running queries için
- **Least privilege**: Database kullanıcısı readonly olmalı

#### 3. Maintainability
- **Clear naming**: Handler, key ve action adları açıklayıcı olmalı
- **Comments**: Complex regex ve query'ler için comment
- **Error handling**: Tüm exception'ları yakalayıp loglayın
- **UserFeedback**: Kullanıcıya ne olduğunu bildirin
- **Modular design**: Büyük handler'ları parçalayın

#### 4. User Experience
- **Descriptive messages**: Kullanıcı ne yapacağını bilmeli
- **Confirmation**: Kritik işlemler için onay isteyin
- **Progress feedback**: Uzun işlemler için progress gösterin
- **Consistent UI**: Tüm handler'larda aynı UI pattern'i kullanın
- **Helpful defaults**: User input'larda default değerler verin

#### 5. Testing
- **Unit tests**: Her handler için test yazın
- **Manual testing**: UI'dan test edin
- **Edge cases**: Null, empty, invalid input'ları test edin
- **Performance tests**: Büyük data setleri ile test edin
- **Error scenarios**: Exception durumlarını test edin

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

