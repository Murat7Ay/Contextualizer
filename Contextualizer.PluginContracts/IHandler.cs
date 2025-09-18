using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface IHandler
    {
        static virtual string TypeName => throw new NotImplementedException();
        Task<bool> CanHandle(ClipboardContent clipboardContent);
        Task<bool> Execute(ClipboardContent clipboardContent);
        HandlerConfig HandlerConfig { get; }
    }
}
