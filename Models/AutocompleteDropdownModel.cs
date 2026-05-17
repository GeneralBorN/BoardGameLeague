using System;

namespace BoardGameLeague.Models
{
    public class AutocompleteDropdownModel
    {
        public string Name { get; set; } = string.Empty;
        public string InputId { get; set; } = string.Empty;
        public string HiddenInputId { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = string.Empty;
        public Guid? SelectedValue { get; set; }
    }
}
