using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class JsonValidator : IHandlerContextProvider
    {
        public string Name => "jsonvalidator";

        public Dictionary<string, string> CreateContext(ClipboardContent clipboardContent)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(ContextKey._input, clipboardContent.Text);
            return dic;
        }

        public bool Validate(ClipboardContent clipboardContent)
        {
            if (string.IsNullOrEmpty(clipboardContent.Text))
            {
                return false;
            }

            var input = clipboardContent.Text.Trim();

            // JSON objesi veya dizisi ile başlamıyorsa geçersizdir
            if (!(input.StartsWith("{") && input.EndsWith("}")) &&
                !(input.StartsWith("[") && input.EndsWith("]")))
                return false;

            //validate the JSON string
            try
            {
                var jsonDocument = System.Text.Json.JsonDocument.Parse(clipboardContent.Text);
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }
    }
}
