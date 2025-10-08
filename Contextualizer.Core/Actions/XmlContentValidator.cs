using Contextualizer.PluginContracts;
using System.Xml;

namespace Contextualizer.Core.Actions
{
    public class XmlContentValidator : IContextValidator
    {
        public string Name => "xmlvalidator";

        public Task<bool> Validate(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsText || string.IsNullOrWhiteSpace(clipboardContent.Text))
            {
                return Task.FromResult(false);
            }

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(clipboardContent.Text);
                return Task.FromResult(true);
            }
            catch (XmlException)
            {
                return Task.FromResult(false);
            }
        }
    }
}

