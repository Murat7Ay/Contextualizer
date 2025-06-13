using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Plugins
{
    /// <summary>
    /// Provides functionality to validate whether clipboard content contains valid XML.
    /// </summary>
    /// <remarks>This class implements the <see cref="IContentValidator"/> interface to validate clipboard
    /// content. It checks if the content is a well-formed XML string. The validation is performed
    /// asynchronously.</remarks>
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
