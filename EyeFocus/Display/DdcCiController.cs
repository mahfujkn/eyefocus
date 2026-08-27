using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using EyeFocus.Display.Native;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class DdcCiController : IDdcCiController
    {
        // Rate-limiting dictionary per monitor
        private readonly ConcurrentDictionary<string, DateTime> _lastCommandTimes = new();
        private readonly TimeSpan _minCommandInterval = TimeSpan.FromMilliseconds(40); // Max ~25 commands/sec to avoid DDC/CI bus flooding

        public bool SetBrightness(MonitorInfo monitor, uint brightnessPercent)
        {
            if (!monitor.SupportsDdcCi || !monitor.SupportsHardwareBrightness)
                return false;

            return ExecuteOnPhysicalMonitor(monitor, hPhys =>
            {
                // Map percentage to monitor range
                var min = monitor.HardwareBrightnessMin;
                var max = monitor.HardwareBrightnessMax;
                var target = min + (uint)((max - min) * (brightnessPercent / 100.0));
                
                var success = Dxva2Native.SetMonitorBrightness(hPhys, target);
                if (success)
                {
                    monitor.CurrentHardwareBrightness = brightnessPercent;
                    LogService.Debug($"DDC/CI Brightness set to {brightnessPercent}% ({target}) on {monitor.DeviceName}");
                }
                return success;
            });
        }

        public bool GetBrightness(MonitorInfo monitor, out uint brightnessPercent)
        {
            brightnessPercent = 0;
            if (!monitor.SupportsDdcCi || !monitor.SupportsHardwareBrightness)
                return false;

            uint current = 0;
            var success = ExecuteOnPhysicalMonitor(monitor, hPhys =>
            {
                if (Dxva2Native.GetMonitorBrightness(hPhys, out uint min, out uint cur, out uint max))
                {
                    if (max > min)
                    {
                        current = (uint)Math.Round((double)(cur - min) / (max - min) * 100.0);
                    }
                    else
                    {
                        current = cur;
                    }
                    return true;
                }
                return false;
            });

            if (success)
            {
                brightnessPercent = current;
                monitor.CurrentHardwareBrightness = current;
            }
            return success;
        }

        public bool SetContrast(MonitorInfo monitor, uint contrastPercent)
        {
            if (!monitor.SupportsDdcCi || !monitor.SupportsContrast)
                return false;

            return ExecuteOnPhysicalMonitor(monitor, hPhys =>
            {
                if (Dxva2Native.GetMonitorContrast(hPhys, out uint min, out _, out uint max))
                {
                    var target = min + (uint)((max - min) * (contrastPercent / 100.0));
                    return Dxva2Native.SetMonitorContrast(hPhys, target);
                }
                return false;
            });
        }

        public bool SetColorTemperature(MonitorInfo monitor, uint colorTempEnum)
        {
            if (!monitor.SupportsDdcCi || !monitor.SupportsHardwareColorTemperature)
                return false;

            return ExecuteOnPhysicalMonitor(monitor, hPhys =>
            {
                return Dxva2Native.SetMonitorColorTemperature(hPhys, colorTempEnum);
            });
        }

        public bool SetRgbGain(MonitorInfo monitor, uint redPercent, uint greenPercent, uint bluePercent)
        {
            if (!monitor.SupportsDdcCi || !monitor.SupportsRgb)
                return false;

            return ExecuteOnPhysicalMonitor(monitor, hPhys =>
            {
                bool ok = true;
                if (Dxva2Native.GetMonitorRedGreenOrBlueGain(hPhys, Dxva2Native.MC_RED_GAIN, out uint rMin, out _, out uint rMax))
                {
                    ok &= Dxva2Native.SetMonitorRedGreenOrBlueGain(hPhys, Dxva2Native.MC_RED_GAIN, rMin + (uint)((rMax - rMin) * (redPercent / 100.0)));
                }
                if (Dxva2Native.GetMonitorRedGreenOrBlueGain(hPhys, Dxva2Native.MC_GREEN_GAIN, out uint gMin, out _, out uint gMax))
                {
                    ok &= Dxva2Native.SetMonitorRedGreenOrBlueGain(hPhys, Dxva2Native.MC_GREEN_GAIN, gMin + (uint)((gMax - gMin) * (greenPercent / 100.0)));
                }
                if (Dxva2Native.GetMonitorRedGreenOrBlueGain(hPhys, Dxva2Native.MC_BLUE_GAIN, out uint bMin, out _, out uint bMax))
                {
                    ok &= Dxva2Native.SetMonitorRedGreenOrBlueGain(hPhys, Dxva2Native.MC_BLUE_GAIN, bMin + (uint)((bMax - bMin) * (bluePercent / 100.0)));
                }
                return ok;
            });
        }

        private bool ExecuteOnPhysicalMonitor(MonitorInfo monitor, Func<IntPtr, bool> action)
        {
            // Rate limiting
            var key = monitor.StableMonitorId;
            if (_lastCommandTimes.TryGetValue(key, out var lastTime))
            {
                var elapsed = DateTime.UtcNow - lastTime;
                if (elapsed < _minCommandInterval)
                {
                    Thread.Sleep(_minCommandInterval - elapsed);
                }
            }
            _lastCommandTimes[key] = DateTime.UtcNow;

            IntPtr hMonitor = IntPtr.Zero;

            // Find HMONITOR matching DeviceName
            User32Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr hdc, ref User32Native.RECT rect, IntPtr data) =>
            {
                var mi = new User32Native.MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(typeof(User32Native.MONITORINFOEX));
                if (User32Native.GetMonitorInfo(hMon, ref mi) && mi.szDevice == monitor.DeviceName)
                {
                    hMonitor = hMon;
                    return false; // Stop enumeration
                }
                return true;
            }, IntPtr.Zero);

            if (hMonitor == IntPtr.Zero)
                return false;

            try
            {
                if (Dxva2Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint numPhys) && numPhys > 0)
                {
                    var physMonitors = new Dxva2Native.PHYSICAL_MONITOR[numPhys];
                    if (Dxva2Native.GetPhysicalMonitorsFromHMONITOR(hMonitor, numPhys, physMonitors))
                    {
                        var result = action(physMonitors[0].hPhysicalMonitor);
                        Dxva2Native.DestroyPhysicalMonitors(numPhys, physMonitors);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"DDC/CI command execution failed on {monitor.DeviceName}: {ex.Message}");
            }

            return false;
        }
    }
}
