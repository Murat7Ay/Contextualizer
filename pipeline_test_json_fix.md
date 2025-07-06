# Pipeline JSON Fix Test

## JSON Pipeline Tests
- Simple JSON: $func:{{ '{"name":"john"}' | json.get(name) }}
- JSON with uppercase: $func:{{ '{"name":"john"}' | json.get(name) | string.upper }}
- JSON array: $func:{{ '{"items":["a","b","c"]}' | json.get(items[0]) | string.upper }}

## String Processing (Should also work better)
- Basic trim: $func:{{ "  hello  " | string.trim }}
- Trim and upper: $func:{{ "  hello  " | string.trim | string.upper }}
- Text split: $func:{{ "a b c" | string.split(" ") | array.get(0) }}

## Expected Results
- Simple JSON: john
- JSON with uppercase: JOHN  
- JSON array: A
- Basic trim: hello
- Trim and upper: HELLO
- Text split: a