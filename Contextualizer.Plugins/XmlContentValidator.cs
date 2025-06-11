using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    public class XmlContentValidator : IContentValidator
    {
        public string Name => "xmlvalidator";

        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            if (string.IsNullOrEmpty(clipboardContent.Text))
            {
                return Task.FromResult(false);
            }

            var input = clipboardContent.Text.Trim();

            if (!input.StartsWith("<"))
                return Task.FromResult(false);

            //validate xml string
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(clipboardContent.Text);
                return Task.FromResult(true);
            }
            catch (System.Xml.XmlException)
            {
                return Task.FromResult(false);
            }
        }
    }
}
