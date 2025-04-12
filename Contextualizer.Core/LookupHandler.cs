using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class LookupHandler : Dispatch, IHandler
    {
        private Dictionary<string, Dictionary<string, string>> data;


        public string Name => "Lookup";

        public LookupHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            data = LoadData();
        }

        private Dictionary<string, Dictionary<string, string>> LoadData()
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                using var reader = new StreamReader(HandlerConfig.Path);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { HandlerConfig.Delimiter }, StringSplitOptions.None);
                    if (parts.Length != HandlerConfig.ValueNames.Count)
                    {
                        continue; // Skip invalid lines
                    }

                    var values = HandlerConfig.ValueNames.Zip(parts, (name, value) => new { name, value })
                        .ToDictionary(x => x.name, x => x.value);

                    foreach (var keyName in HandlerConfig.KeyNames.Where(values.ContainsKey))
                    {
                        data[values[keyName]] = values;
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>().ShowNotification($"ERROR: Failed to load data from {HandlerConfig.Path}: {ex.Message}");
            }

            return data;
        }

        protected override bool CanHandle(string input)
        {
            return data.ContainsKey(input);
        }

        bool IHandler.CanHandle(string input)
        {
            return CanHandle(input);
        }

        protected override string OutputFormat => HandlerConfig.OutputFormat;

        protected override Dictionary<string, string> CreateContext(string input)
        {
            var context = data[input];
            context[ContextKey._input] = input;
            return context;
        }

        protected override List<string> GetActions()
        {
            return HandlerConfig.Actions;
        }

        protected override Dictionary<string, List<string>> GetExecutables()
        {
            return HandlerConfig.Executables;
        }
    }
}
