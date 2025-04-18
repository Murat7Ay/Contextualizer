using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public static class HandlerFactory
    {
        private static readonly Dictionary<string, Type> _handlerMap;

        static HandlerFactory()
        {
            _handlerMap = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToDictionary(
                    t => (string)t.GetProperty("TypeName", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)!,
                    t => t,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        public static IHandler? Create(HandlerConfig config)
        {
            if (_handlerMap.TryGetValue(config.Type, out var handlerType))
            {
                return (IHandler?)Activator.CreateInstance(handlerType, config);
            }

            return null;
        }
    }
}
