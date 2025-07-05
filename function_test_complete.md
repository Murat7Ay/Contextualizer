# Contextualizer Function Test Suite

Bu dosya tüm fonksiyonları ve chaining özelliklerini test etmek için hazırlanmıştır.

## Basic Functions

### Date/Time Functions
- Today: $func:today
- Now: $func:now
- Yesterday: $func:yesterday
- Tomorrow: $func:tomorrow

### System Functions
- Username: $func:username
- Computer Name: $func:computername
- Environment Variable: $func:env(PATH)

### Utility Functions
- GUID: $func:guid
- Random (no params): $func:random
- Random (max): $func:random(100)
- Random (min-max): $func:random(10, 50)

### Encoding Functions
- Base64 Encode: $func:base64encode(Hello World)
- Base64 Decode: $func:base64decode(SGVsbG8gV29ybGQ=)

## Hash Functions

- MD5: $func:hash.md5(test)
- SHA256: $func:hash.sha256(test)

## String Functions

### Basic String Operations
- Upper: $func:string.upper(hello world)
- Lower: $func:string.lower(HELLO WORLD)
- Trim: $func:string.trim(  hello world  )

### String Analysis
- Contains: $func:string.contains(hello world, world)
- Starts With: $func:string.startswith(hello world, hello)
- Ends With: $func:string.endswith(hello world, world)

### String Manipulation
- Replace: $func:string.replace(hello world, world, universe)
- Substring (start): $func:string.substring(hello world, 6)
- Substring (start+length): $func:string.substring(hello world, 0, 5)

### String Splitting
- Split by space: $func:string.split(apple banana cherry,  )
- Split by comma: $func:string.split(a;b;c, ;)
- Split by pipe: $func:string.split(x|y|z, |)

## Math Functions

### Basic Math
- Add: $func:math.add(15, 25)
- Subtract: $func:math.subtract(50, 20)
- Multiply: $func:math.multiply(7, 8)
- Divide: $func:math.divide(100, 4)

### Math Rounding
- Round (no decimals): $func:math.round(3.14159)
- Round (2 decimals): $func:math.round(3.14159, 2)
- Floor: $func:math.floor(4.9)
- Ceiling: $func:math.ceil(4.1)

### Math Comparison
- Minimum: $func:math.min(10, 5)
- Maximum: $func:math.max(10, 5)
- Absolute: $func:math.abs(-42)

## JSON Functions

### JSON Object Access
- Get Property: $func:json.get({"name":"John","age":30,"city":"NYC"}, name)
- Get Nested: $func:json.get({"user":{"profile":{"name":"Alice"}}}, user.profile.name)

### JSON Array Access
- Get Array Element: $func:json.get({"users":[{"id":1,"name":"John"},{"id":2,"name":"Jane"}]}, users[0].name)
- Get Array Length: $func:json.length({"items":[1,2,3,4,5]}, items)
- Get First Element: $func:json.first({"numbers":[10,20,30]}, numbers)
- Get Last Element: $func:json.last({"numbers":[10,20,30]}, numbers)

### JSON Creation
- Create Object: $func:json.create(name, John, age, 30, city, NYC)

## Array Functions

### Direct Array Access
- Get Element: $func:array.get(["apple","banana","cherry"], 1)
- Get Length: $func:array.length(["red","green","blue"])
- Join Elements: $func:array.join(["a","b","c"], -)

## URL Functions

### URL Encoding
- Encode: $func:url.encode(hello world & special chars!)
- Decode: $func:url.decode(hello%20world%20%26%20special%20chars!)

### URL Parsing
- Domain: $func:url.domain(https://api.example.com/v1/users?page=1)
- Path: $func:url.path(https://api.example.com/v1/users?page=1)
- Query: $func:url.query(https://api.example.com/v1/users?page=1&limit=10)

### URL Building
- Combine: $func:url.combine(https://api.example.com, v1, users, 123)

## Web Functions

### HTTP Requests
- GET Request: $func:web.get(https://api.ipify.org)
- POST Request: $func:web.post(https://httpbin.org/post, {"message":"hello"})

## IP Functions

### IP Information
- Local IP: $func:ip.local
- Public IP: $func:ip.public

### IP Validation
- Is Private: $func:ip.isprivate(192.168.1.1)
- Is Public: $func:ip.ispublic(8.8.8.8)

## Chaining Examples

### DateTime Chaining
- Add and Format: $func:today.add(days, 7).format(yyyy-MM-dd)
- Current Time Format: $func:now.format(HH:mm:ss)
- Past Date: $func:yesterday.format(MMM dd, yyyy)

### String Chaining
- Upper and Replace: $func:string.upper(hello world).replace(HELLO, Hi)
- Trim and Upper: $func:string.trim(  hello  ).upper()
- Multiple Operations: $func:string.replace(hello world, world, universe).upper()

### String Split Chaining
- Split and Get: $func:string.split(apple;banana;cherry, ;).get(1)
- Split and Length: $func:string.split(red|green|blue|yellow, |).length()
- Split and Join: $func:string.split(a-b-c, -).join(|)

### Array to String Chaining
- Array Get Upper: $func:array.get(["hello","world","test"], 0).upper()
- Array Join Upper: $func:array.join(["hello","world"], ).upper()

### Complex Chaining
- Split, Get, Upper: $func:string.split(john;jane;bob, ;).get(2).upper()
- Math and String: $func:math.add(10, 5).toString().replace(15, fifteen)

## Advanced Examples

### JSON with Chaining
- JSON Get Upper: $func:json.get({"users":["alice","bob","charlie"]}, users[0]).upper()

### Complex String Operations
- Multi-step: $func:string.replace(hello world test, world, universe).substring(0, 10).upper()

### URL Building with Encoding
- Build and Encode: $func:url.combine(https://api.com, search).replace(search, $func:url.encode(hello world))

## Error Testing

### Edge Cases
- Empty String: $func:string.upper()
- Invalid JSON: $func:json.get({invalid}, test)
- Division by Zero: $func:math.divide(10, 0)
- Array Out of Bounds: $func:array.get(["a","b"], 5)

## Custom

### Custom cases
- Link: $func:base64encode(Murat Ay)
- Url: $(real_url)

---

**Test Instructions:**
1. Use this file as OutputFormat: `$file:C:\Users\murat\source\repos\Contextualizer\function_test_complete.md`
2. Run the application
3. Check each section for correct results
4. Verify chaining operations work as expected
5. Note any errors or unexpected outputs

**Expected Behavior:**
- All basic functions should return appropriate values
- Chaining should work seamlessly
- Error cases should return empty strings or handle gracefully
- JSON parsing should handle complex nested structures
- String operations should preserve formatting