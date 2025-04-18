using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    internal static class HandlerLoader
    {
        internal static List<IHandler> Load(string configPath)
        {
            string json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<JsonDocument>(json);

            List<IHandler> handlers = new List<IHandler>();

            foreach (var handlerConfigElement in config.RootElement.GetProperty("handlers").EnumerateArray())
            {
                var handlerConfig = JsonSerializer.Deserialize<HandlerConfig>(handlerConfigElement.ToString());

                var handler = HandlerFactory.Create(handlerConfig);
                if (handler != null)
                    handlers.Add(handler);
            }

            return handlers;
        }
    }
}
