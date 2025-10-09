# Contextualizer - Function System

## 📋 İçindekiler
- [Genel Bakış](#genel-bakış)
- [Function Syntax](#function-syntax)
- [Function Kategorileri](#function-kategorileri)
- [Date/Time Functions](#datetime-functions)
- [String Functions](#string-functions)
- [Math Functions](#math-functions)
- [Hash Functions](#hash-functions)
- [URL Functions](#url-functions)
- [Web Functions](#web-functions)
- [IP Functions](#ip-functions)
- [JSON Functions](#json-functions)
- [Array Functions](#array-functions)
- [Utility Functions](#utility-functions)
- [İleri Seviye Kullanım](#ileri-seviye-kullanım)

---

## Genel Bakış

**Dosya**: `Contextualizer.Core/FunctionProcessor.cs` (1817 satır!)

Function System, Contextualizer'ın en güçlü özelliklerinden biridir. Handler ve action konfigürasyonlarında dinamik değerler oluşturmanıza olanak tanır.

### Temel Özellikler

- **50+ Built-in Function**: Tarih, string, math, hash, URL, web, IP, JSON, array işlemleri
- **3 Syntax Türü**: Regular, Pipeline, Method Chaining
- **Context Entegrasyonu**: Function parametrelerinde `$(key)` placeholder kullanımı
- **Nested Support**: Function içinde function çağırma desteği
- **Type-Safe Parsing**: Akıllı parametre ayrıştırma (quotes, parentheses, commas)
- **Error Handling**: Detaylı hata mesajları ve logging

### Kullanım Alanları

```json
{
  "seeder": {
    "today": "$func:today",
    "formatted_date": "$func:today.format(yyyy-MM-dd)",
    "guid": "$func:guid",
    "user": "$func:username",
    "hash": "$func:hash.md5($(clipboard_text))"
  }
}
```

---

## Function Syntax

### 1. Regular Functions

En basit kullanım şekli: `$func:functionName(param1,param2)`

```json
{
  "today": "$func:today",
  "random_number": "$func:random(1,100)",
  "encoded": "$func:url.encode(hello world)",
  "hash": "$func:hash.sha256(mypassword)"
}
```

**Özellikleri**:
- Tek bir function çağrısı
- Parametre opsiyonel
- Context placeholder desteği: `$func:hash.md5($(username))`

### 2. Method Chaining

Method chaining ile zincirleme çağrılar: `$func:today.add(days,5).format(yyyy-MM-dd)`

```json
{
  "next_week": "$func:today.add(days,7).format(yyyy-MM-dd)",
  "upper_trim": "$func:string.upper(hello).trim()",
  "calc": "$func:math.add(10,5).multiply(2)"
}
```

**Özellikleri**:
- İlk function sonucu bir sonraki method'a input olur
- Sınırsız chaining
- DateTime, String, Math, Array nesneleri için özel method'lar

### 3. Pipeline Functions

Pipeline syntax: `$func:{{ input | func1 | func2 | func3 }}`

```json
{
  "pipeline_example": "$func:{{ $(username) | string.upper | string.trim }}"
}
```

**Özellikleri**:
- Unix pipe (`|`) benzeri syntax
- İlk değer literal veya placeholder olabilir
- Her step bir sonrakine veri geçirir
- En okunabilir syntax

#### Pipeline Örnekleri

```json
// Literal input
"result": "$func:{{ hello world | string.upper | string.trim }}"
// Output: "HELLO WORLD"

// Placeholder input
"result": "$func:{{ $(name) | string.upper | string.replace(JOHN,JANE) }}"
// Input: name = "john"
// Output: "JANE"

// Function as first step
"result": "$func:{{ today | add(days,7) | format(yyyy-MM-dd) }}"
// Output: "2025-10-16" (örnek)

// Complex pipeline
"hash": "$func:{{ $(password) | string.lower | hash.sha256 }}"
```

---

## Function Kategorileri

### Kategori Özeti

| Kategori | Prefix | Function Sayısı | Açıklama |
|----------|--------|-----------------|----------|
| **Date/Time** | - | 5 | Tarih ve zaman işlemleri |
| **String** | `string.` | 10 | String manipülasyonu |
| **Math** | `math.` | 10 | Matematiksel işlemler |
| **Hash** | `hash.` | 2 | Kriptografik hash |
| **URL** | `url.` | 6 | URL işlemleri |
| **Web** | `web.` | 4 | HTTP istekleri |
| **IP** | `ip.` | 4 | IP adresi işlemleri |
| **JSON** | `json.` | 5 | JSON parsing |
| **Array** | `array.` | 3 | Dizi işlemleri |
| **Utility** | - | 6 | Çeşitli yardımcılar |

---

## Date/Time Functions

### today

Bugünün tarihini döner (saat 00:00:00).

**Syntax**: `$func:today`

**Return**: `DateTime` object

**Örnekler**:
```json
{
  "today": "$func:today",
  "formatted": "$func:today.format(yyyy-MM-dd)"
}
```

### now

Şu anki tarih ve saati döner.

**Syntax**: `$func:now`

**Return**: `DateTime` object

**Örnekler**:
```json
{
  "now": "$func:now",
  "timestamp": "$func:now.format(yyyy-MM-dd HH:mm:ss)"
}
```

### yesterday

Dünün tarihini döner.

**Syntax**: `$func:yesterday`

**Return**: `DateTime` object

**Örnekler**:
```json
{
  "yesterday": "$func:yesterday.format(yyyy-MM-dd)"
}
```

### tomorrow

Yarının tarihini döner.

**Syntax**: `$func:tomorrow`

**Return**: `DateTime` object

**Örnekler**:
```json
{
  "tomorrow": "$func:tomorrow.format(dd/MM/yyyy)"
}
```

### DateTime Methods (Chaining)

#### add(unit, value)

Tarih/saate belirtilen değeri ekler.

**Parameters**:
- `unit`: `days`, `hours`, `minutes`, `seconds`, `months`, `years`
- `value`: Integer (pozitif veya negatif)

**Örnekler**:
```json
{
  "next_week": "$func:today.add(days,7)",
  "next_month": "$func:today.add(months,1)",
  "in_2_hours": "$func:now.add(hours,2)"
}
```

#### subtract(unit, value)

Tarih/saatten belirtilen değeri çıkarır.

**Parameters**:
- `unit`: `days`, `hours`, `minutes`, `seconds`, `months`, `years`
- `value`: Integer (pozitif)

**Örnekler**:
```json
{
  "last_week": "$func:today.subtract(days,7)",
  "3_months_ago": "$func:today.subtract(months,3)"
}
```

#### format(pattern)

Tarih/saati belirtilen formatta string'e çevirir.

**Parameters**:
- `pattern`: .NET DateTime format pattern

**Format Patterns**:
- `yyyy-MM-dd`: 2025-10-09
- `dd/MM/yyyy`: 09/10/2025
- `HH:mm:ss`: 14:30:45
- `yyyy-MM-dd HH:mm:ss`: 2025-10-09 14:30:45
- `dddd, MMMM dd, yyyy`: Thursday, October 09, 2025

**Örnekler**:
```json
{
  "iso_date": "$func:today.format(yyyy-MM-dd)",
  "turkish_date": "$func:today.format(dd.MM.yyyy)",
  "full_timestamp": "$func:now.format(yyyy-MM-dd HH:mm:ss.fff)"
}
```

### Kompleks Date/Time Örnekleri

```json
{
  // 7 gün sonra
  "deadline": "$func:today.add(days,7).format(yyyy-MM-dd)",
  
  // 3 ay önce
  "quarter_ago": "$func:today.subtract(months,3).format(MMMM yyyy)",
  
  // Gelecek yıl aynı gün
  "next_year_same_day": "$func:today.add(years,1).format(dd MMMM yyyy)",
  
  // 2 saat 30 dakika sonra
  "meeting_time": "$func:now.add(hours,2).add(minutes,30).format(HH:mm)"
}
```

---

## String Functions

String manipülasyonu için 10 function.

### string.upper(text)

Metni büyük harfe çevirir.

**Syntax**: `$func:string.upper(text)` veya `$func:{{ text | string.upper }}`

**Örnekler**:
```json
{
  "upper": "$func:string.upper(hello world)",
  "chained": "$func:{{ $(name) | string.upper }}"
}
// Output: "HELLO WORLD"
```

### string.lower(text)

Metni küçük harfe çevirir.

**Syntax**: `$func:string.lower(text)` veya `$func:{{ text | string.lower }}`

**Örnekler**:
```json
{
  "lower": "$func:string.lower(HELLO WORLD)"
}
// Output: "hello world"
```

### string.trim(text)

Başındaki ve sonundaki boşlukları kaldırır.

**Syntax**: `$func:string.trim(text)` veya `$func:{{ text | string.trim }}`

**Örnekler**:
```json
{
  "trimmed": "$func:string.trim(  hello  )"
}
// Output: "hello"
```

### string.replace(text, old, new)

Metinde arama/değiştirme yapar.

**Syntax**: `$func:string.replace(text,old,new)` veya chained: `.replace(old,new)`

**Örnekler**:
```json
{
  "replaced": "$func:string.replace(hello world,world,universe)"
}
// Output: "hello universe"

// Chained
{
  "replaced": "$func:{{ $(text) | string.replace(old,new) }}"
}
```

### string.substring(text, start, [length])

Alt string alır.

**Parameters**:
- `text`: Kaynak string
- `start`: Başlangıç index (0-based)
- `length`: (Opsiyonel) Karakter sayısı

**Örnekler**:
```json
{
  // İlk 5 karakter
  "first_5": "$func:string.substring(hello world,0,5)",
  // Output: "hello"
  
  // 6. karakterden itibaren tümü
  "from_6": "$func:string.substring(hello world,6)",
  // Output: "world"
}
```

### string.contains(text, search)

Metin içinde arama yapar. Boolean döner (`true`/`false`).

**Syntax**: `$func:string.contains(text,search)`

**Örnekler**:
```json
{
  "has_world": "$func:string.contains(hello world,world)"
}
// Output: "true"
```

### string.startswith(text, prefix)

Metnin belirtilen prefix ile başlayıp başlamadığını kontrol eder.

**Syntax**: `$func:string.startswith(text,prefix)`

**Örnekler**:
```json
{
  "starts": "$func:string.startswith(hello world,hello)"
}
// Output: "true"
```

### string.endswith(text, suffix)

Metnin belirtilen suffix ile bitip bitmediğini kontrol eder.

**Syntax**: `$func:string.endswith(text,suffix)`

**Örnekler**:
```json
{
  "ends": "$func:string.endswith(hello world,world)"
}
// Output: "true"
```

### string.split(text, separator)

Metni ayırıcıya göre böler ve JSON array döner.

**Syntax**: `$func:string.split(text,separator)`

**Örnekler**:
```json
{
  "words": "$func:string.split(hello,world,test,,)"
}
// Output: ["hello","world","test"]
```

### string.length(text)

Metnin karakter sayısını döner.

**Syntax**: `$func:string.length(text)`

**Örnekler**:
```json
{
  "len": "$func:string.length(hello world)"
}
// Output: "11"
```

### Kompleks String Örnekleri

```json
{
  // Email normalization
  "normalized_email": "$func:{{ $(email) | string.lower | string.trim }}",
  
  // Username extraction from email
  "username": "$func:{{ $(email) | string.split(@) | array.get(0) }}",
  
  // Clean and uppercase
  "clean": "$func:{{ $(input) | string.trim | string.upper | string.replace( ,_) }}",
  
  // Extract domain from URL
  "domain": "$func:{{ $(url) | url.domain | string.upper }}"
}
```

---

## Math Functions

Matematiksel işlemler için 10 function.

### math.add(num1, num2)

İki sayıyı toplar.

**Syntax**: `$func:math.add(10,5)`

**Örnekler**:
```json
{
  "sum": "$func:math.add(10,5)"
}
// Output: "15"
```

### math.subtract(num1, num2)

İki sayıyı çıkarır.

**Syntax**: `$func:math.subtract(10,5)`

**Örnekler**:
```json
{
  "diff": "$func:math.subtract(100,25)"
}
// Output: "75"
```

### math.multiply(num1, num2)

İki sayıyı çarpar.

**Syntax**: `$func:math.multiply(10,5)`

**Örnekler**:
```json
{
  "product": "$func:math.multiply(7,8)"
}
// Output: "56"
```

### math.divide(num1, num2)

İki sayıyı böler.

**Syntax**: `$func:math.divide(10,2)`

**Örnekler**:
```json
{
  "quotient": "$func:math.divide(100,4)"
}
// Output: "25"
```

⚠️ **Not**: Sıfıra bölme hatası fırlatır.

### math.round(number, [digits])

Sayıyı yuvarlar.

**Parameters**:
- `number`: Yuvarlanacak sayı
- `digits`: (Opsiyonel) Ondalık basamak sayısı (default: 0)

**Örnekler**:
```json
{
  "rounded": "$func:math.round(3.14159)",
  // Output: "3"
  
  "two_decimals": "$func:math.round(3.14159,2)"
  // Output: "3.14"
}
```

### math.floor(number)

Sayıyı alta yuvarlar.

**Syntax**: `$func:math.floor(3.9)`

**Örnekler**:
```json
{
  "floored": "$func:math.floor(3.9)"
}
// Output: "3"
```

### math.ceil(number)

Sayıyı üste yuvarlar.

**Syntax**: `$func:math.ceil(3.1)`

**Örnekler**:
```json
{
  "ceiled": "$func:math.ceil(3.1)"
}
// Output: "4"
```

### math.min(num1, num2)

İki sayıdan küçük olanı döner.

**Syntax**: `$func:math.min(10,5)`

**Örnekler**:
```json
{
  "minimum": "$func:math.min(100,50)"
}
// Output: "50"
```

### math.max(num1, num2)

İki sayıdan büyük olanı döner.

**Syntax**: `$func:math.max(10,5)`

**Örnekler**:
```json
{
  "maximum": "$func:math.max(100,50)"
}
// Output: "100"
```

### math.abs(number)

Sayının mutlak değerini döner.

**Syntax**: `$func:math.abs(-5)`

**Örnekler**:
```json
{
  "absolute": "$func:math.abs(-42)"
}
// Output: "42"
```

### Kompleks Math Örnekleri

```json
{
  // Yüzde hesaplama
  "percentage": "$func:math.divide($(part),$(total)).multiply(100).round(2)",
  
  // KDV hesaplama
  "with_vat": "$func:math.multiply($(price),1.18).round(2)",
  
  // Ortalama
  "average": "$func:math.add($(num1),$(num2)).divide(2).round(1)"
}
```

---

## Hash Functions

Kriptografik hash işlemleri.

### hash.md5(text)

MD5 hash üretir (32 karakter hex).

**Syntax**: `$func:hash.md5(text)`

**Örnekler**:
```json
{
  "md5": "$func:hash.md5(hello world)"
}
// Output: "5eb63bbbe01eeed093cb22bb8f5acdc3"
```

⚠️ **Not**: MD5 artık güvenli sayılmaz, sadece checksum için kullanın.

### hash.sha256(text)

SHA-256 hash üretir (64 karakter hex).

**Syntax**: `$func:hash.sha256(text)`

**Örnekler**:
```json
{
  "sha256": "$func:hash.sha256(hello world)"
}
// Output: "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9"
```

✅ **Güvenli**: Şifre hash'leme ve güvenlik için önerilen.

### Hash Örnekleri

```json
{
  // Password hash (with salt)
  "password_hash": "$func:{{ $(password) | string.lower | hash.sha256 }}",
  
  // File checksum
  "file_md5": "$func:hash.md5($file:path/to/file.txt)",
  
  // User ID generation
  "user_id": "$func:{{ $(email) | string.lower | hash.md5 | string.substring(0,8) }}"
}
```

---

## URL Functions

URL işlemleri için 6 function.

### url.encode(text)

URL encoding yapar.

**Syntax**: `$func:url.encode(text)`

**Örnekler**:
```json
{
  "encoded": "$func:url.encode(hello world)"
}
// Output: "hello+world"
```

### url.decode(text)

URL decoding yapar.

**Syntax**: `$func:url.decode(text)`

**Örnekler**:
```json
{
  "decoded": "$func:url.decode(hello+world)"
}
// Output: "hello world"
```

### url.domain(url)

URL'den domain çıkarır.

**Syntax**: `$func:url.domain(url)`

**Örnekler**:
```json
{
  "domain": "$func:url.domain(https://www.example.com/path?query=1)"
}
// Output: "www.example.com"
```

### url.path(url)

URL'den path çıkarır.

**Syntax**: `$func:url.path(url)`

**Örnekler**:
```json
{
  "path": "$func:url.path(https://www.example.com/api/users/123)"
}
// Output: "/api/users/123"
```

### url.query(url)

URL'den query string çıkarır (without `?`).

**Syntax**: `$func:url.query(url)`

**Örnekler**:
```json
{
  "query": "$func:url.query(https://example.com/search?q=test&page=1)"
}
// Output: "q=test&page=1"
```

### url.combine(base, path1, path2, ...)

URL segment'lerini birleştirir.

**Syntax**: `$func:url.combine(base,path1,path2,...)`

**Örnekler**:
```json
{
  "full_url": "$func:url.combine(https://api.example.com,users,123,profile)"
}
// Output: "https://api.example.com/users/123/profile"
```

### URL Örnekleri

```json
{
  // API endpoint construction
  "api_url": "$func:url.combine($(base_url),api,v1,users,$(user_id))",
  
  // Search URL
  "search_url": "$func:{{ $(base_url) | url.combine(search) }}?q=$func:url.encode($(query))",
  
  // Extract and process
  "clean_domain": "$func:{{ $(url) | url.domain | string.upper }}"
}
```

---

## Web Functions

HTTP istekleri için 4 function.

⚠️ **Performance Note**: Synchronous HTTP calls! Uzun sürebilir.

### web.get(url)

HTTP GET isteği yapar.

**Syntax**: `$func:web.get(url)`

**Timeout**: 30 saniye

**Örnekler**:
```json
{
  "response": "$func:web.get(https://api.example.com/data)"
}
```

### web.post(url, data)

HTTP POST isteği yapar (JSON content-type).

**Syntax**: `$func:web.post(url,jsonData)`

**Örnekler**:
```json
{
  "response": "$func:web.post(https://api.example.com/users,{\"name\":\"John\"})"
}
```

### web.put(url, data)

HTTP PUT isteği yapar (JSON content-type).

**Syntax**: `$func:web.put(url,jsonData)`

**Örnekler**:
```json
{
  "response": "$func:web.put(https://api.example.com/users/123,{\"name\":\"Jane\"})"
}
```

### web.delete(url)

HTTP DELETE isteği yapar.

**Syntax**: `$func:web.delete(url)`

**Örnekler**:
```json
{
  "response": "$func:web.delete(https://api.example.com/users/123)"
}
```

### Web Örnekleri

```json
{
  // Fetch and parse
  "user_data": "$func:{{ web.get(https://api.example.com/users/123) | json.get(name) }}",
  
  // POST with context data
  "create_user": "$func:web.post($(api_url),$func:json.create(name,$(username),email,$(email)))"
}
```

---

## IP Functions

IP adresi işlemleri için 4 function.

### ip.local

Local IP adresini döner (ilk IPv4).

**Syntax**: `$func:ip.local`

**Örnekler**:
```json
{
  "local_ip": "$func:ip.local"
}
// Output: "192.168.1.100"
```

### ip.public

Public IP adresini döner (https://api.ipify.org kullanır).

**Syntax**: `$func:ip.public`

**Timeout**: 10 saniye

**Örnekler**:
```json
{
  "public_ip": "$func:ip.public"
}
// Output: "203.0.113.45"
```

### ip.isprivate(ip)

IP'nin private range'de olup olmadığını kontrol eder.

**Private Ranges**:
- 10.0.0.0/8
- 172.16.0.0/12
- 192.168.0.0/16
- 127.0.0.0/8 (localhost)

**Syntax**: `$func:ip.isprivate(ip)`

**Örnekler**:
```json
{
  "is_private": "$func:ip.isprivate(192.168.1.1)"
}
// Output: "true"
```

### ip.ispublic(ip)

IP'nin public olup olmadığını kontrol eder.

**Syntax**: `$func:ip.ispublic(ip)`

**Örnekler**:
```json
{
  "is_public": "$func:ip.ispublic(8.8.8.8)"
}
// Output: "true"
```

---

## JSON Functions

JSON parsing ve manipülasyon için 5 function.

### json.get(json, path)

JSON'dan değer okur (dot notation ile).

**Path Syntax**:
- `name`: Root property
- `user.name`: Nested property
- `items[0]`: Array index
- `users[0].name`: Combined

**Syntax**: `$func:json.get(json,path)`

**Örnekler**:
```json
{
  "name": "$func:json.get({\"user\":{\"name\":\"John\"}},user.name)"
}
// Output: "John"

{
  "first_item": "$func:json.get({\"items\":[\"a\",\"b\"]},items[0])"
}
// Output: "a"
```

### json.length(json, arrayPath)

JSON array'in uzunluğunu döner.

**Syntax**: `$func:json.length(json,arrayPath)`

**Örnekler**:
```json
{
  "count": "$func:json.length({\"items\":[1,2,3]},items)"
}
// Output: "3"
```

### json.first(json, arrayPath)

JSON array'in ilk elemanını döner.

**Syntax**: `$func:json.first(json,arrayPath)`

**Örnekler**:
```json
{
  "first": "$func:json.first({\"items\":[\"a\",\"b\"]},items)"
}
// Output: "a"
```

### json.last(json, arrayPath)

JSON array'in son elemanını döner.

**Syntax**: `$func:json.last(json,arrayPath)`

**Örnekler**:
```json
{
  "last": "$func:json.last({\"items\":[\"a\",\"b\",\"c\"]},items)"
}
// Output: "c"
```

### json.create(key1, value1, key2, value2, ...)

JSON object oluşturur.

**Syntax**: `$func:json.create(key1,value1,key2,value2,...)`

**Örnekler**:
```json
{
  "user_json": "$func:json.create(name,John,age,30,active,true)"
}
// Output: "{\"name\":\"John\",\"age\":\"30\",\"active\":\"true\"}"
```

### JSON Örnekleri

```json
{
  // API response parsing
  "user_name": "$func:json.get($(api_response),data.user.fullName)",
  
  // Array operations
  "first_error": "$func:json.first($(api_response),errors)",
  
  // Create and send
  "payload": "$func:json.create(user,$(username),timestamp,$func:now.format(yyyy-MM-dd))"
}
```

---

## Array Functions

Array (JSON array) işlemleri için 3 function.

### array.get(arrayJson, index)

Array'den index'e göre eleman alır.

**Index Support**:
- Pozitif: `0`, `1`, `2`
- Negatif: `-1` (son), `-2` (sondan ikinci)

**Syntax**: `$func:array.get(arrayJson,index)`

**Örnekler**:
```json
{
  "first": "$func:array.get([\"a\",\"b\",\"c\"],0)",
  // Output: "a"
  
  "last": "$func:array.get([\"a\",\"b\",\"c\"],-1)"
  // Output: "c"
}
```

### array.length(arrayJson)

Array uzunluğunu döner.

**Syntax**: `$func:array.length(arrayJson)`

**Örnekler**:
```json
{
  "count": "$func:array.length([1,2,3,4,5])"
}
// Output: "5"
```

### array.join(arrayJson, separator)

Array elemanlarını birleştirir.

**Syntax**: `$func:array.join(arrayJson,separator)`

**Örnekler**:
```json
{
  "joined": "$func:array.join([\"hello\",\"world\"],\" \")"
}
// Output: "hello world"

{
  "csv": "$func:array.join([\"a\",\"b\",\"c\"],\",\")"
}
// Output: "a,b,c"
```

### Array Örnekleri

```json
{
  // Split and get first
  "first_word": "$func:{{ $(sentence) | string.split( ) | array.get(0) }}",
  
  // Split and count
  "word_count": "$func:{{ $(sentence) | string.split( ) | array.length }}",
  
  // Split, process, join
  "uppercase_words": "$func:{{ $(sentence) | string.split( ) | array.join(_) | string.upper }}"
}
```

---

## Utility Functions

Çeşitli yardımcı fonksiyonlar.

### guid

Yeni bir GUID (UUID) oluşturur.

**Syntax**: `$func:guid`

**Örnekler**:
```json
{
  "request_id": "$func:guid"
}
// Output: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

### random([max])
### random(min, max)

Rastgele sayı üretir.

**Syntax**:
- `$func:random`: 0 ile Int32.MaxValue arası
- `$func:random(max)`: 0 ile max arası
- `$func:random(min,max)`: min ile max arası

**Örnekler**:
```json
{
  "dice": "$func:random(1,7)",
  "percentage": "$func:random(0,101)"
}
```

### base64encode(text)

Base64 encoding yapar (UTF-8).

**Syntax**: `$func:base64encode(text)`

**Örnekler**:
```json
{
  "encoded": "$func:base64encode(hello world)"
}
// Output: "aGVsbG8gd29ybGQ="
```

### base64decode(base64Text)

Base64 decoding yapar.

**Syntax**: `$func:base64decode(base64Text)`

**Örnekler**:
```json
{
  "decoded": "$func:base64decode(aGVsbG8gd29ybGQ=)"
}
// Output: "hello world"
```

### env(variableName)

Environment variable okur.

**Syntax**: `$func:env(variableName)`

**Örnekler**:
```json
{
  "user_profile": "$func:env(USERPROFILE)",
  "temp_dir": "$func:env(TEMP)"
}
```

### username

Kullanıcı adını döner.

**Syntax**: `$func:username`

**Örnekler**:
```json
{
  "user": "$func:username"
}
// Output: "murat"
```

### computername

Bilgisayar adını döner.

**Syntax**: `$func:computername`

**Örnekler**:
```json
{
  "hostname": "$func:computername"
}
// Output: "DESKTOP-ABC123"
```

---

## İleri Seviye Kullanım

### 1. Nested Functions

Function içinde function kullanımı.

```json
{
  "nested": "$func:hash.md5($func:string.lower($(username)))"
}
```

**İşlem Sırası**:
1. `$(username)` → Context'ten değer al
2. `string.lower(...)` → Küçük harfe çevir
3. `hash.md5(...)` → Hash al

### 2. Context Placeholder + Function

```json
{
  "seeder": {
    "user_hash": "$func:hash.sha256($(email))",
    "formatted_date": "$func:today.format($(date_format))"
  }
}
```

### 3. Pipeline ile Complex Transformations

```json
{
  // Email normalization pipeline
  "normalized_email": "$func:{{ $(raw_email) | string.trim | string.lower }}",
  
  // URL construction pipeline
  "api_url": "$func:{{ $(base_path) | url.combine(api,v1,users) }}",
  
  // Data hashing pipeline
  "secure_id": "$func:{{ $(user_id) | string.lower | hash.sha256 | string.substring(0,16) }}"
}
```

### 4. File Content + Function

`$file:` prefix ile dosya okuyup function uygulama.

```json
{
  "file_hash": "$func:hash.md5($file:path/to/file.txt)",
  "file_lines": "$func:string.split($file:data.csv,\\n)"
}
```

### 5. Dynamic Function Parameters

Context değerlerini function parametrelerinde kullanma.

```json
{
  "seeder": {
    "days_ahead": "7",
    "deadline": "$func:today.add(days,$(days_ahead)).format(yyyy-MM-dd)"
  }
}
```

### 6. Conditional Function Usage

Condition içinde function sonuçlarını kullanma.

```json
{
  "conditions": [
    {
      "key": "$func:string.length($(password))",
      "operator": "greater_than",
      "value": "8"
    }
  ]
}
```

### 7. Multi-Step Processing

```json
{
  "seeder": {
    // Step 1: Get date
    "today_str": "$func:today.format(yyyy-MM-dd)",
    
    // Step 2: Combine with username
    "session_key": "$(username)_$(today_str)",
    
    // Step 3: Hash it
    "session_id": "$func:hash.md5($(session_key))"
  }
}
```

### 8. Web API + JSON Parsing

```json
{
  "seeder": {
    // Fetch API
    "api_response": "$func:web.get(https://api.example.com/user/123)",
    
    // Parse JSON
    "user_name": "$func:json.get($(api_response),data.name)",
    "user_email": "$func:json.get($(api_response),data.email)"
  }
}
```

### 9. Array Processing Pipeline

```json
{
  "seeder": {
    // Split CSV
    "items_array": "$func:string.split($(csv_data),\",\")",
    
    // Get first item
    "first_item": "$func:array.get($(items_array),0)",
    
    // Count items
    "item_count": "$func:array.length($(items_array))"
  }
}
```

### 10. Error Handling

Function hataları `UserFeedback.ShowError()` ile bildirilir ve loglenir.

```csharp
// Hata durumunda
try {
    // Function processing
} catch (Exception ex) {
    UserFeedback.ShowError($"Error processing function: {ex.Message}");
    logger?.LogError("Function failed", ex, metadata);
    return input; // Original input'u döner
}
```

---

## Best Practices

### ✅ Yapılması Gerekenler

1. **Pipeline Kullanın**: Okunabilirlik için pipeline syntax tercih edin
   ```json
   "result": "$func:{{ $(input) | string.trim | string.upper }}"
   ```

2. **Context Placeholder**: Dinamik değerler için `$(key)` kullanın
   ```json
   "hash": "$func:hash.md5($(password))"
   ```

3. **Seeder'da Function**: Seeder içinde function'ları kullanıp sonuçları context'e kaydedin
   ```json
   "seeder": {
     "today_formatted": "$func:today.format(yyyy-MM-dd)"
   }
   ```

4. **Error Handling**: Function hatalarını bekleyin, fallback değerler kullanın

5. **Performance**: Web function'ları dikkatli kullanın (30s timeout)

### ❌ Yapılmaması Gerekenler

1. **Aşırı Nested**: Çok fazla nested function okunabilirliği azaltır
   ```json
   // ❌ Kötü
   "result": "$func:hash.md5($func:string.upper($func:string.trim($(input))))"
   
   // ✅ İyi
   "result": "$func:{{ $(input) | string.trim | string.upper | hash.md5 }}"
   ```

2. **Synchronous Web Calls in Loops**: Performance sorunlarına yol açar

3. **Unchecked Division**: Sıfıra bölme kontrolü yapın

4. **Hardcoded Values**: Dinamik değerler için function kullanın
   ```json
   // ❌ Kötü
   "date": "2025-10-09"
   
   // ✅ İyi
   "date": "$func:today.format(yyyy-MM-dd)"
   ```

---

## Sonraki Adımlar

✅ **Function System öğrenildi!** Artık:

1. 🎯 [Action System](05-action-system.md) ile bu function'ları action'larda kullanın
2. 🔌 [Plugin Geliştirme](06-plugin-gelistirme.md) ile custom function'lar yazın
3. 📚 [Örnekler](08-ornekler-ve-use-cases.md) ile gerçek senaryolara bakın

---

*Bu dokümantasyon Contextualizer v1.0.0 için hazırlanmıştır.*

