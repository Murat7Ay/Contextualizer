using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    internal class PluginServiceProviderImp : IPluginServiceProvider
    {
        public T GetService<T>() where T : class
        {
            return ServiceLocator.Get<T>();
        }
    }
}
