using System;
using System.Management;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class WmiBrightnessController : IWmiBrightnessController
    {
        public bool SetBrightness(MonitorInfo monitor, uint brightnessPercent)
        {
            if (!monitor.SupportsWmiBrightness && !monitor.IsInternal)
                return false;

            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                using var results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    var targetBrightness = Math.Clamp(brightnessPercent, 0, 100);
                    using var inParams = obj.GetMethodParameters("WmiSetBrightness");
                    inParams["Timeout"] = 1; // 1 second timeout
                    inParams["Brightness"] = targetBrightness;

                    obj.InvokeMethod("WmiSetBrightness", inParams, null);
                    monitor.CurrentHardwareBrightness = targetBrightness;
                    LogService.Debug($"WMI Brightness set to {targetBrightness}% on {monitor.DeviceName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"WMI SetBrightness failed: {ex.Message}");
            }

            return false;
        }

        public bool GetBrightness(MonitorInfo monitor, out uint brightnessPercent)
        {
            brightnessPercent = 0;
            if (!monitor.SupportsWmiBrightness && !monitor.IsInternal)
                return false;

            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightness");
                using var results = searcher.Get();

                foreach (ManagementObject obj in results)
                {
                    if (obj["CurrentBrightness"] != null)
                    {
                        brightnessPercent = Convert.ToUInt32(obj["CurrentBrightness"]);
                        monitor.CurrentHardwareBrightness = brightnessPercent;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"WMI GetBrightness failed: {ex.Message}");
            }

            return false;
        }
    }
}
