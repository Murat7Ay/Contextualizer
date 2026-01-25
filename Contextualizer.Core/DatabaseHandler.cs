using Contextualizer.PluginContracts;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Contextualizer.Core.Handlers.Database;

namespace Contextualizer.Core
{
    public class DatabaseHandler : Dispatch, IHandler
    {
        private readonly Regex? _optionalRegex;
        private Dictionary<string, string> parameters;
        private Dictionary<string, string> resultSet;

        public static string TypeName => "Database";

        private const int MAX_PARAMETER_LENGTH = 4000;

        public DatabaseHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            if (!string.IsNullOrEmpty(handlerConfig.Regex))
            {
                try
                {
                    _optionalRegex = new Regex(
                        handlerConfig.Regex,
                        RegexOptions.Compiled | RegexOptions.CultureInvariant,
                        TimeSpan.FromSeconds(5)
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

        protected override string OutputFormat => HandlerConfig.OutputFormat ?? DatabaseMarkdownFormatter.GenerateMarkdownTable(resultSet, parameters);

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

            string resolvedQuery = HandlerContextProcessor.ReplaceDynamicValues(
                HandlerConfig.Query,
                new Dictionary<string, string>()
            );

            if (!DatabaseSafetyValidator.IsSafeSqlQuery(resolvedQuery))
            {
                return false;
            }

            if (_optionalRegex != null)
            {
                try
                {
                    if (!_optionalRegex.IsMatch(clipboardContent.Text))
                        return false;

                    string input = clipboardContent.Text;
                    var match = _optionalRegex.Match(input);

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
                        string matchValue = match.Value;
                        if (matchValue.Length > MAX_PARAMETER_LENGTH)
                        {
                            matchValue = matchValue.Substring(0, MAX_PARAMETER_LENGTH);
                            UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Match value truncated to {MAX_PARAMETER_LENGTH} characters");
                        }
                        parameters["p_match"] = matchValue;

                        if (base.HandlerConfig.Groups != null && base.HandlerConfig.Groups.Count > 0)
                        {
                            for (int i = 0; i < base.HandlerConfig.Groups.Count; i++)
                            {
                                var groupName = base.HandlerConfig.Groups[i];
                                string groupValue;

                                var namedGroup = match.Groups[groupName];
                                if (namedGroup.Success)
                                {
                                    groupValue = namedGroup.Value;
                                }
                                else
                                {
                                    var groupIndex = i + 1;
                                    groupValue = match.Groups.Count > groupIndex ? match.Groups[groupIndex].Value : string.Empty;
                                }

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
                            int maxGroups = Math.Min(match.Groups.Count - 1, 20);
                            for (int i = 1; i <= maxGroups; i++)
                            {
                                var groupValue = match.Groups[i].Value;
                                if (groupValue.Length > MAX_PARAMETER_LENGTH)
                                {
                                    groupValue = groupValue.Substring(0, MAX_PARAMETER_LENGTH);
                                    UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Group {i} value truncated to {MAX_PARAMETER_LENGTH} characters");
                                }
                                parameters[$"p_group_{i}"] = groupValue;
                            }

                            if (match.Groups.Count - 1 > 20)
                            {
                                UserFeedback.ShowWarning($"DatabaseHandler '{HandlerConfig.Name}': Only first 20 groups added. Total groups: {match.Groups.Count - 1}");
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

            DatabaseParameterBuilder.PrepareParametersForExecution(
                clipboardContent,
                _optionalRegex,
                HandlerConfig,
                parameters);

            if (string.IsNullOrEmpty(HandlerConfig.Query) ||
                string.IsNullOrEmpty(HandlerConfig.ConnectionString) ||
                string.IsNullOrEmpty(HandlerConfig.Connector))
            {
                var msg = $"Database handler '{HandlerConfig.Name}': Missing required configuration (query/connectionString/connector).";
                return new Dictionary<string, string> { [ContextKey._error] = msg, [ContextKey._formatted_output] = msg };
            }

            using IDbConnection connection = DatabaseQueryExecutor.CreateConnection(
                HandlerConfig.ConnectionString,
                HandlerConfig.Connector,
                HandlerConfig);

            DynamicParameters dynamicParameters = DatabaseParameterBuilder.CreateDynamicParameters(
                parameters,
                HandlerConfig.Connector);

            IEnumerable<dynamic> queryResults;
            try
            {
                queryResults = await DatabaseQueryExecutor.ExecuteQueryAsync(
                    connection,
                    HandlerConfig.Query,
                    dynamicParameters,
                    HandlerConfig.CommandTimeoutSeconds);
            }
            catch (Exception ex)
            {
                var msg = $"Database handler '{HandlerConfig.Name}': Query execution failed - {ex.Message}";
                UserFeedback.ShowError(msg);
                return new Dictionary<string, string> { [ContextKey._error] = msg, [ContextKey._formatted_output] = msg };
            }

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

        protected override List<ConfigAction> GetActions()
        {
            return HandlerConfig.Actions;
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }
    }
}
