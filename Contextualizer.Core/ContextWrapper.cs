using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class ContextWrapper : Dictionary<string, string>
    {
        private readonly Dictionary<string, string> _context;
        private readonly HandlerConfig _handlerConfig;

        public ContextWrapper(ReadOnlyDictionary<string, string> context, HandlerConfig handlerConfig)
        {
            _context = new Dictionary<string, string>(context);
            _handlerConfig = handlerConfig;
        }

        public bool Confirmable => _handlerConfig.RequiresConfirmation;
        public Condition Conditions => _handlerConfig.Conditions;

        public override string ToString()
        {
            return System.Text.Json.JsonSerializer.Serialize(_context, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
}
