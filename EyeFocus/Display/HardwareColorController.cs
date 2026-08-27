using System;
using EyeFocus.Display.Native;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class HardwareColorController : IHardwareColorController
    {
        private readonly IDdcCiController _ddcCiController;

        public HardwareColorController(IDdcCiController ddcCiController)
        {
            _ddcCiController = ddcCiController;
        }

        public bool ApplyHardwareColorTemperature(MonitorInfo monitor, int kelvin)
        {
            if (!monitor.SupportsDdcCi || !monitor.SupportsHardwareColorTemperature)
                return false;

            // Map Kelvin to closest MC_COLOR_TEMPERATURE
            uint targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_6500K;
            if (kelvin < 4500)
                targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_4000K;
            else if (kelvin < 5750)
                targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_5000K;
            else if (kelvin < 7000)
                targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_6500K;
            else if (kelvin < 7800)
                targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_7500K;
            else if (kelvin < 8700)
                targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_8200K;
            else
                targetTempEnum = Dxva2Native.MC_COLOR_TEMPERATURE_9300K;

            return _ddcCiController.SetColorTemperature(monitor, targetTempEnum);
        }

        public bool ApplyHardwareRgb(MonitorInfo monitor, int red, int green, int blue)
        {
            if (!monitor.SupportsDdcCi || !monitor.SupportsRgb)
                return false;

            return _ddcCiController.SetRgbGain(monitor, (uint)Math.Clamp(red, 0, 100), (uint)Math.Clamp(green, 0, 100), (uint)Math.Clamp(blue, 0, 100));
        }
    }
}
