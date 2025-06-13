using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Provides context creation functionality for XML validation operations.
    /// </summary>
    /// <remarks>This class implements the <see cref="IContextProvider"/> interface to generate a context
    /// dictionary based on clipboard content. The context is primarily used for XML validation workflows, with the
    /// input text being extracted from the clipboard content.</remarks>
    public class XmlContextProvider : IContextProvider
    {
        public string Name => "xmlvalidator";

        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(ContextKey._input, clipboardContent.Text);
            return Task.FromResult(dic);
        }
    }
}
