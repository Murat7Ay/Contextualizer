# Pipeline Array Test

## Array Processing Tests
- Get array element: $func:{{ '["first","second","third"]' | array.get(1) }}
- Get with uppercase: $func:{{ '["first","second","third"]' | array.get(1) | string.upper }}
- Get last element: $func:{{ '["first","second","third"]' | array.get(-1) | string.upper }}
- Array length: $func:{{ '["a","b","c","d"]' | array.length }}

## Array Join and Split
- Join array: $func:{{ '["item1","item2","item3"]' | array.join(",") }}
- Join then split: $func:{{ '["item1","item2","item3"]' | array.join(",") | string.split(",") | array.length }}
- Join with pipe: $func:{{ '["a","b","c"]' | array.join("|") }}

## Complex Array Processing
- Filter tags: $func:{{ '["tag_web","tag_api","tag_json"]' | array.join("|") | string.replace("tag_", "") | string.split("|") | array.get(0) }}
- Process and count: $func:{{ '["hello","world","test"]' | array.join(" ") | string.upper | string.split(" ") | array.length }}

## Expected Results
- Get array element: second
- Get with uppercase: SECOND
- Get last element: THIRD
- Array length: 4
- Join array: item1,item2,item3
- Join then split: 3
- Join with pipe: a|b|c
- Filter tags: web
- Process and count: 3