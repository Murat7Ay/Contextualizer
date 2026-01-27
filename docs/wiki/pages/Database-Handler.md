# Database Handler

## Purpose
Executes a parameterized SQL query and produces a context result set (optionally formatted as Markdown).

## Runtime Flow
1. Validate query safety (SELECT-only; block dangerous keywords).
2. Prepare parameters from clipboard and optional regex groups.
3. Apply MCP seed context parameters if configured.
4. Execute query via Dapper.
5. Build context result set and default output format.

## Source References
- Query executor: [Contextualizer.Core/Handlers/Database/DatabaseQueryExecutor.cs](Contextualizer.Core/Handlers/Database/DatabaseQueryExecutor.cs)
- Parameter builder: [Contextualizer.Core/Handlers/Database/DatabaseParameterBuilder.cs](Contextualizer.Core/Handlers/Database/DatabaseParameterBuilder.cs)
- Safety validator: [Contextualizer.Core/Handlers/Database/DatabaseSafetyValidator.cs](Contextualizer.Core/Handlers/Database/DatabaseSafetyValidator.cs)
- Markdown formatter: [Contextualizer.Core/Handlers/Database/DatabaseMarkdownFormatter.cs](Contextualizer.Core/Handlers/Database/DatabaseMarkdownFormatter.cs)

## Key Behaviors
- Input values are truncated for safety (max length).
- Regex groups map into query parameters.
- Only SELECT statements are allowed.

## Related
- Handler entry: [Contextualizer.Core/DatabaseHandler.cs](Contextualizer.Core/DatabaseHandler.cs)