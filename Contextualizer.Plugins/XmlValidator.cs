using Contextualizer.Core;
using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class XmlValidator : IHandlerContextProvider
    {
        public string Name => "xmlvalidator";

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

            if (!input.StartsWith("<"))
                return false;

            //validate xml string
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(clipboardContent.Text);
                return true;
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
        }
    }
}
