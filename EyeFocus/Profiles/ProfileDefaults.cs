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
                    IconKey = "IconComfort",
                    Description = "Balanced warmth and reduced brightness for comfortable everyday viewing.",
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
                    Description = "Bright, neutral settings optimized for gameplay.",
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
                    Description = "Warm cinematic presentation with balanced brightness for media.",
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
                    IconKey = "IconOffice",
                    Description = "Neutral color temperature and moderate brightness for general productivity.",
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
                    Description = "Neutral color temperature and controlled brightness for color-sensitive work.",
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
                    Description = "Warm temperature with reduced brightness for comfortable reading.",
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
                    Description = "Balanced brightness and moderate warmth for extended coding sessions.",
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
                    Description = "Warm amber color temperature and lower brightness for dark environments.",
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
                    Description = "User-configured custom profile settings.",
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
