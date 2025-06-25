using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface ISyntheticContent
    {
        public ClipboardContent CreateSyntheticContent(UserInputRequest? userInputRequest);
    }
}
