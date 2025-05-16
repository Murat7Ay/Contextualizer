using Contextualizer.Core;
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

                    if (!regex.IsMatch(userInput))
                    {
                        System.Console.WriteLine("Invalid input format. Please follow the expected format.");
                        continue;
                    }
                }

                validInput = true;
            }

            return userInput;
        }

        public void Log(LogType notificationType, string message, DateTime? timestamp = null, string? additionalInfo = null)
        {
            System.Console.WriteLine($"[{notificationType.ToString()}]: {message}");
        }

        public async Task ShowActionableNotification(string message, string actionLabel, Action action, LogType notificationType = LogType.Info)
        {
            bool isConfirmed =  this.ShowConfirmation("Action Required", message + " " + actionLabel);

            if (isConfirmed)
            {
                action.Invoke();
            }
            else
            {
                System.Console.WriteLine("Action was not confirmed.");
            }
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


        public void ShowNotification(string message, LogType notificationType = LogType.Info, string title = "", int durationInSeconds = 5, Action? onActionClicked = null)
        {
            System.Console.WriteLine(message);
        }

        public void ShowToastMessage(string message, int duration = 3)
        {
            throw new NotImplementedException();
        }

        public void ShowWindow(string screenId, string title, Dictionary<string, string> context, List<KeyValuePair<string, Action<Dictionary<string, string>>>>? actions = null)
        {
            throw new NotImplementedException();
        }

    }
}
