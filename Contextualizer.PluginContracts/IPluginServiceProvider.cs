using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public interface IPluginServiceProvider
    {
        public T GetService<T>() where T : class;
    }
}
