using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface IHardwareColorController
    {
        bool ApplyHardwareColorTemperature(MonitorInfo monitor, int kelvin);
        bool ApplyHardwareRgb(MonitorInfo monitor, int red, int green, int blue);
    }
}
