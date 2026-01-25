using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.Handlers.Database
{
    internal static class DatabaseMarkdownFormatter
    {
        public static string GenerateMarkdownTable(
            Dictionary<string, string> resultSet,
            Dictionary<string, string> parameters)
        {
            if (resultSet[ContextKey._count] == "0")
            {
                return $"No data available. {EscapeMarkdown(parameters["p_input"])}";
            }

            var headers = resultSet.Keys
                .Where(key => key.Contains("#"))
                .Select(key => key.Split('#')[0])
                .Distinct()
                .ToList();

            var rowNumbers = resultSet.Keys
                .Where(key => key.Contains("#"))
                .Select(key => int.Parse(key.Split('#')[1]))
                .Distinct()
                .OrderBy(row => row)
                .ToList();

            var markdownBuilder = new StringBuilder();

            markdownBuilder.AppendLine($"**Captured Text:** {EscapeMarkdown(parameters["p_input"])}");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("---");
            markdownBuilder.AppendLine();

            markdownBuilder.Append("| Row | ");
            markdownBuilder.Append(string.Join(" | ", headers.Select(h => EscapeMarkdown(h))));
            markdownBuilder.AppendLine(" |");

            markdownBuilder.Append("|---|");
            markdownBuilder.Append(string.Join("|", headers.Select(_ => "---")));
            markdownBuilder.AppendLine("|");

            foreach (var rowNumber in rowNumbers)
            {
                markdownBuilder.Append($"| {rowNumber} | ");
                var rowValues = headers.Select(header =>
                {
                    var key = $"{header}#{rowNumber}";
                    if (resultSet.ContainsKey(key))
                    {
                        var value = resultSet[key];
                        return EscapeMarkdown(value);
                    }
                    return "";
                });
                markdownBuilder.Append(string.Join(" | ", rowValues));
                markdownBuilder.AppendLine(" |");
            }

            return markdownBuilder.ToString();
        }

        private static string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = text.Replace("|", "&#124;");
            text = text.Replace("\r\n", "<br>").Replace("\n", "<br>");
            return text;
        }
    }
}
