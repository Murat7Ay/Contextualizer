using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Contextualizer.Core
{
    public static class HandlerFactory
    {
        private static readonly Dictionary<string, Type> _handlerMap;

        static HandlerFactory()
        {
            // IMPORTANT:
            // Some loaded assemblies (especially optional plugins) may have missing dependencies at runtime.
            // Calling Assembly.GetTypes() can throw ReflectionTypeLoadException and crash the whole app if we don't handle it.
            // We must only use the loadable types and keep going.
            var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                foreach (var t in GetLoadableTypes(assembly))
                {
                    if (t == null || t.IsInterface || t.IsAbstract)
                        continue;

                    if (!typeof(IHandler).IsAssignableFrom(t))
                        continue;

                    string? typeName = null;
                    try
                    {
                        typeName = t.GetProperty("TypeName", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(typeName))
                        continue;

                    // Prefer the first seen type name to keep behavior stable.
                    if (!map.ContainsKey(typeName))
                    {
                        map[typeName] = t;
                    }
                }
            }

            _handlerMap = map;
        }

        public static IHandler? Create(HandlerConfig config)
        {
            if (_handlerMap.TryGetValue(config.Type, out var handlerType))
            {
                try
                {
                    return (IHandler?)Activator.CreateInstance(handlerType, config);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"HandlerFactory.Create failed for type '{config.Type}': {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // ex.Types may contain nulls for the types that failed to load.
                // We intentionally do not rethrow; a single bad dependency must not break the app.
                return ex.Types.Where(t => t != null)!;
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }
    }
}
