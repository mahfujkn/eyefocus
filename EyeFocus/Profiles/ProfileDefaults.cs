using System.Collections.Generic;
using EyeFocus.Models;

namespace EyeFocus.Profiles
{
    public static class ProfileDefaults
    {
        public const string IdComfort = "comfort";
        public const string IdGame = "game";
        public const string IdMovie = "movie";
        public const string IdOffice = "office";
        public const string IdEditing = "editing";
        public const string IdReading = "reading";
        public const string IdCoding = "coding";
        public const string IdNight = "night";
        public const string IdCustom = "custom";

        public static List<DisplayProfile> GetDefaultProfiles()
        {
            return new List<DisplayProfile>
            {
                new DisplayProfile
                {
                    Id = IdComfort,
                    Name = "Comfort",
                    Kelvin = 4200,
                    Brightness = 40,
                    Red = 100,
                    Green = 94,
                    Blue = 84,
                    SoftwareDim = 5,
                    IconKey = "IconLeaf",
                    Description = "Balanced settings for everyday use. Reduce eye strain and stay comfortable.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdGame,
                    Name = "Game",
                    Kelvin = 6500,
                    Brightness = 100,
                    Red = 100,
                    Green = 100,
                    Blue = 100,
                    SoftwareDim = 0,
                    IconKey = "IconGame",
                    Description = "Vivid colors and higher contrast for an immersive experience.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdMovie,
                    Name = "Movie",
                    Kelvin = 4500,
                    Brightness = 65,
                    Red = 100,
                    Green = 96,
                    Blue = 88,
                    SoftwareDim = 0,
                    IconKey = "IconMovie",
                    Description = "Richer colors and dynamic contrast for a cinematic experience.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdOffice,
                    Name = "Office",
                    Kelvin = 5000,
                    Brightness = 70,
                    Red = 100,
                    Green = 98,
                    Blue = 94,
                    SoftwareDim = 0,
                    IconKey = "IconBriefcase",
                    Description = "Optimized for productivity and all-day comfort.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdEditing,
                    Name = "Editing",
                    Kelvin = 6500,
                    Brightness = 70,
                    Red = 100,
                    Green = 100,
                    Blue = 100,
                    SoftwareDim = 0,
                    IconKey = "IconPalette",
                    Description = "Accurate colors for professional photo and video work.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdReading,
                    Name = "Reading",
                    Kelvin = 4000,
                    Brightness = 55,
                    Red = 100,
                    Green = 92,
                    Blue = 78,
                    SoftwareDim = 5,
                    IconKey = "IconBook",
                    Description = "Warmer tones and reduced blue light for comfortable reading.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdCoding,
                    Name = "Coding",
                    Kelvin = 4500,
                    Brightness = 60,
                    Red = 100,
                    Green = 96,
                    Blue = 90,
                    SoftwareDim = 0,
                    IconKey = "IconCode",
                    Description = "Lower eye strain for long coding sessions.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdNight,
                    Name = "Night",
                    Kelvin = 3000,
                    Brightness = 40,
                    Red = 100,
                    Green = 85,
                    Blue = 68,
                    SoftwareDim = 10,
                    IconKey = "IconNight",
                    Description = "Warmer, softer light for comfortable evening viewing.",
                    IsBuiltIn = true
                },
                new DisplayProfile
                {
                    Id = IdCustom,
                    Name = "Custom",
                    Kelvin = 5000,
                    Brightness = 70,
                    Red = 100,
                    Green = 100,
                    Blue = 100,
                    SoftwareDim = 0,
                    IconKey = "IconTune",
                    Description = "Create and save your own display settings.",
                    IsBuiltIn = true
                }
            };
        }

        public static List<HotkeyBinding> GetDefaultHotkeys()
        {
            return new List<HotkeyBinding>
            {
                new HotkeyBinding { Action = HotkeyAction.BrightnessUp, ActionName = "Brightness Up", Key = "PageUp", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.BrightnessDown, ActionName = "Brightness Down", Key = "PageDown", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.TemperatureWarmer, ActionName = "Warmer Temperature", Key = "Down", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.TemperatureCooler, ActionName = "Cooler Temperature", Key = "Up", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.NextProfile, ActionName = "Next Profile", Key = "Right", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.PreviousProfile, ActionName = "Previous Profile", Key = "Left", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.ToggleNightMode, ActionName = "Toggle Night Mode", Key = "N", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.ToggleAutoMode, ActionName = "Toggle Day/Night Mode", Key = "A", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true },
                new HotkeyBinding { Action = HotkeyAction.ResetDisplay, ActionName = "Restore Display", Key = "R", ModifiersCtrl = true, ModifiersAlt = true, IsEnabled = true }
            };
        }
    }
}
