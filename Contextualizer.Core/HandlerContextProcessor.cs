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

        public void MergeIntoContext(Dictionary<string, string>? source, Dictionary<string, string> context)
        {
            if (source == null) return;

            foreach (var (key, value) in source)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                context[key] = value;
            }
        }

        public void ContextResolve(Dictionary<string, string>? constantSeeder, Dictionary<string, string>? seeder, Dictionary<string, string> context)
        {
            if (constantSeeder is not null)
            {
                MergeIntoContext(constantSeeder, context);
            }

            if (seeder is not null)
            {
                Resolve(seeder, context);
            }
            
            foreach (var key in context.Keys)
            {
                context[key] = ReplaceDynamicValues(context[key], context);
            }
        }
        private void Resolve(Dictionary<string, string> seeder, Dictionary<string, string> context)
        {
            foreach (var key in seeder.Keys)
            {
                context[key] = ReplaceDynamicValues(seeder[key], context);
            }
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

                // First process functions ($func: calls) with placeholders intact
                input = FunctionProcessor.ProcessFunctions(input, context);

                // Then process any remaining context placeholders that weren't handled by functions
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

                return input;
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"Error replacing dynamic values: {ex.Message}");
                return input;
            }
        }
    }
}
