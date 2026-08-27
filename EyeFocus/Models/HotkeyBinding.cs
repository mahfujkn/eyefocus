using System;
using System.Collections.Generic;

namespace EyeFocus.Models
{
    public enum HotkeyAction
    {
        BrightnessUp,
        BrightnessDown,
        TemperatureWarmer,
        TemperatureCooler,
        NextProfile,
        PreviousProfile,
        ToggleAutoMode,
        ToggleNightMode,
        ResetDisplay
    }

    public class HotkeyBinding
    {
        public HotkeyAction Action { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string Key { get; set; } = "None"; // e.g. "F11", "PageUp", "Up"
        public bool ModifiersCtrl { get; set; } = true;
        public bool ModifiersAlt { get; set; } = true;
        public bool ModifiersShift { get; set; } = false;
        public bool ModifiersWin { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        public string DisplayShortcut
        {
            get
            {
                if (Key == "None" || string.IsNullOrEmpty(Key)) return "None";
                var parts = new List<string>();
                if (ModifiersCtrl) parts.Add("Ctrl");
                if (ModifiersAlt) parts.Add("Alt");
                if (ModifiersShift) parts.Add("Shift");
                if (ModifiersWin) parts.Add("Win");
                parts.Add(Key);
                return string.Join(" + ", parts);
            }
        }

        public override string ToString() => DisplayShortcut;
    }
}
