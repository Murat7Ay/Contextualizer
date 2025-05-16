using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WpfInteractionApp
{
    public interface IDynamicScreen 
    {
        public string ScreenId { get; }
        public void SetScreenInformation(Dictionary<string, string> context);
    }
}
