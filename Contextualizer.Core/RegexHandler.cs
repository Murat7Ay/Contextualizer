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

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            return regex.IsMatch(clipboardContent.Text);
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            string input = clipboardContent.Text;
            var match = regex.Match(input);
            var context = new Dictionary<string, string>();
            context[ContextKey._input] = input;

            if (match.Success && HandlerConfig.Groups != null)
            {
                for (int i = 0; i < HandlerConfig.Groups.Count; i++)
                {
                    // Groups[0] is always the full match, actual capturing groups start from index 1
                    var groupValue = match.Groups.Count > i + 1 ? match.Groups[i + 1].Value : match.Groups[0].Value;
                    context[HandlerConfig.Groups[i]] = groupValue;
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
