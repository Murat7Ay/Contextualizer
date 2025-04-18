using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class ManualHandler : Dispatch, IHandler, ITriggerableHandler
    {
        public static string TypeName => "Manual";
        public ManualHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
        }
        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            return true;
        }
        public HandlerConfig HandlerConfig => base.HandlerConfig;
        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        protected override Dictionary<string, string> CreateContext(ClipboardContent clipboardContent)
        {
            return new Dictionary<string, string>();
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }

        bool IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return true;
        }
    }
}
