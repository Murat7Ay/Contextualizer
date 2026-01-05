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

        /// <summary>
        /// Optional programmatic seed context (e.g., MCP tool arguments).
        /// This is NOT the final handler execution context; it is an input/argument bag intended to be available
        /// to handlers/validators/providers before they build their own context.
        /// </summary>
        public Dictionary<string, string>? SeedContext { get; set; }

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
