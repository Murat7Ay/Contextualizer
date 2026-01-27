# Function Pipeline Reference

This page documents the $func: template engine, including parsing rules, pipeline syntax, chaining, and the supported function catalog.

## Entry Point and Execution Order
Function expansion is handled by `FunctionProcessor.ProcessFunctions()`:
1. Pipeline functions are processed first: $func:{{ ... }}
2. Regular functions are processed second: $func:...

Source:
- Function processor: [Contextualizer.Core/FunctionProcessor.cs](Contextualizer.Core/FunctionProcessor.cs)
- Parser and pipeline logic: [Contextualizer.Core/FunctionProcessing/FunctionParser.cs](Contextualizer.Core/FunctionProcessing/FunctionParser.cs)

## Syntax Overview
### Regular Functions
Syntax: $func:functionName(arg1, arg2)

Examples:
- $func:guid()
- $func:base64encode("hello")
- $func:string.replace("hello","l","x")

### Pipeline Functions
Syntax: $func:{{ step1 | step2 | step3 }}

Rules:
- The first step can be a literal (string, number, placeholder) or a function.
- Every subsequent step is executed as a function against the output of the previous step.
- Pipeline parsing respects quotes and parentheses.

Examples:
- $func:{{ "hello" | string.upper | string.replace("H","J") }}
- $func:{{ $(id) | string.trim | string.upper }}

## Placeholder Resolution
Placeholders are resolved using the syntax $(key). Placeholder resolution occurs inside function parameters and literal pipeline steps.

Source:
- Placeholder resolver: [Contextualizer.Core/FunctionProcessing/FunctionHelpers/PlaceholderResolver.cs](Contextualizer.Core/FunctionProcessing/FunctionHelpers/PlaceholderResolver.cs)
- Parameter parsing: [Contextualizer.Core/FunctionProcessing/FunctionHelpers/ParameterParser.cs](Contextualizer.Core/FunctionProcessing/FunctionHelpers/ParameterParser.cs)

## Parameter Parsing Rules
- Commas separate parameters only when not inside quotes or nested brackets/parentheses.
- Surrounding quotes are stripped from parameter values.
- After parsing, each parameter is passed through placeholder resolution.

Source:
- Parameter parser: [Contextualizer.Core/FunctionProcessing/FunctionHelpers/ParameterParser.cs](Contextualizer.Core/FunctionProcessing/FunctionHelpers/ParameterParser.cs)

## Chaining Rules
Chaining is supported in two ways:
1. Explicit chained calls: $func:baseFunction(...).method(...).method(...)
2. Pipeline style: $func:{{ literal | method | method(...) }}

Chaining behavior:
- Namespaced functions (string., math., url., hash., json., array.) can be used as base functions or chained methods.
- `base64encode` and `base64decode` are special-case chained methods when the input is a string.
- If the input is a DateTime (from today/now/yesterday/tomorrow), DateTime methods are allowed.

Source:
- Base executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/BaseFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/BaseFunctionExecutor.cs)
- Parser chaining: [Contextualizer.Core/FunctionProcessing/FunctionParser.cs](Contextualizer.Core/FunctionProcessing/FunctionParser.cs)

## Error Handling
- Regular functions: errors return the original $func:... expression (with an error toast shown).
- Pipeline functions: errors return an empty string (with an error toast shown).

Source:
- Function error handling: [Contextualizer.Core/FunctionProcessing/FunctionParser.cs](Contextualizer.Core/FunctionProcessing/FunctionParser.cs)

## Function Catalog
This section lists all built-in functions.

### Base Functions
- today
- now
- yesterday
- tomorrow
- guid
- random(max) or random(min, max)
- base64encode(text)
- base64decode(text)
- env(name)
- username
- computername

Source:
- Base switch: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/BaseFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/BaseFunctionExecutor.cs)

### String Functions (string.)
- string.upper(text)
- string.lower(text)
- string.trim(text)
- string.replace(text, old, new)
- string.substring(text, start, [length])
- string.contains(text, value)
- string.startswith(text, value)
- string.endswith(text, value)
- string.split(text, separator) → returns JSON array
- string.length(text)

Chained methods: upper, lower, trim, replace, substring, contains, startswith, endswith, split, length

Source:
- String executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/StringFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/StringFunctionExecutor.cs)

### Math Functions (math.)
- math.add(a, b)
- math.subtract(a, b)
- math.multiply(a, b)
- math.divide(a, b)
- math.round(number, [digits])
- math.floor(number)
- math.ceil(number)
- math.min(a, b)
- math.max(a, b)
- math.abs(number)

Chained methods: add, subtract, multiply, divide, round, floor, ceil, min, max, abs

Source:
- Math executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/MathFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/MathFunctionExecutor.cs)

### Array Functions (array.)
- array.get(jsonArray, index) (supports negative index)
- array.length(jsonArray)
- array.join(jsonArray, separator)

Chained methods: get, length, join

Source:
- Array executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/ArrayFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/ArrayFunctionExecutor.cs)

### JSON Functions (json.)
- json.get(json, path) (dot/bracket path, arrays allowed)
- json.length(json, arrayPath)
- json.first(json, arrayPath)
- json.last(json, arrayPath)
- json.create(key1, value1, ...)

Chained methods: get, length, first, last, create

Source:
- JSON executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/JsonFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/JsonFunctionExecutor.cs)

### URL Functions (url.)
- url.encode(text)
- url.decode(text)
- url.domain(url)
- url.path(url)
- url.query(url)
- url.combine(baseUrl, segment1, ...)

Chained methods: encode, decode, domain, path, query, combine

Source:
- URL executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/UrlFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/UrlFunctionExecutor.cs)

### Web Functions (web.)
- web.get(url)
- web.post(url, jsonBody)
- web.put(url, jsonBody)
- web.delete(url)

Source:
- Web executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/WebFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/WebFunctionExecutor.cs)

### IP Functions (ip.)
- ip.local()
- ip.public()
- ip.isprivate(ip)
- ip.ispublic(ip)

Source:
- IP executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/IpFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/IpFunctionExecutor.cs)

### Hash Functions (hash.)
- hash.md5(text)
- hash.sha256(text)

Source:
- Hash executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/HashFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/HashFunctionExecutor.cs)

### DateTime Methods (chained)
When the input is a DateTime (e.g., from today or now), the following chained methods are supported:
- add(unit, value)
- subtract(unit, value)
- format(formatString)

Source:
- DateTime executor: [Contextualizer.Core/FunctionProcessing/FunctionExecutors/DateTimeFunctionExecutor.cs](Contextualizer.Core/FunctionProcessing/FunctionExecutors/DateTimeFunctionExecutor.cs)

## Related Docs
- Configuration: [docs/wiki/pages/Configuration.md](docs/wiki/pages/Configuration.md)
- Configuration examples: [docs/wiki/pages/Configuration-Examples.md](docs/wiki/pages/Configuration-Examples.md)
- Configuration recipes: [docs/wiki/pages/Configuration-Recipes.md](docs/wiki/pages/Configuration-Recipes.md)
