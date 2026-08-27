using System;
using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface IDdcCiController
    {
        bool SetBrightness(MonitorInfo monitor, uint brightnessPercent);
        bool GetBrightness(MonitorInfo monitor, out uint brightnessPercent);
        bool SetContrast(MonitorInfo monitor, uint contrastPercent);
        bool SetColorTemperature(MonitorInfo monitor, uint colorTempEnum);
        bool SetRgbGain(MonitorInfo monitor, uint redPercent, uint greenPercent, uint bluePercent);
    }
}
