using System.Text.Json.Serialization;

namespace Contextualizer.Core
{
    public class ConfigAction
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("requires_confirmation")]
        public bool RequiresConfirmation { get; set; }
        [JsonPropertyName("key")]
        public string? Key { get; set; }
        [JsonPropertyName("conditions")]
        public Condition Conditions { get; set; }
        [JsonPropertyName("user_inputs")]
        public List<UserInputRequest> UserInputs { get; set; }
        [JsonPropertyName("seeder")]
        public Dictionary<string, string> Seeder { get; set; }
    }

}
