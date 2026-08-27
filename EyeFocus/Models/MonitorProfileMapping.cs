using System;

namespace EyeFocus.Models
{
    public class MonitorProfileMapping
    {
        public string StableMonitorId { get; set; } = string.Empty;
        public string ProfileId { get; set; } = string.Empty;
        public bool IndependentControl { get; set; } = false;
    }
}
