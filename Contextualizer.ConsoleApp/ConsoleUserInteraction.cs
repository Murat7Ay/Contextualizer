using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Contextualizer.ConsoleApp
{
    internal class ConsoleUserInteraction : IUserInteractionService
    {
        public string? GetUserInput(UserInputRequest? request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request cannot be null");
            }

            string? userInput = null;
            bool validInput = false;

            while (!validInput)
            {
                System.Console.WriteLine(request.Title);  // Display title
                System.Console.WriteLine(request.Message);  // Display message

                userInput = System.Console.ReadLine()?.Trim();

                if (request.IsRequired && string.IsNullOrWhiteSpace(userInput))
                {
                    System.Console.WriteLine("Input is required. Please provide a value.");
                    continue;
                }

                // Validate input with regex if provided
                if (!string.IsNullOrEmpty(request.ValidationRegex))
                {
                    var regex = new Regex(request.ValidationRegex);

                    if (!regex.IsMatch(userInput ?? string.Empty))
                    {
                        System.Console.WriteLine("Invalid input format. Please follow the expected format.");
                        continue;
                    }
                }

                validInput = true;
            }

            return userInput;
        }

        public void ShowActivityFeedback(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            System.Console.WriteLine($"[{notificationType.ToString()}]: {message}");
        }

        [Obsolete("Use ShowActivityFeedback instead for clarity")]
        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            ShowActivityFeedback(notificationType, message, timestamp, additionalInfo);
        }

        public Task ShowActionableNotification(string message, string actionLabel, Action action, LogType notificationType = LogType.Info)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ShowConfirmation(string title, string message)
        {
            System.Console.WriteLine(title);
            System.Console.WriteLine(message);
            System.Console.WriteLine("Press Y for Yes, N for No");

            while (true)
            {
                var key = System.Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.Y:
                        return true;
                    case ConsoleKey.N:
                        return false;
                    default:
                        System.Console.WriteLine("Invalid input. Press Y or N.");
                        continue;
                }
            }
        }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            throw new NotImplementedException();
        }

        public void ShowNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action? onActionClicked = null)
        {
            System.Console.WriteLine(message);
        }

        public void ShowToastMessage(string message, int duration = 3)
        {
            throw new NotImplementedException();
        }

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null, bool autoFocus = false, bool bringToFront = false)
        {
            throw new NotImplementedException();
        }

        public NavigationResult GetUserInputWithNavigation(UserInputRequest request, Dictionary<string, string> context, bool canGoBack, int currentStep, int totalSteps)
        {
            System.Console.WriteLine($"Step {currentStep} of {totalSteps}");
            if (canGoBack)
            {
                System.Console.WriteLine("Enter 'back' to go back, 'cancel' to cancel, or provide input:");
            }
            else
            {
                System.Console.WriteLine("Enter 'cancel' to cancel, or provide input:");
            }

            var input = GetUserInput(request);
            
            if (string.IsNullOrEmpty(input))
            {
                return new NavigationResult { Action = NavigationAction.Cancel };
            }
            
            if (input.ToLower() == "back" && canGoBack)
            {
                return new NavigationResult { Action = NavigationAction.Back };
            }
            
            if (input.ToLower() == "cancel")
            {
                return new NavigationResult { Action = NavigationAction.Cancel };
            }

            return new NavigationResult 
            { 
                Action = NavigationAction.Next, 
                Value = input 
            };
        }

        public void ShowNotificationWithActions(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, params ToastAction[] actions)
        {
            System.Console.WriteLine($"[{notificationType}] {title}: {message}");
            if (actions != null && actions.Length > 0)
            {
                System.Console.WriteLine("Available actions:");
                for (int i = 0; i < actions.Length; i++)
                {
                    System.Console.WriteLine($"{i + 1}. {actions[i].Text}");
                }
            }
        }

    }
}
