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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class DatabaseHandler : Dispatch, IHandler
    {
        private readonly Regex? _optionalRegex;
        private Dictionary<string, string> parameters;
        private Dictionary<string, string> resultSet;
        public static string TypeName => "Database";
        
        // Constants for safe parameter limits
        private const int MAX_AUTO_GROUPS = 20; // Limit auto-discovered groups
        private const int MAX_PARAMETER_LENGTH = 4000; // SQL varchar limit
        
        public DatabaseHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            // Initialize optional regex if configured
            if (!string.IsNullOrEmpty(handlerConfig.Regex))
            {
                try
                {
                    _optionalRegex = new Regex(
                        handlerConfig.Regex,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(5) // ReDoS protection
                    );
                }
                catch (ArgumentException ex)
                {
                    UserFeedback.ShowError($"DatabaseHandler '{handlerConfig.Name}': Invalid regex pattern - {ex.Message}");
                    throw new InvalidOperationException($"Invalid regex pattern in DatabaseHandler '{handlerConfig.Name}': {handlerConfig.Regex}", ex);
                }
                catch (RegexMatchTimeoutException ex)
                {
                    UserFeedback.ShowError($"DatabaseHandler '{handlerConfig.Name}': Regex compilation timeout - {ex.Message}");
                    throw new InvalidOperationException($"Regex compilation timeout in DatabaseHandler '{handlerConfig.Name}': {handlerConfig.Regex}", ex);
                }
            }
            
            resultSet = new Dictionary<string, string>();
            parameters = new Dictionary<string, string>();
        }

        protected override string OutputFormat => HandlerConfig.OutputFormat ?? GenerateMarkdownTable(resultSet);

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText
                || string.IsNullOrEmpty(clipboardContent.Text)
                || string.IsNullOrEmpty(HandlerConfig.Query)
                || string.IsNullOrEmpty(HandlerConfig.ConnectionString)
                || string.IsNullOrEmpty(HandlerConfig.Connector))
            {
                return false;
            }

            // Resolve $file: and $config: patterns in query for validation
            string resolvedQuery = HandlerContextProcessor.ReplaceDynamicValues(
                HandlerConfig.Query, 
                new Dictionary<string, string>() // Empty context for file/config resolution
            );
            
            // Validate the resolved query
            if (!IsSafeSqlQuery(resolvedQuery))
            {
                return false;
            }

            // Process regex if configured
            if (_optionalRegex != null)
            {
                try
                {
                    if (!_optionalRegex.IsMatch(clipboardContent.Text))
                        return false;

                    string input = clipboardContent.Text;
                    var match = _optionalRegex.Match(input);
                    
                    // Truncate input if too long for SQL parameter
                    string safeInput = input.Length > MAX_PARAMETER_LENGTH 
                        ? input.Substring(0, MAX_PARAMETER_LENGTH) 
                        : input;
                    if (input.Length > MAX_PARAMETER_LENGTH)
                    {
                        UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Input truncated to {MAX_PARAMETER_LENGTH} characters");
                    }
                    parameters["p_input"] = safeInput;

                    if (match.Success)
                    {
                        // Add full match
                        string matchValue = match.Value;
                        if (matchValue.Length > MAX_PARAMETER_LENGTH)
                        {
                            matchValue = matchValue.Substring(0, MAX_PARAMETER_LENGTH);
                            UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Match value truncated to {MAX_PARAMETER_LENGTH} characters");
                        }
                        parameters["p_match"] = matchValue;
                        
                        // Process configured groups
                        if (base.HandlerConfig.Groups != null && base.HandlerConfig.Groups.Count > 0)
                        {
                            for (int i = 0; i < base.HandlerConfig.Groups.Count; i++)
                            {
                                var groupName = base.HandlerConfig.Groups[i];
                                string groupValue;

                                // Try to get named group first, then fall back to indexed group
                                var namedGroup = match.Groups[groupName];
                                if (namedGroup.Success)
                                {
                                    groupValue = namedGroup.Value;
                                }
                                else
                                {
                                    // Groups[0] is always the full match, actual capturing groups start from index 1
                                    var groupIndex = i + 1;
                                    groupValue = match.Groups.Count > groupIndex ? match.Groups[groupIndex].Value : string.Empty;
                                }

                                // Truncate configured group values if too long
                                if (groupValue.Length > MAX_PARAMETER_LENGTH)
                                {
                                    groupValue = groupValue.Substring(0, MAX_PARAMETER_LENGTH);
                                    UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Group '{groupName}' value truncated to {MAX_PARAMETER_LENGTH} characters");
                                }
                                parameters[groupName] = groupValue;
                            }
                        }
                        else
                        {
                            // If no groups configured, add captured groups with safe limits
                            int maxGroups = Math.Min(match.Groups.Count - 1, MAX_AUTO_GROUPS);
                            for (int i = 1; i <= maxGroups; i++)
                            {
                                var groupValue = match.Groups[i].Value;
                                // Truncate long values to prevent SQL parameter issues
                                if (groupValue.Length > MAX_PARAMETER_LENGTH)
                                {
                                    groupValue = groupValue.Substring(0, MAX_PARAMETER_LENGTH);
                                    UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Group {i} value truncated to {MAX_PARAMETER_LENGTH} characters");
                                }
                                parameters[$"p_group_{i}"] = groupValue;
                            }
                            
                            // Warn if we hit the limit
                            if (match.Groups.Count - 1 > MAX_AUTO_GROUPS)
                            {
                                UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Only first {MAX_AUTO_GROUPS} groups added. Total groups: {match.Groups.Count - 1}");
                            }
                        }
                    }
                }
                catch (RegexMatchTimeoutException ex)
                {
                    UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Regex match timeout for input length {clipboardContent.Text.Length}");
                    System.Diagnostics.Debug.WriteLine($"DatabaseHandler: Regex match timeout - {ex.Message}");
                    return false;
                }
                catch (ArgumentException ex)
                {
                    UserFeedback.ShowError($"DatabaseHandler '{HandlerConfig.Name}': Invalid input for regex matching - {ex.Message}");
                    return false;
                }
            }
            else
            {
                // Safe input parameter when no regex
                string input = clipboardContent.Text;
                string safeInput = input.Length > MAX_PARAMETER_LENGTH 
                    ? input.Substring(0, MAX_PARAMETER_LENGTH) 
                    : input;
                if (input.Length > MAX_PARAMETER_LENGTH)
                {
                    UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Input truncated to {MAX_PARAMETER_LENGTH} characters");
                }
                parameters["p_input"] = safeInput;
            }
            return true;
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            resultSet = new Dictionary<string, string>();

            using IDbConnection connection = CreateConnection();
            DynamicParameters dynamicParameters = CreateDynamicParameters();
            
            // Resolve $file: and $config: patterns in query
            string resolvedQuery = HandlerContextProcessor.ReplaceDynamicValues(
                HandlerConfig.Query, 
                new Dictionary<string, string>() // Empty context for file/config resolution
            );
            
            // Use configurable command timeout, default to 30 seconds instead of 3
            int commandTimeout = HandlerConfig.CommandTimeoutSeconds ?? 30;
            var queryResults = await connection.QueryAsync(resolvedQuery, dynamicParameters, commandTimeout: commandTimeout);
            int rowCount = queryResults.Count();

            if (rowCount == 0)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string parametersJson = JsonSerializer.Serialize(parameters, options);

                UserFeedback.ShowWarning($"No records found matching the criteria.{Environment.NewLine}" +
                    $"Query: {resolvedQuery}{Environment.NewLine}" +
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

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
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
            // Resolve config patterns in connection string
            var baseConnectionString = HandlerContextProcessor.ReplaceDynamicValues(
                HandlerConfig.ConnectionString, 
                new Dictionary<string, string>() // Empty context for config-only resolution
            );

            // Use ConnectionManager to ensure proper connection pooling across handlers
            return Contextualizer.PluginContracts.ConnectionManager.CreateConnection(baseConnectionString, HandlerConfig.Connector, HandlerConfig);
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
