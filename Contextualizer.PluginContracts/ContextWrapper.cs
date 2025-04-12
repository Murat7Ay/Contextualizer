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
        private readonly HandlerConfig _handlerConfig;

        public ContextWrapper(ReadOnlyDictionary<string, string> context, HandlerConfig handlerConfig) : base(context) 
        {
            _handlerConfig = handlerConfig;
        }

        public override string ToString()
        {
            return System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
    }
}
