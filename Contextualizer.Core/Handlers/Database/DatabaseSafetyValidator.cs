using System;
using System.Linq;

namespace Contextualizer.Core.Handlers.Database
{
    internal static class DatabaseSafetyValidator
    {
        public static bool IsSafeSqlQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            var lowerQuery = query.ToLowerInvariant().AsSpan().Trim();

            if (!lowerQuery.StartsWith("select "))
            {
                return false;
            }

            return !ContainsForbiddenContent(lowerQuery);
        }

        private static bool ContainsForbiddenContent(ReadOnlySpan<char> query)
        {
            var forbiddenKeywords = new[]
            {
                "insert ",
                "update ",
                "delete ",
                "drop ",
                "alter ",
                "create ",
                "exec ",
                "execute ",
                "truncate ",
                "merge ",
                "grant ",
                "revoke ",
                "shutdown",
                "--",
                "/*",
                "*/",
                "xp_",
                "sp_"
            };

            foreach (var keyword in forbiddenKeywords)
            {
                if (query.Contains(keyword, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return query.Contains(';');
        }
    }
}
