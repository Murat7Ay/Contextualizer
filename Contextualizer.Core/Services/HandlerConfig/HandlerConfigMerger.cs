using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Contextualizer.Core.Services.HandlerConfigOperations
{
    internal static class HandlerConfigMerger
    {
        // Used for merge/update flows where explicit null should be preserved (to allow clearing fields).
        private static readonly JsonSerializerOptions HandlersJsonOptionsIncludeNulls = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            PropertyNamingPolicy = null
        };

        public static HandlerConfig Merge(HandlerConfig existing, JsonElement updates, out List<string> updatedFields)
        {
            updatedFields = new List<string>();

            var existingJson = JsonSerializer.Serialize(existing, HandlersJsonOptionsIncludeNulls);
            var baseNode = JsonNode.Parse(existingJson) as JsonObject ?? new JsonObject();
            var updatesNode = JsonNode.Parse(updates.GetRawText()) as JsonObject ?? new JsonObject();

            // Ignore identity fields
            updatesNode.Remove("name");
            updatesNode.Remove("type");

            foreach (var kvp in updatesNode)
            {
                // JsonNode cannot be attached to multiple parents; clone to avoid "node already has a parent".
                baseNode[kvp.Key] = kvp.Value?.DeepClone();
                updatedFields.Add(kvp.Key);
            }

            // Re-apply original identity fields to be safe
            baseNode["name"] = existing.Name;
            baseNode["type"] = existing.Type;

            var mergedJson = baseNode.ToJsonString(HandlersJsonOptionsIncludeNulls);
            var merged = JsonSerializer.Deserialize<HandlerConfig>(mergedJson, HandlersJsonOptionsIncludeNulls);
            if (merged == null)
                return existing;

            // Preserve identity even if deserializer produced empty values
            merged.Name = existing.Name;
            merged.Type = existing.Type;

            return merged;
        }
    }
}
