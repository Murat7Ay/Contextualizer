using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public class ClipboardContent
    {
        public bool Success { get; set; }
        public bool IsText { get; set; }
        public bool IsFile { get; set; }
        public string Text { get; set; }
        public string[] Files { get; set; }

        public ClipboardContent()
        {
            Files = new string[0];
        }

        public override string ToString()
        {
            return Success
                ? IsText
                    ? Text
                    : IsFile
                        ? string.Join(", ", Files)
                        : "Unknown content type"
                : "No content in clipboard";
        }
    }
}
