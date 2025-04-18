using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class HandlerContextProcessor
    {
        public void PromptUserInputsAsync(List<UserInputRequest> userInputs, Dictionary<string, string> context)
        {
            if (userInputs is null)
                return;

            foreach (var inpt in userInputs)
            {
                context[inpt.Key] = ServiceLocator.Get<IUserInteractionService>().GetUserInput(inpt)!;
            }
        }

        public void ContextSeederSeed(Dictionary<string, string> seeder, Dictionary<string, string> context)
        {
            if (seeder is null)
                return;

            foreach (var kvp in seeder)
            {
                context[kvp.Key] = ReplaceDynamicValues(kvp.Value, context);
            }
        }


        public static string ReplaceDynamicValues(string input, Dictionary<string, string> context)
        {
            foreach (var kvp in context)
            {
                input = input.Replace($"$({kvp.Key})", kvp.Value);
            }
            return input;
        }
    }
}
