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
    /// <remarks>This class implements the <see cref="IContextValidator"/> interface to validate clipboard
    /// content. It checks if the content is a well-formed XML string with at least one root element.
    /// The validation is performed asynchronously.</remarks>
    public class XmlContentValidator : IContextValidator
    {
        public string Name => "xmlvalidator";

        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
            {
                return Task.FromResult(false);
            }

            var input = clipboardContent.Text.Trim();

            // ✅ Aggressive validation: Must start with < and end with >
            // This ensures we only accept XML documents, not plain text or fragments
            if (!input.StartsWith("<") || !input.EndsWith(">"))
            {
                return Task.FromResult(false);
            }

            // ✅ Must contain at least one opening and closing tag
            // Quick check before expensive XML parsing
            int openingTagCount = input.Count(c => c == '<');
            int closingTagCount = input.Count(c => c == '>');
            
            if (openingTagCount < 1 || closingTagCount < 1 || openingTagCount != closingTagCount)
            {
                return Task.FromResult(false);
            }

            // ✅ Validate well-formed XML with proper root element
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(input);
                
                // Additional check: Must have a root element (DocumentElement is not null)
                if (doc.DocumentElement == null)
                {
                    return Task.FromResult(false);
                }
                
                return Task.FromResult(true);
            }
            catch (System.Xml.XmlException)
            {
                return Task.FromResult(false);
            }
        }
    }
}
