using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public class UserInputRequest
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("validation_regex")]
        public string? ValidationRegex { get; set; }
        [JsonPropertyName("is_required")]
        public bool IsRequired { get; set; } = true;
        [JsonPropertyName("is_selection_list")]
        public bool IsSelectionList { get; set; } = false;
        [JsonPropertyName("is_password")]
        public bool IsPassword { get; set; } = false;
        [JsonPropertyName("selection_items")]
        public List<SelectionItem>? SelectionItems { get; set; }
        [JsonPropertyName("is_multi_select")]
        public bool IsMultiSelect { get; set; } = false;
        [JsonPropertyName("is_file_picker")]
        public bool IsFilePicker { get; set; } = false;
        [JsonPropertyName("is_multi_line")]
        public bool IsMultiLine { get; set; } = false;
        [JsonPropertyName("default_value")]
        public string DefaultValue { get; set; } = string.Empty;
        [JsonPropertyName("dependent_key")]
        public string? DependentKey { get; set; } = null;
        [JsonPropertyName("dependent_selection_item_map")]
        public Dictionary<string, DependentSelectionItemMap>? DependentSelectionItemMap { get; set; } = null;

    }

    public class DependentSelectionItemMap
    {
        [JsonPropertyName("selection_items")]
        public List<SelectionItem> SelectionItems { get; set; } = new List<SelectionItem>();
        [JsonPropertyName("default_value")]
        public string DefaultValue { get; set; } = string.Empty;
    }

    public class SelectionItem
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("display")]
        public string Display { get; set; } = string.Empty;
    }

    public enum NavigationAction
    {
        Next,     // OK pressed
        Back,     // Back pressed  
        Cancel    // Cancel pressed
    }

    public class NavigationResult
    {
        public NavigationAction Action { get; set; }
        public string? Value { get; set; }
        public List<string>? SelectedValues { get; set; }
    }
}
