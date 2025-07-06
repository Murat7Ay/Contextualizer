# Final Pipeline Test Suite - Complete Validation

## ✅ String Processing
- Basic upper: $func:{{ hello world | string.upper }}
- Trim and upper: $func:{{ "  hello world  " | string.trim | string.upper }}
- Replace and length: $func:{{ hello world | string.replace("world", "universe") | string.length }}
- Multi-step: $func:{{ "  Some Dirty Text  " | string.trim | string.lower | string.replace(" ", "_") }}

## ✅ Date Processing  
- Today formatted: $func:{{ today | format(yyyy-MM-dd) }}
- Tomorrow: $func:{{ tomorrow | format(dd.MM.yyyy) }}
- Date arithmetic: $func:{{ today | add(days, 7) | format(MMM dd, yyyy) }}

## ✅ Math Processing
- Simple math: $func:{{ 10 | math.add(5) | math.multiply(2) }}
- Rounding: $func:{{ 3.14159 | math.multiply(100) | math.round | math.divide(100) }}
- Complex calc: $func:{{ 20 | math.add(5) | math.multiply(3) | math.subtract(10) }}

## ✅ Array Processing
- Array length: $func:{{ '["apple","banana","cherry"]' | array.length }}
- Get first: $func:{{ '["first","second","last"]' | array.get(0) | string.upper }}
- Get last: $func:{{ '["first","second","last"]' | array.get(-1) | string.upper }}
- Join array: $func:{{ '["a","b","c"]' | array.join("-") | string.upper }}
- Complex: $func:{{ '["item1","item2","item3"]' | array.join(",") | string.split(",") | array.length }}

## ✅ Text Analysis
- Word count: $func:{{ "The quick brown fox" | string.split(" ") | array.length }}
- First word: $func:{{ "The quick brown fox" | string.split(" ") | array.get(0) | string.upper }}
- Last word: $func:{{ "The quick brown fox" | string.split(" ") | array.get(-1) | string.upper }}

## ✅ Hash and Encoding
- MD5 hash: $func:{{ hello | string.upper | hash.md5 }}
- SHA256: $func:{{ password123 | hash.sha256 }}
- Base64: $func:{{ hello world | base64encode }}
- Complex: $func:{{ "secret data" | string.trim | string.upper | hash.sha256 }}

## ✅ URL Processing
- URL encode: $func:{{ hello world & test | url.encode }}
- Get domain: $func:{{ https://www.example.com/path | url.domain | string.lower }}
- Get path: $func:{{ https://cdn.example.com/files/doc.pdf | url.path }}
- Get filename: $func:{{ https://cdn.example.com/files/doc.pdf | url.path | string.split("/") | array.get(-1) }}
- URL combine: $func:{{ https://api.example.com | url.combine("v1", "users", "123") }}

## ✅ JSON Processing
- Get value: $func:{{ '{"name":"john","age":30}' | json.get(name) | string.upper }}
- Nested access: $func:{{ '{"user":{"profile":{"name":"alice"}}}' | json.get(user.profile.name) | string.upper }}
- Array access: $func:{{ '{"items":["a","b","c"]}' | json.get(items[0]) | string.upper }}
- Array length: $func:{{ '{"data":[1,2,3,4,5]}' | json.length(data) }}

## ✅ Complex Real-World Examples
- Email format: $func:{{ "  USER@EXAMPLE.COM  " | string.trim | string.lower }}
- Username clean: $func:{{ "  John Doe  " | string.trim | string.lower | string.replace(" ", ".") }}
- File extension: $func:{{ "document.PDF" | string.split(".") | array.get(-1) | string.lower }}
- Data processing: $func:{{ '["tag_web","tag_api","tag_json"]' | array.join("|") | string.replace("tag_", "") | string.split("|") | array.get(1) }}

## ✅ Error Handling Tests
- Empty input: $func:{{ "" | string.upper | string.length }}
- Out of bounds: $func:{{ '["a","b"]' | array.get(10) }}
- Invalid JSON: $func:{{ '{"invalid": json}' | json.get(name) }}

---

## Expected Results Summary:

### String Processing
- Basic upper: HELLO WORLD
- Trim and upper: HELLO WORLD  
- Replace and length: 15
- Multi-step: some_dirty_text

### Date Processing
- Today formatted: 2025-07-06
- Tomorrow: 07.07.2025
- Date arithmetic: Jul 13

### Math Processing
- Simple math: 30
- Rounding: 3.14
- Complex calc: 65

### Array Processing
- Array length: 3
- Get first: FIRST
- Get last: LAST
- Join array: A-B-C
- Complex: 3

### Text Analysis
- Word count: 4
- First word: THE
- Last word: FOX

### Hash and Encoding
- MD5 hash: (32 char hash)
- SHA256: (64 char hash)
- Base64: aGVsbG8gd29ybGQ=
- Complex: (64 char hash)

### URL Processing
- URL encode: hello+world+%26+test
- Get domain: www.example.com
- Get path: /files/doc.pdf
- Get filename: doc.pdf
- URL combine: https://api.example.com/v1/users/123

### JSON Processing
- Get value: JOHN
- Nested access: ALICE
- Array access: A
- Array length: 5

### Complex Examples
- Email format: user@example.com
- Username clean: john.doe
- File extension: pdf
- Data processing: api

### Error Handling
- Empty input: 0
- Out of bounds: (empty)
- Invalid JSON: (empty)

---

**🎯 This comprehensive test validates all pipeline functionality is working correctly!**