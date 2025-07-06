# Contextualizer Pipeline Function Test Suite

Bu dosya Unix-style pipeline fonksiyonlarını test etmek için hazırlanmıştır.

## Pipeline Syntax

Pipeline syntax: `$func:{{ input | function1 | function2 | function3 }}`
- `{{` ve `}}` ile pipeline bloğunu sarmalayın
- `|` operatörü ile fonksiyonları zincirleyebilirsiniz
- Soldan sağa doğru işlem yapılır
- Her adım bir önceki adımın sonucunu alır

## Basic Pipeline Tests

### String Processing Pipelines
- Basic pipeline: $func:{{ $(input_text) | string.upper }}
- Multi-step: $func:{{ $(input_text) | string.trim | string.upper }}
- Complex: $func:{{ $(input_text) | string.trim | string.upper | string.replace(HELLO, Hi) }}

### Date Processing Pipelines
- Date formatting: $func:{{ today | format(yyyy-MM-dd) }}
- Date manipulation: $func:{{ today | add(days, 7) | format(MMM dd, yyyy) }}
- Complex date: $func:{{ now | add(hours, -2) | format(HH:mm:ss) }}

### Math Processing Pipelines
- Simple math: $func:{{ $(number1) | math.add(10) | math.multiply(2) }}
- Rounding: $func:{{ $(decimal_number) | math.multiply(100) | math.round | math.divide(100) }}
- Complex calc: $func:{{ $(base_number) | math.add(5) | math.multiply(3) | math.subtract(2) }}

## String Manipulation Pipelines

### Text Cleaning
- Clean text: $func:{{ "  Some Dirty Text With Spaces  " | string.trim | string.lower | string.replace( , _) }}
- Format name: $func:{{ "  Murat AY  " | string.trim | string.lower | string.replace( , .) | string.replace(ı, i) }}
- Clean URL: $func:{{ "  HTTPS://WWW.EXAMPLE.COM/PATH  " | string.trim | string.lower | string.replace(www., ) | url.encode }}

### Text Analysis
- Word count: $func:{{ "The quick brown fox jumps over the lazy dog" | string.split( ) | array.length }}
- First word: $func:{{ "The quick brown fox jumps over the lazy dog" | string.split( ) | array.get(0) | string.upper }}
- Last word: $func:{{ "The quick brown fox jumps over the lazy dog" | string.split( ) | array.get(-1) | string.upper }}

### Text Transformation
- Snake case: $func:{{ "thisIsCamelCase" | string.lower | string.replace( , _) }}
- Title case: $func:{{ "this is lower text" | string.split( ) | array.join( ) | string.upper }}
- Reverse words: $func:{{ "word1 word2 word3" | string.split( ) | array.join( ) }}

## URL Processing Pipelines

### URL Building
- Build API URL: $func:{{ "https://api.example.com" | url.combine(api, v1, users) | url.combine("12345") }}
- Search URL: $func:{{ "https://search.example.com" | url.combine(search) | url.combine("hello world") }}
- Clean domain: $func:{{ "https://www.example.com/path/to/resource" | url.domain | string.lower | string.replace(www., ) }}

