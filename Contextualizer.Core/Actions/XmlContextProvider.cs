using Contextualizer.PluginContracts;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Contextualizer.Core.Actions
{
    public class XmlContextProvider : IContextProvider
    {
        public string Name => "xmlvalidator";

        public Task<Dictionary<string, string>> CreateContext(ClipboardContent clipboardContent)
        {
            Dictionary<string, string> context = new Dictionary<string, string>();
            
            // ✅ Trim input to match validator behavior and handle whitespace/newlines
            var input = clipboardContent.Text?.Trim() ?? string.Empty;
            context[ContextKey._input] = input;

            try
            {
                XDocument xDoc = XDocument.Parse(input);
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n",
                    NewLineHandling = NewLineHandling.Replace,
                    Encoding = Encoding.UTF8
                };

                using (var stringWriter = new StringWriter())
                using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                {
                    xDoc.Save(xmlWriter);
                    context[ContextKey._formatted_output] = stringWriter.ToString();
                }
            }
            catch (XmlException ex)
            {
                context[ContextKey._error] = $"Invalid XML: {ex.Message}";
            }

            return Task.FromResult(context);
        }
    }
}

