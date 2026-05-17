using System;
using System.Globalization;

namespace BoardGameLeague.Models
{
    public class DateTimePickerModel
    {
        public string Name { get; set; } = string.Empty;
        public string InputId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public DateTime? Value { get; set; }
        public string Culture { get; set; } = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public string FormattedValue => Value.HasValue
            ? Culture switch
            {
                "hr" => Value.Value.ToString("dd.MM.yyyy HH:mm"),
                _ => Value.Value.ToString("MM/dd/yyyy HH:mm")
            }
            : string.Empty;
    }
}
