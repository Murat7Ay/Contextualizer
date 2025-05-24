using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class HandlerContextProcessor
    {
        private static readonly Regex PlaceholderRegex = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);

        public void PromptUserInputsAsync(List<UserInputRequest> userInputs, Dictionary<string, string> context)
        {
            if (userInputs is null)
                return;

            foreach (var inpt in userInputs)
            {
                if (string.IsNullOrEmpty(inpt.Key))
                    continue;

                var userInput = ServiceLocator.Get<IUserInteractionService>().GetUserInput(inpt);
                if (!string.IsNullOrEmpty(userInput))
                {
                    context[inpt.Key] = userInput;
                }
            }
        }

        public void ContextSeederSeed(Dictionary<string, string> seeder, Dictionary<string, string> context)
        {
            if (seeder is null)
                return;

            foreach (var kvp in seeder)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    continue;

                var replacedValue = ReplaceDynamicValues(kvp.Value, context);
                if (replacedValue != null)
                {
                    context[kvp.Key] = replacedValue;
                }
            }
        }

        public static string ReplaceDynamicValues(string input, Dictionary<string, string> context)
        {
            if (string.IsNullOrEmpty(input) || context == null)
                return input;

            try
            {
                return PlaceholderRegex.Replace(input, match =>
                {
                    var key = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(key))
                        return match.Value;

                    return context.TryGetValue(key, out var value) ? value : match.Value;
                });
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Error replacing dynamic values: {ex.Message}");
                return input;
            }
        }
    }
}
