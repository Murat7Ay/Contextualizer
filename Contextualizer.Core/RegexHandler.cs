using Contextualizer.PluginContracts;
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

        public static string TypeName => "Regex";

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        public HandlerConfig HandlerConfig => base.HandlerConfig;

        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            return regex.IsMatch(clipboardContent.Text);
        }

        bool IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return CanHandle(clipboardContent);
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            string input = clipboardContent.Text;
            var match = regex.Match(input);
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = input;

            if (match.Success)
            {
                for (int i = 1; i <= base.HandlerConfig.Groups.Count; i++)
                {
                    context[base.HandlerConfig.Groups[i - 1]] = match.Groups[i].Value;
                }
            }

            return context;
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
