using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface IWmiBrightnessController
    {
        bool SetBrightness(MonitorInfo monitor, uint brightnessPercent);
        bool GetBrightness(MonitorInfo monitor, out uint brightnessPercent);
    }
}
