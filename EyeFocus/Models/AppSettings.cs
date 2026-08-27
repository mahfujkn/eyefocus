using System;
using System.Collections.Generic;

namespace EyeFocus.Models
{
    public class AppSettings
    {
        // General
        public string Theme { get; set; } = "System"; // "System", "Light", "Dark"
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool RememberLastProfile { get; set; } = true;
        public bool RestoreOnExit { get; set; } = true;

        // Display Strategy
        public string HardwareBrightnessPreference { get; set; } = "Automatic"; // "Automatic", "PreferHardware", "PreferSoftware"
        public bool SoftwareDimFallback { get; set; } = true;
        public bool DdcCiEnabled { get; set; } = true;
        public bool GammaFallbackEnabled { get; set; } = true;
        public string HdrBehavior { get; set; } = "Automatic"; // "Automatic", "DisableGammaOnHdr", "ForceFallback"

        // Transitions
        public bool SmoothTransitionsEnabled { get; set; } = true;
        public int DefaultTransitionDuration { get; set; } = 3; // seconds for smooth manual profile transitions

        // Quick Manual Presets
        public string DayProfileId { get; set; } = "office";
        public string NightProfileId { get; set; } = "night";

        // Automatic Day/Night Switching (Specific Scope)
        public bool AutomaticDayNightEnabled { get; set; } = false;
        public string TimeDetectionMode { get; set; } = "System"; // "System" or "Manual"
        public string ManualTimeZoneId { get; set; } = string.Empty; // Windows / IANA TimeZone ID
        public string DayStartTime { get; set; } = "06:00"; // HH:mm
        public string NightStartTime { get; set; } = "21:00"; // HH:mm

        // Active State
        public string ActiveProfileId { get; set; } = "comfort";
        public string SelectedMonitorId { get; set; } = "ALL"; // "ALL" or specific StableMonitorId

        // Advanced / Diagnostics
        public bool DebugLogging { get; set; } = false;

        // Collections
        public List<DisplayProfile> CustomProfiles { get; set; } = new();
        public List<DisplayProfile> ProfileOverrides { get; set; } = new();
        public List<MonitorProfileMapping> MonitorMappings { get; set; } = new();
        public List<HotkeyBinding> Hotkeys { get; set; } = new();
    }
}
