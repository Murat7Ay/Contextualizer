using System.Collections.Generic;
using System.Text.Json;
using Contextualizer.PluginContracts;

namespace WpfInteractionApp.Services.Mcp.McpSchemas
{
    internal static class SchemaBuilder
    {
        // UI Tool Schemas
        public static JsonElement UiConfirmSchema() => UiToolSchemas.UiConfirmSchema();
        public static JsonElement UiUserInputsSchema() => UiToolSchemas.UiUserInputsSchema();
        public static JsonElement UiNotifySchema() => UiToolSchemas.UiNotifySchema();
        public static JsonElement UiShowMarkdownSchema() => UiToolSchemas.UiShowMarkdownSchema();
        public static JsonElement UserInputsSchema(List<UserInputRequest> userInputs) => UiToolSchemas.UserInputsSchema(userInputs);

        // Management Tool Schemas
        public static JsonElement HandlersListSchema() => ManagementToolSchemas.HandlersListSchema();
        public static JsonElement HandlersGetSchema() => ManagementToolSchemas.HandlersGetSchema();
        public static JsonElement HandlerCreateSchema() => ManagementToolSchemas.HandlerCreateSchema();
        public static JsonElement HandlerUpdateSchema() => ManagementToolSchemas.HandlerUpdateSchema();
        public static JsonElement HandlerDeleteSchema() => ManagementToolSchemas.HandlerDeleteSchema();
        public static JsonElement HandlerReloadSchema() => ManagementToolSchemas.HandlerReloadSchema();
        public static JsonElement HandlerDocsSchema() => ManagementToolSchemas.HandlerDocsSchema();
        public static JsonElement ConfigGetSectionSchema() => ManagementToolSchemas.ConfigGetSectionSchema();
        public static JsonElement ConfigSetValueSchema() => ManagementToolSchemas.ConfigSetValueSchema();

        // Database Tool Schemas
        public static JsonElement DatabaseToolCreateSchema() => DatabaseToolSchemas.DatabaseToolCreateSchema();

        // Common Schemas
        public static JsonElement DefaultTextSchema() => CommonSchemas.DefaultTextSchema();
        public static JsonElement FilesSchema() => CommonSchemas.FilesSchema();
        public static JsonElement EmptyObjectSchema() => CommonSchemas.EmptyObjectSchema();
    }
}
