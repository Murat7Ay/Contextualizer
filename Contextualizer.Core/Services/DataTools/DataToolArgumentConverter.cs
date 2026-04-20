using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Contextualizer.Core.Services.DataTools
{
    public static class DataToolArgumentConverter
    {
        public static Dictionary<string, object?> FromJsonObject(JsonElement element, IEnumerable<string>? excludedKeys = null)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var excluded = excludedKeys == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(excludedKeys, StringComparer.OrdinalIgnoreCase);

            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in element.EnumerateObject())
            {
                if (excluded.Contains(prop.Name))
                    continue;

                result[prop.Name] = FromJsonValue(prop.Value);
            }

            return result;
        }

        public static object? FromJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => TryReadNumber(element),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => FromJsonObject(element),
                JsonValueKind.Array => element.EnumerateArray().Select(FromJsonValue).ToList(),
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        private static object TryReadNumber(JsonElement element)
        {
            if (element.TryGetInt32(out var int32Value))
                return int32Value;

            if (element.TryGetInt64(out var int64Value))
                return int64Value;

            if (element.TryGetDecimal(out var decimalValue))
                return decimalValue;

            if (element.TryGetDouble(out var doubleValue))
                return doubleValue;

            return element.ToString();
        }
    }
}