using Contextualizer.PluginContracts;
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
        public static string TypeName => "Lookup";
        public LookupHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            data = LoadData();
        }

        private Dictionary<string, Dictionary<string, string>> LoadData()
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                using var reader = new StreamReader(base.HandlerConfig.Path);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(new[] { base.HandlerConfig.Delimiter }, StringSplitOptions.None);
                    if (parts.Length != base.HandlerConfig.ValueNames.Count)
                    {
                        continue; // Skip invalid lines
                    }

                    var values = HandlerConfig.ValueNames.Zip(parts, (name, value) => new { name, value })
                        .ToDictionary(x => x.name, x => x.value);

                    foreach (var keyName in base.HandlerConfig.KeyNames.Where(values.ContainsKey))
                    {
                        data[values[keyName]] = values;
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.Get<IUserInteractionService>().ShowNotification($"ERROR: Failed to load data from {base.HandlerConfig.Path}: {ex.Message}");
            }

            return data;
        }

        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            return data.ContainsKey(clipboardContent.Text);
        }

        bool IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return CanHandle(clipboardContent);
        }
        public HandlerConfig HandlerConfig => base.HandlerConfig;

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            string input = clipboardContent.Text;
            var context = data[input];
            context[ContextKey._input] = input;
            return context;
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
