using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System.Collections.Generic;

namespace Contextualizer.Plugins
{
    public class JsonContextProvider : IContextProvider
    {
        public string Name => "jsonvalidator";

        public Dictionary<string, string> CreateContext(ClipboardContent clipboardContent)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(ContextKey._input, clipboardContent.Text);
            return dic;
        }
    }
} 