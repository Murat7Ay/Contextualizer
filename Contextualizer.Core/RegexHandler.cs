using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class RegexHandler : Dispatch, IHandler
    {
        private System.Text.RegularExpressions.Regex regex;

        public RegexHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            regex = new System.Text.RegularExpressions.Regex(handlerConfig.Regex);
        }

        public string Name => "regex";

        protected override string OutputFormat => HandlerConfig.OutputFormat;

        protected override bool CanHandle(string input)
        {
            return regex.IsMatch(input);
        }

        bool IHandler.CanHandle(string input)
        {
            return CanHandle(input);
        }

        protected override Dictionary<string, string> CreateContext(string input)
        {
            var match = regex.Match(input);
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = input;

            if (match.Success)
            {
                for (int i = 1; i <= HandlerConfig.Groups.Count; i++)
                {
                    context[HandlerConfig.Groups[i - 1]] = match.Groups[i].Value;
                }
            }

            return context;
        }

        protected override List<string> GetActions()
        {
            return HandlerConfig.Actions;
        }

        protected override Dictionary<string, List<string>> GetExecutables()
        {
            return HandlerConfig.Executables;
        }
    }
}
