using Contextualizer.PluginContracts;
using Contextualizer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Contextualizer.Core.Services
{
    public sealed class HandlerConfigValidator
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

        private static readonly HashSet<string> AllowedConditionOperators = new(StringComparer.OrdinalIgnoreCase)
        {
            "and",
            "or",
            "equals",
            "not_equals",
            "greater_than",
            "less_than",
            "contains",
            "starts_with",
            "ends_with",
            "matches_regex",
            "is_empty",
            "is_not_empty",
        };

        public ValidationResult ValidateForAdd(HandlerConfig config, IReadOnlyList<HandlerConfig> existingConfigs)
        {
            var errors = ValidateCommon(config);
            errors.AddRange(ValidateTypeSpecific(config));
            errors.AddRange(ValidateActions(config));
            errors.AddRange(ValidateUserInputs(config));

            if (existingConfigs.Any(c => c.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Duplicate handler name: '{config.Name}'");
            }

            return new ValidationResult(errors);
        }

        public ValidationResult ValidateForUpdate(HandlerConfig config, IReadOnlyList<HandlerConfig> existingConfigs, string originalName)
        {
            var errors = ValidateCommon(config);
            errors.AddRange(ValidateTypeSpecific(config));
            errors.AddRange(ValidateActions(config));
            errors.AddRange(ValidateUserInputs(config));

            // Name is the identifier; it must match the original.
            if (!config.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Handler name cannot be changed via update.");
            }

            return new ValidationResult(errors);
        }

        private static List<string> ValidateCommon(HandlerConfig config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Handler config is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(config.Name))
                errors.Add("name is required.");

            if (string.IsNullOrWhiteSpace(config.Type))
                errors.Add("type is required.");
            else if (!HandlerFactory.IsTypeRegistered(config.Type))
                errors.Add($"Invalid/unknown handler type: '{config.Type}'. Registered: {string.Join(", ", HandlerFactory.GetRegisteredTypeNames())}");

            return errors;
        }

        private static List<string> ValidateTypeSpecific(HandlerConfig config)
        {
            var errors = new List<string>();
            var type = (config.Type ?? string.Empty).Trim();

            // Normalize to handler TypeName values (case-insensitive).
            if (type.Equals(RegexHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(config.Regex))
                    errors.Add("regex is required for Regex handler.");
                else
                    errors.AddRange(ValidateRegexPattern(config.Regex, "regex"));
            }
            else if (type.Equals(FileHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (config.FileExtensions == null || config.FileExtensions.Count == 0)
                    errors.Add("file_extensions is required for File handler.");
            }
            else if (type.Equals(LookupHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(config.Path))
                    errors.Add("path is required for Lookup handler.");
                if (string.IsNullOrWhiteSpace(config.Delimiter))
                    errors.Add("delimiter is required for Lookup handler.");
                if (config.KeyNames == null || config.KeyNames.Count == 0)
                    errors.Add("key_names is required for Lookup handler.");
                if (config.ValueNames == null || config.ValueNames.Count == 0)
                    errors.Add("value_names is required for Lookup handler.");
            }
            else if (type.Equals(DatabaseHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(config.ConnectionString))
                    errors.Add("connectionString is required for Database handler.");
                if (string.IsNullOrWhiteSpace(config.Connector))
                    errors.Add("connector is required for Database handler (mssql/plsql).");
                if (string.IsNullOrWhiteSpace(config.Query))
                    errors.Add("query is required for Database handler.");
                else if (!Contextualizer.Core.Handlers.Database.DatabaseSafetyValidator.IsSafeSqlQuery(HandlerContextProcessor.ReplaceDynamicValues(config.Query, new Dictionary<string, string>())))
                    errors.Add("query is not safe (SELECT-only policy).");

                if (!string.IsNullOrWhiteSpace(config.Regex))
                    errors.AddRange(ValidateRegexPattern(config.Regex, "regex"));
            }
            else if (type.Equals(ApiHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                var httpUrl = config.Http?.Request?.Url;
                if (string.IsNullOrWhiteSpace(httpUrl) && string.IsNullOrWhiteSpace(config.Url))
                {
                    errors.Add("url is required for Api handler.");
                }
                else
                {
                    var urlToValidate = !string.IsNullOrWhiteSpace(httpUrl) ? httpUrl : config.Url;
                    errors.AddRange(ValidateUrl(urlToValidate));
                }

                if (!string.IsNullOrWhiteSpace(config.Regex))
                    errors.AddRange(ValidateRegexPattern(config.Regex, "regex"));
            }
            else if (type.Equals(CustomHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(config.Validator) && string.IsNullOrWhiteSpace(config.ContextProvider))
                    errors.Add("custom handler requires validator or context_provider.");
            }
            else if (type.Equals(SyntheticHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(config.ReferenceHandler) && string.IsNullOrWhiteSpace(config.ActualType))
                    errors.Add("synthetic handler requires reference_handler or actual_type.");
            }
            else if (type.Equals(CronHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(config.CronJobId))
                    errors.Add("cron_job_id is required for Cron handler.");
                if (string.IsNullOrWhiteSpace(config.CronExpression))
                    errors.Add("cron_expression is required for Cron handler.");
                if (string.IsNullOrWhiteSpace(config.ActualType))
                    errors.Add("actual_type is required for Cron handler.");
            }
            else if (type.Equals(ManualHandler.TypeName, StringComparison.OrdinalIgnoreCase))
            {
                // no required fields
            }

            return errors;
        }

        private static List<string> ValidateActions(HandlerConfig config)
        {
            var errors = new List<string>();
            if (config.Actions == null || config.Actions.Count == 0)
                return errors;

            foreach (var a in config.Actions)
            {
                if (a == null)
                {
                    errors.Add("actions contains null item.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(a.Name))
                    errors.Add("action.name is required.");

                if (a.Conditions != null)
                {
                    errors.AddRange(ValidateConditionTree(a.Conditions, prefix: $"actions[{a.Name}].conditions"));
                }

                if (a.UserInputs != null && a.UserInputs.Count > 0)
                {
                    foreach (var ui in a.UserInputs)
                    {
                        if (ui == null) continue;
                        errors.AddRange(ValidateUserInput(ui, $"actions[{a.Name}].user_inputs"));
                    }
                }

                if (a.InnerActions != null && a.InnerActions.Count > 0)
                {
                    foreach (var inner in a.InnerActions)
                    {
                        if (inner == null) continue;
                        if (string.IsNullOrWhiteSpace(inner.Name))
                            errors.Add($"actions[{a.Name}].inner_actions[].name is required.");
                    }
                }
            }

            return errors;
        }

        private static List<string> ValidateConditionTree(Condition condition, string prefix)
        {
            var errors = new List<string>();
            if (condition == null) return errors;

            if (string.IsNullOrWhiteSpace(condition.Operator))
            {
                errors.Add($"{prefix}.operator is required.");
                return errors;
            }

            if (!AllowedConditionOperators.Contains(condition.Operator))
            {
                errors.Add($"{prefix}.operator '{condition.Operator}' is not supported.");
                return errors;
            }

            if (condition.Operator.Equals("and", StringComparison.OrdinalIgnoreCase) ||
                condition.Operator.Equals("or", StringComparison.OrdinalIgnoreCase))
            {
                if (condition.Conditions == null || condition.Conditions.Count == 0)
                    errors.Add($"{prefix}.conditions must be non-empty for '{condition.Operator}'.");
                else
                {
                    for (int i = 0; i < condition.Conditions.Count; i++)
                    {
                        var sub = condition.Conditions[i];
                        if (sub == null)
                            errors.Add($"{prefix}.conditions[{i}] is null.");
                        else
                            errors.AddRange(ValidateConditionTree(sub, $"{prefix}.conditions[{i}]"));
                    }
                }
                return errors;
            }

            // leaf conditions
            if (condition.Operator.Equals("is_empty", StringComparison.OrdinalIgnoreCase) ||
                condition.Operator.Equals("is_not_empty", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(condition.Field))
                    errors.Add($"{prefix}.field is required for '{condition.Operator}'.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(condition.Field))
                errors.Add($"{prefix}.field is required.");
            if (string.IsNullOrWhiteSpace(condition.Value))
                errors.Add($"{prefix}.value is required.");

            if (condition.Operator.Equals("matches_regex", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(condition.Value))
            {
                errors.AddRange(ValidateRegexPattern(condition.Value, $"{prefix}.value"));
            }

            return errors;
        }

        private static List<string> ValidateUserInputs(HandlerConfig config)
        {
            var errors = new List<string>();
            if (config.UserInputs == null || config.UserInputs.Count == 0)
                return errors;

            foreach (var input in config.UserInputs)
            {
                if (input == null) continue;
                errors.AddRange(ValidateUserInput(input, "user_inputs"));
            }

            return errors;
        }

        private static List<string> ValidateUserInput(UserInputRequest input, string prefix)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(input.Key))
                errors.Add($"{prefix}.key is required.");
            if (string.IsNullOrWhiteSpace(input.Title))
                errors.Add($"{prefix}.title is required.");
            if (string.IsNullOrWhiteSpace(input.Message))
                errors.Add($"{prefix}.message is required.");

            if (!string.IsNullOrWhiteSpace(input.ValidationRegex))
            {
                errors.AddRange(ValidateRegexPattern(input.ValidationRegex, $"{prefix}.validation_regex"));
            }

            if (input.IsSelectionList)
            {
                if (input.SelectionItems == null || input.SelectionItems.Count == 0)
                    errors.Add($"{prefix}.selection_items is required when is_selection_list=true.");
            }

            if (!string.IsNullOrWhiteSpace(input.ConfigTarget))
            {
                // Expected: secrets.section.key or config.section.key (3 parts)
                var parts = input.ConfigTarget.Split('.', 3);
                if (parts.Length != 3)
                    errors.Add($"{prefix}.config_target must be in format 'secrets.section.key' or 'config.section.key'.");
            }

            return errors;
        }

        private static List<string> ValidateRegexPattern(string pattern, string fieldName)
        {
            var errors = new List<string>();
            try
            {
                _ = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexTimeout);
            }
            catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
            {
                errors.Add($"{fieldName} is not a valid regex: {ex.Message}");
            }
            return errors;
        }

        private static List<string> ValidateUrl(string url)
        {
            var errors = new List<string>();
            var s = url.Trim();
            if (s.Length == 0) return errors;

            // Allow templates; best-effort validation:
            // - Replace $(key) placeholders with 'x'
            // - Replace $config:/ $file:/ $func: prefixes with 'x'
            var normalized = Regex.Replace(s, @"\$\([^)]+\)", "x");
            normalized = normalized.Replace("$config:", "x").Replace("$file:", "x").Replace("$func:", "x");

            if (!Uri.TryCreate(normalized, UriKind.RelativeOrAbsolute, out var _))
            {
                errors.Add($"url is not a valid URI format (after placeholder normalization): '{url}'");
            }
            return errors;
        }

        public sealed class ValidationResult
        {
            public ValidationResult(List<string> errors)
            {
                Errors = errors ?? new List<string>();
            }

            public List<string> Errors { get; }
            public bool Ok => Errors.Count == 0;
        }
    }
}


