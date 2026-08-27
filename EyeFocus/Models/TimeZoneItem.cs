using System;

namespace EyeFocus.Models
{
    public class TimeZoneItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string StandardName { get; set; } = string.Empty;
        public TimeSpan BaseUtcOffset { get; set; }
        public string FormattedOffset { get; set; } = "+00:00";
        public string Country { get; set; } = string.Empty;
        public string MajorCities { get; set; } = string.Empty;
        public string SearchText { get; set; } = string.Empty;

        public override string ToString() => DisplayName;
    }
}
