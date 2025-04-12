using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class Condition
    {
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = "equals";

        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("conditions")]
        public List<Condition>? Conditions { get; set; }
    }
}
