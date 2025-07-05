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
            if (userInputs == null || userInputs.Count == 0)
                return;

            var userInteractionService = ServiceLocator.Get<IUserInteractionService>();

            foreach (var input in userInputs)
            {
                if (string.IsNullOrWhiteSpace(input?.Key))
                    continue;

                if (!string.IsNullOrEmpty(input.DependentKey) &&
                    context.TryGetValue(input.DependentKey, out var dependentValue) &&
                    input.DependentSelectionItemMap?.TryGetValue(dependentValue, out var dependentSelection) == true)
                {
                    input.SelectionItems = dependentSelection.SelectionItems;
                    input.DefaultValue = dependentSelection.DefaultValue;
                }

                var userInput = userInteractionService.GetUserInput(input);
                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    context[input.Key] = userInput;
                }
            }
        }

        public bool PromptUserInputsWithNavigation(List<UserInputRequest> userInputs, Dictionary<string, string> context)
        {
            if (userInputs == null || userInputs.Count == 0)
                return true;

            var userInteractionService = ServiceLocator.Get<IUserInteractionService>();
            int currentIndex = 0;

            while (currentIndex < userInputs.Count)
            {
                var input = userInputs[currentIndex];
                
                if (string.IsNullOrWhiteSpace(input?.Key))
                {
                    currentIndex++;
                    continue;
                }

                // Handle dependent selection items
                if (!string.IsNullOrEmpty(input.DependentKey) &&
                    context.TryGetValue(input.DependentKey, out var dependentValue) &&
                    input.DependentSelectionItemMap?.TryGetValue(dependentValue, out var dependentSelection) == true)
                {
                    input.SelectionItems = dependentSelection.SelectionItems;
                    input.DefaultValue = dependentSelection.DefaultValue;
                }

                var result = ShowNavigationDialog(input, context, currentIndex > 0, currentIndex, userInputs.Count);

                switch (result.Action)
                {
                    case NavigationAction.Next:
                        if (!string.IsNullOrWhiteSpace(result.Value))
                        {
                            context[input.Key] = result.Value;
                        }
                        currentIndex++;
                        break;

                    case NavigationAction.Back:
                        if (currentIndex > 0)
                        {
                            currentIndex--;
                            // Remove current input from context when going back
                            context.Remove(input.Key);
                        }
                        break;

                    case NavigationAction.Cancel:
                        return false; // User cancelled entire process
                }
            }

            return true; // All inputs completed successfully
        }

        private NavigationResult ShowNavigationDialog(
            UserInputRequest request, 
            Dictionary<string, string> context, 
            bool canGoBack, 
            int currentStep, 
            int totalSteps)
        {
            var userInteractionService = ServiceLocator.Get<IUserInteractionService>();
            
            // Call the navigation method directly from interface
            return userInteractionService.GetUserInputWithNavigation(request, context, canGoBack, currentStep, totalSteps);
        }

        public void ContextConstantSeederSeed(Dictionary<string,string> constantSeeder, Dictionary<string, string> context)
        {
            if(constantSeeder is null)
            {
                return;
            }

            foreach (var kvp in constantSeeder)
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

        public void ContextSeederSeed(Dictionary<string, string> seeder, Dictionary<string, string> context)
        {
            if (seeder is null)
                return;
            var resolvedSeeder = ResolveSeeder(seeder);
            foreach (var kvp in resolvedSeeder)
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
        private Dictionary<string, string> ResolveSeeder(Dictionary<string, string> seeder)
        {
            var resolved = new Dictionary<string, string>(seeder);
            foreach (var key in seeder.Keys)
            {
                resolved[key] = ReplaceDynamicValues(seeder[key], resolved);
            }
            return resolved;
        }

        public static string ReplaceDynamicValues(string input, Dictionary<string, string> context)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                // Handle $file: prefix - read file content and then process placeholders
                if (input.StartsWith("$file:"))
                {
                    var filePath = input.Substring(6); // Remove "$file:"
                    var fileContent = File.ReadAllText(filePath);
                    input = fileContent;
                }

                // First process context placeholders
                if (context != null)
                {
                    input = PlaceholderRegex.Replace(input, match =>
                    {
                        var key = match.Groups[1].Value;
                        if (string.IsNullOrEmpty(key))
                            return match.Value;

                        return context.TryGetValue(key, out var value) ? value : match.Value;
                    });
                }

                // Then process functions ($func: calls) after placeholders are resolved
                input = FunctionProcessor.ProcessFunctions(input);

                return input;
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>().Log(LogType.Error, $"Error replacing dynamic values: {ex.Message}");
                return input;
            }
        }
    }
}
