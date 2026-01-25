using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.Handlers.Database
{
    internal static class DatabaseRegexProcessor
    {
        private const int MAX_AUTO_GROUPS = 20;
        private const int MAX_PARAMETER_LENGTH = 4000;

        public static void ProcessRegexMatch(
            Regex regex,
            string input,
            HandlerConfig handlerConfig,
            Dictionary<string, string> parameters)
        {
            if (regex == null || string.IsNullOrEmpty(input))
                return;

            try
            {
                if (!regex.IsMatch(input))
                    return;

                var match = regex.Match(input);
                if (!match.Success)
                    return;

                string matchValue = match.Value;
                if (matchValue.Length > MAX_PARAMETER_LENGTH)
                {
                    matchValue = matchValue.Substring(0, MAX_PARAMETER_LENGTH);
                    UserFeedback.ShowWarning($"DatabaseHandler '{handlerConfig.Name}': Match value truncated to {MAX_PARAMETER_LENGTH} characters");
                }
                parameters["p_match"] = matchValue;

                if (handlerConfig.Groups != null && handlerConfig.Groups.Count > 0)
                {
                    for (int i = 0; i < handlerConfig.Groups.Count; i++)
                    {
                        var groupName = handlerConfig.Groups[i];
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
                            UserFeedback.ShowWarning($"DatabaseHandler '{handlerConfig.Name}': Group '{groupName}' value truncated to {MAX_PARAMETER_LENGTH} characters");
                        }
                        parameters[groupName] = groupValue;
                    }
                }
                else
                {
                    int maxGroups = Math.Min(match.Groups.Count - 1, MAX_AUTO_GROUPS);
                    for (int i = 1; i <= maxGroups; i++)
                    {
                        var groupValue = match.Groups[i].Value;
                        if (groupValue.Length > MAX_PARAMETER_LENGTH)
                        {
                            groupValue = groupValue.Substring(0, MAX_PARAMETER_LENGTH);
                            UserFeedback.ShowWarning($"DatabaseHandler '{handlerConfig.Name}': Group {i} value truncated to {MAX_PARAMETER_LENGTH} characters");
                        }
                        parameters[$"p_group_{i}"] = groupValue;
                    }

                    if (match.Groups.Count - 1 > MAX_AUTO_GROUPS)
                    {
                        UserFeedback.ShowWarning($"DatabaseHandler '{handlerConfig.Name}': Only first {MAX_AUTO_GROUPS} groups added. Total groups: {match.Groups.Count - 1}");
                    }
                }
            }
            catch
            {
                // Ignore regex issues; CanHandle already handles timeouts/invalid patterns
            }
        }
    }
}
