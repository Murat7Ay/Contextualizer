using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new();

        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service {typeof(T)} not found.");
        }

        /// <summary>
        /// Safe version of Get that returns null instead of throwing exception
        /// </summary>
        public static T? SafeGet<T>() where T : class
        {
            return services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        }

        /// <summary>
        /// Safe version that executes action only if service is available
        /// </summary>
        public static void SafeExecute<T>(Action<T> action) where T : class
        {
            var service = SafeGet<T>();
            if (service != null)
            {
                try
                {
                    action(service);
                }
                catch
                {
                    // Ignore errors in safe execution to prevent cascading failures
                }
            }
        }

        public static bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }
    }
}
