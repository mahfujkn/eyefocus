using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace EyeFocus.UI.Themes
{
    public static class ThemeService
    {
        private static string _currentThemeSetting = "System"; // "System", "Light", "Dark"
        private static string _activeTheme = "System"; // Actual rendered theme: "Light" or "Dark"

        public static string CurrentThemeSetting => _currentThemeSetting;
        public static string ActiveTheme => _activeTheme;

        public static event Action<string>? ThemeChanged;

        static ThemeService()
        {
            try
            {
                SystemEvents.UserPreferenceChanged += (s, e) =>
                {
                    if (_currentThemeSetting == "System")
                    {
                        ApplyTheme("System");
                    }
                };
            }
            catch
            {
                // Ignore system event hook exceptions
            }
        }

        public static void ApplyTheme(string themeSetting)
        {
            _currentThemeSetting = themeSetting switch
            {
                "Light" => "Light",
                "Dark" => "Dark",
                _ => "System"
            };

            if (_currentThemeSetting == "System")
            {
                _activeTheme = GetSystemTheme();
            }
            else
            {
                _activeTheme = _currentThemeSetting;
            }

            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.Invoke(() =>
            {
                if (_activeTheme == "Light")
                {
                    // Clean Windows Desktop Utility Light Theme
                    ApplyPalette(
                        app,
                        primary: "#00808F",
                        onPrimary: "#FFFFFF",
                        primaryContainer: "#E0F2FE",
                        onPrimaryContainer: "#004D40",
                        secondary: "#475569",
                        onSecondary: "#FFFFFF",
                        secondaryContainer: "#F1F5F9",
                        onSecondaryContainer: "#0F172A",
                        tertiary: "#0284C7",
                        onTertiary: "#FFFFFF",
                        tertiaryContainer: "#E0F2FE",
                        onTertiaryContainer: "#0369A1",
                        surface: "#F8FAFC",
                        surfaceDim: "#F1F5F9",
                        surfaceBright: "#FFFFFF",
                        surfaceContainerLowest: "#FFFFFF",
                        surfaceContainerLow: "#FFFFFF",
                        surfaceContainer: "#F1F5F9",
                        surfaceContainerHigh: "#E2E8F0",
                        surfaceContainerHighest: "#CBD5E1",
                        onSurface: "#0F172A",
                        onSurfaceVariant: "#334155",
                        outline: "#64748B",
                        outlineVariant: "#CBD5E1",
                        error: "#DC2626",
                        onError: "#FFFFFF",
                        errorContainer: "#FEE2E2",
                        onErrorContainer: "#7F1D1D",
                        success: "#00808F",
                        warmAccent: "#D97706",
                        coolAccent: "#00808F",
                        nightAccent: "#0284C7"
                    );
                }
                else
                {
                    // EyeFocus Deep Glassmorphism Dark Theme
                    ApplyPalette(
                        app,
                        primary: "#2DD4BF",
                        onPrimary: "#04211E",
                        primaryContainer: "#134E4A",
                        onPrimaryContainer: "#99F6E4",
                        secondary: "#97A3AD",
                        onSecondary: "#0A0D12",
                        secondaryContainer: "#161D26",
                        onSecondaryContainer: "#EAF0F4",
                        tertiary: "#38BDF8",
                        onTertiary: "#0A0D12",
                        tertiaryContainer: "#0C4A6E",
                        onTertiaryContainer: "#E0F2FE",
                        surface: "#0A0D12",
                        surfaceDim: "#070A0E",
                        surfaceBright: "#1A212D",
                        surfaceContainerLowest: "#06080B",
                        surfaceContainerLow: "#0D1117",
                        surfaceContainer: "#131821",
                        surfaceContainerHigh: "#1C2330",
                        surfaceContainerHighest: "#263042",
                        onSurface: "#EAF0F4",
                        onSurfaceVariant: "#97A3AD",
                        outline: "#5C6772",
                        outlineVariant: "#202936",
                        error: "#F87171",
                        onError: "#450A0A",
                        errorContainer: "#7F1D1D",
                        onErrorContainer: "#FEE2E2",
                        success: "#2DD4BF",
                        warmAccent: "#F59E0B",
                        coolAccent: "#38BDF8",
                        nightAccent: "#7DD3FC"
                    );
                }

                ThemeChanged?.Invoke(_activeTheme);
            });
        }

        private static void ApplyPalette(
            System.Windows.Application app,
            string primary, string onPrimary, string primaryContainer, string onPrimaryContainer,
            string secondary, string onSecondary, string secondaryContainer, string onSecondaryContainer,
            string tertiary, string onTertiary, string tertiaryContainer, string onTertiaryContainer,
            string surface, string surfaceDim, string surfaceBright,
            string surfaceContainerLowest, string surfaceContainerLow, string surfaceContainer,
            string surfaceContainerHigh, string surfaceContainerHighest,
            string onSurface, string onSurfaceVariant,
            string outline, string outlineVariant,
            string error, string onError, string errorContainer, string onErrorContainer,
            string success, string warmAccent, string coolAccent, string nightAccent)
        {
            SetToken(app, "Primary", primary);
            SetToken(app, "OnPrimary", onPrimary);
            SetToken(app, "PrimaryContainer", primaryContainer);
            SetToken(app, "OnPrimaryContainer", onPrimaryContainer);

            SetToken(app, "Secondary", secondary);
            SetToken(app, "OnSecondary", onSecondary);
            SetToken(app, "SecondaryContainer", secondaryContainer);
            SetToken(app, "OnSecondaryContainer", onSecondaryContainer);

            SetToken(app, "Tertiary", tertiary);
            SetToken(app, "OnTertiary", onTertiary);
            SetToken(app, "TertiaryContainer", tertiaryContainer);
            SetToken(app, "OnTertiaryContainer", onTertiaryContainer);

            SetToken(app, "Surface", surface);
            SetToken(app, "SurfaceDim", surfaceDim);
            SetToken(app, "SurfaceBright", surfaceBright);
            SetToken(app, "SurfaceContainerLowest", surfaceContainerLowest);
            SetToken(app, "SurfaceContainerLow", surfaceContainerLow);
            SetToken(app, "SurfaceContainer", surfaceContainer);
            SetToken(app, "SurfaceContainerHigh", surfaceContainerHigh);
            SetToken(app, "SurfaceContainerHighest", surfaceContainerHighest);

            SetToken(app, "OnSurface", onSurface);
            SetToken(app, "OnSurfaceVariant", onSurfaceVariant);
            SetToken(app, "Outline", outline);
            SetToken(app, "OutlineVariant", outlineVariant);

            SetToken(app, "Error", error);
            SetToken(app, "OnError", onError);
            SetToken(app, "ErrorContainer", errorContainer);
            SetToken(app, "OnErrorContainer", onErrorContainer);

            SetToken(app, "Success", success);
            SetToken(app, "WarmAccent", warmAccent);
            SetToken(app, "CoolAccent", coolAccent);
            SetToken(app, "NightAccent", nightAccent);
        }

        private static void SetToken(System.Windows.Application app, string tokenName, string hexColor)
        {
            var col = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
            app.Resources[$"{tokenName}Color"] = col;
            app.Resources[$"{tokenName}Brush"] = new SolidColorBrush(col);
        }

        private static string GetSystemTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int useLight && useLight == 1)
                {
                    return "Light";
                }
            }
            catch
            {
                // Fallback to dark if registry cannot be queried
            }
            return "Dark";
        }
    }
}
