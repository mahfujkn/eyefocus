using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface IDisplayEngine
    {
        IMonitorManager MonitorManager { get; }
        ISoftwareDimmer SoftwareDimmer { get; }
        IGammaController GammaController { get; }

        void ApplyProfile(DisplayProfile profile, string? targetMonitorId = null);
        void SetColorTemperature(int kelvin, string? targetMonitorId = null);
        void SetBrightness(int brightnessPercent, string? targetMonitorId = null);
        void SetSoftwareDim(int dimPercent, string? targetMonitorId = null);
        void ResetDisplay(string? targetMonitorId = null);
        void RestoreInitialState();

        int CurrentKelvin { get; }
        int CurrentBrightness { get; }
        int CurrentSoftwareDim { get; }
    }
}
