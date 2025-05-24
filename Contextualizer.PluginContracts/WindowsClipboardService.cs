using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public class WindowsClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            WindowsClipboard.SetText(text);
        }

        public Task SetTextAsync(string text, CancellationToken cancellation)
        {
            return WindowsClipboard.SetTextAsync(text, cancellation);
        }
    }
}
