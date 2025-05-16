using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class CustomHandler : Dispatch, IHandler
    {
        public CustomHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
        }
        public static string TypeName => "Custom";
        protected override string OutputFormat => base.HandlerConfig.OutputFormat;
        public HandlerConfig HandlerConfig => base.HandlerConfig;
        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            return ServiceLocator.Get<IActionService>().GetValidator(HandlerConfig.Validator)?.Validate(clipboardContent) ?? false;
        }
        bool IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return CanHandle(clipboardContent);
        }
        protected override Dictionary<string, string> CreateContext(ClipboardContent clipboardContent)
        {
            return ServiceLocator.Get<IActionService>().GetValidator(HandlerConfig.Validator)?.CreateContext(clipboardContent) ?? new Dictionary<string, string>();
        }
        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
