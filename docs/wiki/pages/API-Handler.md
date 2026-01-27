# API Handler

## Purpose
Executes HTTP requests based on handler configuration and produces context keys from responses.

## Runtime Flow
1. Normalize config into a unified HTTP config model.
2. Build request (URL, query, headers, body) from templates and context.
3. Apply auth rules (basic, bearer, api_key, custom).
4. Execute request with retry rules.
5. Process response (raw body, JSON flattening, output mappings).
6. Apply pagination if enabled.

## Source References
- Config normalization: [Contextualizer.Core/Handlers/Api/HttpConfigNormalizer.cs](Contextualizer.Core/Handlers/Api/HttpConfigNormalizer.cs)
- Request building: [Contextualizer.Core/Handlers/Api/HttpRequestBuilder.cs](Contextualizer.Core/Handlers/Api/HttpRequestBuilder.cs)
- Auth handling: [Contextualizer.Core/Handlers/Api/HttpAuthHandler.cs](Contextualizer.Core/Handlers/Api/HttpAuthHandler.cs)
- Retry handling: [Contextualizer.Core/Handlers/Api/HttpRetryHandler.cs](Contextualizer.Core/Handlers/Api/HttpRetryHandler.cs)
- Pagination: [Contextualizer.Core/Handlers/Api/HttpPaginationHandler.cs](Contextualizer.Core/Handlers/Api/HttpPaginationHandler.cs)
- Response processing: [Contextualizer.Core/Handlers/Api/HttpResponseProcessor.cs](Contextualizer.Core/Handlers/Api/HttpResponseProcessor.cs)

## Key Behaviors
- URL, headers, and body are template-resolved via dynamic placeholders.
- Response JSON can be flattened into context keys.
- Optional output mappings can map JSON paths to named keys.
- Response size is capped by max_bytes to avoid large payloads.

## Related
- Handler entry: [Contextualizer.Core/ApiHandler.cs](Contextualizer.Core/ApiHandler.cs)