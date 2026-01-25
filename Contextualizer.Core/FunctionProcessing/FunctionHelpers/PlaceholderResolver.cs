using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Contextualizer.Core.FunctionProcessing.FunctionHelpers
{
    internal static class PlaceholderResolver
    {
        private static readonly Regex PlaceholderRegex = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);

        public static string ResolvePlaceholders(string input, Dictionary<string, string>? context)
        {
            if (string.IsNullOrEmpty(input) || context == null)
                return input;

            return PlaceholderRegex.Replace(input, match =>
            {
                var key = match.Groups[1].Value;
                if (string.IsNullOrEmpty(key))
                    return match.Value;

                return context.TryGetValue(key, out var value) ? value : match.Value;
            });
        }

        public static bool IsPlaceholderPattern(string text)
        {
            return PlaceholderRegex.IsMatch(text);
        }
    }
}
