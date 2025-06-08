using Contextualizer.PluginContracts;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Logging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class DatabaseHandler : Dispatch, IHandler
    {
        private System.Text.RegularExpressions.Regex? regex;
        private Dictionary<string, string> parameters;
        private Dictionary<string, string> resultSet;
        public static string TypeName => "Database";
        public DatabaseHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            if (!string.IsNullOrEmpty(handlerConfig.Regex))
            {
                regex = new System.Text.RegularExpressions.Regex(handlerConfig.Regex);
            }
            resultSet = new Dictionary<string, string>();
            parameters = new Dictionary<string, string>();
        }

        protected override string OutputFormat => HandlerConfig.OutputFormat ?? GenerateMarkdownTable(resultSet);

        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText
                || string.IsNullOrEmpty(clipboardContent.Text)
                || string.IsNullOrEmpty(HandlerConfig.Query)
                || !IsSafeSqlQuery(HandlerConfig.Query)
                || string.IsNullOrEmpty(HandlerConfig.ConnectionString)
                || string.IsNullOrEmpty(HandlerConfig.Connector))
            {
                return false;
            }

            // Regex varsa ve eşleşmiyorsa false dön
            if (regex is not null)
            {
                if (!regex.IsMatch(clipboardContent.Text))
                    return false;

                string input = clipboardContent.Text;
                var match = regex.Match(input);
                parameters["p_input"] = input;

                if (match.Success)
                {
                    for (int i = 1; i <= base.HandlerConfig.Groups?.Count; i++)
                    {
                        parameters[base.HandlerConfig.Groups[i - 1]] = match.Groups[i].Value;
                    }
                }
            }
            else
            {
                parameters["p_input"] = clipboardContent.Text;
            }
            return true;
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            resultSet = new Dictionary<string, string>();

            using IDbConnection connection = CreateConnection();
            DynamicParameters dynamicParameters = CreateDynamicParameters();
            var queryResults = await connection.QueryAsync(HandlerConfig.Query, dynamicParameters, commandTimeout: 3);
            int rowCount = queryResults.Count();

            if (rowCount == 0)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string parametersJson = JsonSerializer.Serialize(parameters, options);

                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"No records found matching the criteria.{Environment.NewLine}" +
                    $"Query: {HandlerConfig.Query}{Environment.NewLine}" +
                    $"Parameters:{Environment.NewLine}{parametersJson}");
            }

            resultSet[ContextKey._count] = rowCount.ToString();
            int rowNumber = 1;
            foreach (var row in queryResults)
            {
                foreach (var property in row)
                {
                    string key = $"{property.Key}#{rowNumber}";
                    resultSet[key] = property.Value?.ToString() ?? string.Empty;
                }
                rowNumber++;
            }

            return resultSet;
        }

        private DynamicParameters CreateDynamicParameters()
        {
            string alias = GetParameterAlias();
            var dynamicParameters = new DynamicParameters();
            foreach (var item in parameters)
            {
                dynamicParameters.Add($"{alias}{item.Key}", item.Value);
            }
            return dynamicParameters;
        }

        protected override List<ConfigAction> GetActions()
        {
            return HandlerConfig.Actions;
        }

        bool IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return CanHandle(clipboardContent);
        }
        public bool IsSafeSqlQuery(string query)
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

        private IDbConnection CreateConnection()
        {
            return HandlerConfig.Connector.ToLowerInvariant() switch
            {
                "mssql" => new SqlConnection(HandlerConfig.ConnectionString),
                "plsql" => new OracleConnection(HandlerConfig.ConnectionString),
                _ => throw new NotSupportedException($"Connector type '{HandlerConfig.Connector}' is not supported.")
            };
        }

        private string GetParameterAlias()
        {
            return HandlerConfig.Connector.ToLowerInvariant() switch
            {
                "mssql" => "@",
                "plsql" => ":",
                _ => throw new NotSupportedException($"Connector type '{HandlerConfig.Connector}' is not supported.")
            };
        }

        public string GenerateMarkdownTable(Dictionary<string, string> resultSet)
        {
            if (resultSet[ContextKey._count] == "0")
            {
                return $"No data available. {parameters["p_input"]}";
            }

            // Extract unique column headers from the keys
            var headers = resultSet.Keys
                .Where(key => key.Contains("#"))
                .Select(key => key.Split('#')[0])
                .Distinct()
                .ToList();

            // Extract row numbers
            var rowNumbers = resultSet.Keys
                .Where(key => key.Contains("#"))
                .Select(key => int.Parse(key.Split('#')[1]))
                .Distinct()
                .OrderBy(row => row)
                .ToList();

            // Build the Markdown table
            var markdownBuilder = new StringBuilder();

            markdownBuilder.AppendLine($"Captured Text:{parameters["p_input"]}");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("---");

            // Add headers
            markdownBuilder.Append("| Row | ");
            markdownBuilder.Append(string.Join(" | ", headers));
            markdownBuilder.AppendLine(" |");

            // Add header separator
            markdownBuilder.Append("|---|");
            markdownBuilder.Append(string.Join("|", headers.Select(_ => "---")));
            markdownBuilder.AppendLine("|");

            // Add rows
            foreach (var rowNumber in rowNumbers)
            {
                markdownBuilder.Append($"| {rowNumber} | ");
                var rowValues = headers.Select(header =>
                {
                    var key = $"{header}#{rowNumber}";
                    return resultSet.ContainsKey(key) ? resultSet[key] : "";
                });
                markdownBuilder.Append(string.Join(" | ", rowValues));
                markdownBuilder.AppendLine(" |");
            }

            return markdownBuilder.ToString();
        }

    }
}
