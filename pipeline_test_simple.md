# Simple Pipeline Test - Working Examples



## JSON Processing (Simple)
- Get value: $func:{{ '{"name":"john","age":30}' | json.get(name) | string.upper }}
- Array access: $func:{{ '{"items":["a","b","c"]}' | json.get(items[0]) | string.upper }}

## Debug Tests
- Simple URL domain: $func:url.domain(https://www.example.com/path)
- Simple JSON: $func:json.get('{"name":"test"}', name)
- Pipeline URL domain: $func:{{ https://www.example.com/path | url.domain }}
- Pipeline JSON: $func:{{ '{"name":"test"}' | json.get(name) }}
- Simple string split: $func:string.split("a b c", " ")
- Pipeline string split: $func:{{ "a b c" | string.split(" ") }}
- Pipeline array access: $func:{{ "a b c" | string.split(" ") | array.get(0) }}

---

**Expected Results:**
- Basic upper: HELLO WORLD
- Trim and upper: HELLO WORLD  
- Replace and length: 15
- Multi-step: some_dirty_text
- Array last item: LAST
- Word count: 4
- First word: THE
- Last word: FOX