using EyeFocus.Display.Native;
using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface IGammaController
    {
        bool ApplyColorSettings(MonitorInfo monitor, int kelvin, int redPercent, int greenPercent, int bluePercent, int? contrastPercent = null);
        bool RestoreBaselineGamma(MonitorInfo monitor);
        bool ResetToIdentityGamma(MonitorInfo monitor);
        GdiNative.RgbRamp GenerateGammaRamp(int kelvin, int redPercent, int greenPercent, int bluePercent, int? contrastPercent = null);
        (double R, double G, double B) KelvinToRgbMultipliers(int kelvin);
    }
}
