using System;
using System.Drawing;

namespace EyeFocus.Models
{
    public class MonitorInfo
    {
        public string StableMonitorId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty; // e.g. \\.\DISPLAY1
        public string DeviceId { get; set; } = string.Empty; // e.g. \\?\DISPLAY#DEL41BB#...
        public string DeviceKey { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = "Generic";
        public string Model { get; set; } = "Display";
        public string FriendlyName { get; set; } = "Display";
        public string Serial { get; set; } = string.Empty;
        public int DisplayIndex { get; set; } = 1;
        
        // Bounds and geometry
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public double DpiScaleX { get; set; } = 1.0;
        public double DpiScaleY { get; set; } = 1.0;
        
        // Monitor classification
        public bool IsPrimary { get; set; } = false;
        public bool IsInternal { get; set; } = false; // e.g. laptop internal display
        public bool IsConnected { get; set; } = true;
        public bool IsHdrActive { get; set; } = false;

        // Capabilities
        public bool SupportsDdcCi { get; set; } = false;
        public bool SupportsHardwareBrightness { get; set; } = false;
        public bool SupportsHardwareColorTemperature { get; set; } = false;
        public bool SupportsRgb { get; set; } = false;
        public bool SupportsContrast { get; set; } = false;
        public bool SupportsWmiBrightness { get; set; } = false;
        public bool SupportsGamma { get; set; } = true;
        public bool SupportsSoftwareDimming { get; set; } = true;

        // Min/Max hardware ranges
        public uint HardwareBrightnessMin { get; set; } = 0;
        public uint HardwareBrightnessMax { get; set; } = 100;
        public uint CurrentHardwareBrightness { get; set; } = 70;

        // Software Dimming State
        public int CurrentSoftwareDim { get; set; } = 0;

        // Display configuration overrides
        public string? AssignedProfileId { get; set; } = null;
        public bool IndependentControlEnabled { get; set; } = false;

        public string DisplayTitle => $"{FriendlyName} {(IsPrimary ? "(Primary)" : "")}".Trim();
    }
}
