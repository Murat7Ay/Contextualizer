using Contextualizer.PluginContracts;
using System.Collections.Generic;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Provides a context for JSON validation by extracting relevant data from clipboard content.
    /// </summary>
    /// <remarks>This class implements the <see cref="IContextProvider"/> interface and generates a context
    /// dictionary containing the input text from the clipboard content. The context can be used for JSON validation or
    /// other related operations.</remarks>
    public class JsonContextProvider : IContextProvider
    {
        public string Name => "jsonvalidator";

        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(ContextKey._input, clipboardContent.Text);
            return Task.FromResult(dic);
        }
    }
} 