### URL Analysis
- Get filename: $func:{{ "https://cdn.example.com/files/document.pdf" | url.path | string.split(/) | array.get(-1) }}
- Get extension: $func:{{ "https://cdn.example.com/files/document.pdf" | url.path | string.split(.) | array.get(-1) | string.lower }}
- Clean path: $func:{{ "https://example.com/path//to/resource?page=1" | url.path | string.replace(//, /) }}

## JSON Processing Pipelines

### JSON Data Extraction
- User name: $func:{{ '{"user":{"profile":{"name":"john doe","email":"john@example.com"}}}' | json.get(user.profile.name) | string.upper }}
- First item: $func:{{ '{"items":[{"name":"item1"},{"name":"item2"}]}' | json.get(items[0].name) | string.upper }}
- Count items: $func:{{ '{"data":[1,2,3,4,5]}' | json.length(data) }}

### JSON Transformation
- Extract and format: $func:{{ $(api_response) | json.get(result.user.email) | string.lower | string.trim }}
- Get and encode: $func:{{ $(data_json) | json.get(message) | string.trim | url.encode }}
- Complex extract: $func:{{ $(nested_json) | json.get(users[0].profile.settings.theme) | string.lower }}

## Array Processing Pipelines

### Array Manipulation
- Process array: $func:{{ '["first","second","third"]' | array.get(1) | string.upper | string.replace(" ", "_") }}
- Join and split: $func:{{ '["item1","item2","item3"]' | array.join(",") | string.split(",") | array.length }}
- Filter and get: $func:{{ '["tag_web","tag_api","tag_json"]' | array.join("|") | string.replace("tag_", "") | string.split("|") }}

## Web API Pipelines

### API Data Processing
- Get IP info: $func:{{ ip.public | web.get | string.trim }}
- Process response: $func:{{ $(api_endpoint) | web.get | json.get(data.result) | string.upper }}
- Chain requests: $func:{{ $(base_api) | url.combine(status) | web.get | json.get(status) }}

## Hash and Encoding Pipelines

### Security Processing
- Hash pipeline: $func:{{ $(password) | string.trim | string.lower | hash.sha256 }}
- Encode data: $func:{{ $(sensitive_data) | string.trim | base64encode | url.encode }}
- Double hash: $func:{{ $(input_data) | hash.md5 | string.upper | hash.sha256 }}

### Data Transformation
- Clean and hash: $func:{{ $(user_input) | string.trim | string.lower | string.replace( , ) | hash.md5 }}
- Encode for URL: $func:{{ $(text_data) | string.trim | base64encode | url.encode }}
- Format token: $func:{{ $(raw_token) | string.trim | string.upper | string.replace(-, ) | hash.sha256 }}

## Complex Real-World Pipelines

### User Data Processing
- Format username: $func:{{ $(raw_username) | string.trim | string.lower | string.replace( , .) | string.substring(0, 20) }}
- Email validation: $func:{{ $(email_input) | string.trim | string.lower | string.contains(@) }}
- Phone format: $func:{{ $(phone_number) | string.replace( , ) | string.replace(-, ) | string.replace((, ) | string.replace(), ) }}

### File Processing
- Get filename: $func:{{ $(file_path) | string.split(\\) | array.get(-1) | string.split(.) | array.get(0) }}
- File extension: $func:{{ $(file_name) | string.split(.) | array.get(-1) | string.lower }}
- Clean filename: $func:{{ $(raw_filename) | string.replace( , _) | string.lower | string.replace(ı, i) }}

### Date and Time Formatting
- Format timestamp: $func:{{ $(unix_timestamp) | add(hours, 3) | format(dd.MM.yyyy HH:mm) }}
- Business date: $func:{{ today | add(days, 1) | format(yyyy-MM-dd) | string.replace(-, /) }}
- Log timestamp: $func:{{ now | format(yyyy-MM-dd HH:mm:ss) | string.replace( , T) }}

## Error Handling Tests

### Edge Cases
- Empty input: $func:{{ | string.upper | string.trim }}
- Invalid JSON: $func:{{ $(invalid_json) | json.get(nonexistent) | string.upper }}
- Math errors: $func:{{ $(zero_value) | math.divide(0) | math.add(1) }}
- Array bounds: $func:{{ $(small_array) | array.get(10) | string.upper }}

### Null/Empty Handling
- Null check: $func:{{ $(maybe_null) | string.trim | string.length | math.add(0) }}
- Empty array: $func:{{ [] | array.length | math.add(1) }}
- Empty string: $func:{{ | string.upper | string.length }}

## Test Variables (Replace these with actual values)

### String Variables
- $(input_text): "  Hello World  "
- $(dirty_text): "  Some Dirty Text With Spaces  "
- $(full_name): "  Murat AY  "
- $(sentence): "The quick brown fox jumps over the lazy dog"
- $(camel_case): "thisIsCamelCase"
- $(lower_text): "this is lower text"
- $(text_input): "word1 word2 word3"

### URL Variables
- $(base_url): "https://api.example.com"
- $(search_base): "https://search.example.com"
- $(user_id): "12345"
- $(search_term): "hello world"
- $(full_url): "https://www.example.com/path/to/resource"
- $(file_url): "https://cdn.example.com/files/document.pdf"
- $(url_input): "  HTTPS://WWW.EXAMPLE.COM/PATH  "

### JSON Variables
- $(user_json): '{"user":{"profile":{"name":"john doe","email":"john@example.com"}}}'
- $(items_json): '{"items":[{"name":"item1"},{"name":"item2"}]}'
- $(collection_json): '{"data":[1,2,3,4,5]}'
- $(api_response): '{"result":{"user":{"email":"  USER@EXAMPLE.COM  "}}}'
- $(nested_json): '{"users":[{"profile":{"settings":{"theme":"DARK MODE"}}}]}'

### Number Variables
- $(number1): "15"
- $(decimal_number): "3.14159"
- $(base_number): "10"
- $(zero_value): "0"

### Array Variables
- $(array_json): '["first","second","third"]'
- $(items_array): '["item1","item2","item3"]'
- $(tags_array): '["tag_web","tag_api","tag_json"]'
- $(small_array): '["a","b"]'

### API Variables
- $(api_endpoint): "https://httpbin.org/json"
- $(base_api): "https://api.github.com"

### Security Variables
- $(password): "  MySecretPassword  "
- $(sensitive_data): "confidential information"
- $(input_data): "user input data"
- $(user_input): "  User Input With Spaces  "
- $(raw_token): "abc-def-ghi-jkl"

### File Variables
- $(file_path): "C:\\Users\\Documents\\MyFile.txt"
- $(file_name): "document.PDF"
- $(raw_filename): "My File Name İçerik.txt"

### Other Variables
- $(raw_username): "  Murat AY  "
- $(email_input): "  USER@EXAMPLE.COM  "
- $(phone_number): "(555) 123-4567"
- $(unix_timestamp): "1640995200"
- $(invalid_json): '{"invalid": json}'
- $(maybe_null): ""

---

**Test Instructions:**
1. Replace all $(variable) placeholders with actual test values
2. Use this file as OutputFormat: `$file:C:\Users\murat\source\repos\Contextualizer\pipeline_test_cases.md`
3. Run the application to test pipeline functionality
4. Verify each pipeline works correctly
5. Check error handling for edge cases

**Expected Pipeline Behavior:**
- Pipeline should process left-to-right
- Each function receives the output of the previous function
- Error in any step should be handled gracefully
- Pipeline should be more readable than nested function calls
- Backward compatibility with existing chaining should be maintained