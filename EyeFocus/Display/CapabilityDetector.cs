using System;
using System.Collections.Concurrent;
using System.Management;
using EyeFocus.Display.Native;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class CapabilityDetector : ICapabilityDetector
    {
        private readonly ConcurrentDictionary<string, MonitorCapabilityCache> _cache = new();

        private class MonitorCapabilityCache
        {
            public bool SupportsDdcCi { get; set; }
            public bool SupportsHardwareBrightness { get; set; }
            public bool SupportsHardwareColorTemperature { get; set; }
            public bool SupportsRgb { get; set; }
            public bool SupportsContrast { get; set; }
            public bool SupportsWmiBrightness { get; set; }
            public bool SupportsGamma { get; set; }
            public uint MinBrightness { get; set; }
            public uint MaxBrightness { get; set; }
            public uint CurrentBrightness { get; set; }
        }

        public void InvalidateCache()
        {
            _cache.Clear();
        }

        public void ProbeCapabilities(MonitorInfo monitor, IntPtr hMonitor)
        {
            var key = string.IsNullOrEmpty(monitor.StableMonitorId) ? monitor.DeviceName : monitor.StableMonitorId;
            if (_cache.TryGetValue(key, out var cached))
            {
                ApplyCachedCapabilities(monitor, cached);
                return;
            }

            var entry = new MonitorCapabilityCache();

            // 1. Probe DDC/CI via DXVA2
            if (hMonitor != IntPtr.Zero)
            {
                try
                {
                    if (Dxva2Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint numPhys) && numPhys > 0)
                    {
                        var physMonitors = new Dxva2Native.PHYSICAL_MONITOR[numPhys];
                        if (Dxva2Native.GetPhysicalMonitorsFromHMONITOR(hMonitor, numPhys, physMonitors))
                        {
                            var hPhys = physMonitors[0].hPhysicalMonitor;

                            if (Dxva2Native.GetMonitorCapabilities(hPhys, out uint caps, out uint colorTemps))
                            {
                                entry.SupportsDdcCi = true;
                                entry.SupportsHardwareBrightness = (caps & Dxva2Native.MC_CAPS_BRIGHTNESS) != 0;
                                entry.SupportsContrast = (caps & Dxva2Native.MC_CAPS_CONTRAST) != 0;
                                entry.SupportsHardwareColorTemperature = (caps & Dxva2Native.MC_CAPS_COLOR_TEMPERATURE) != 0;
                                entry.SupportsRgb = (caps & Dxva2Native.MC_CAPS_RED_GREEN_BLUE_GAIN) != 0;

                                if (entry.SupportsHardwareBrightness &&
                                    Dxva2Native.GetMonitorBrightness(hPhys, out uint minB, out uint curB, out uint maxB))
                                {
                                    entry.MinBrightness = minB;
                                    entry.CurrentBrightness = curB;
                                    entry.MaxBrightness = maxB;
                                }
                            }
                            else if (Dxva2Native.GetMonitorBrightness(hPhys, out uint minB, out uint curB, out uint maxB))
                            {
                                // Some monitors don't return full capabilities flags but respond directly to GetMonitorBrightness
                                entry.SupportsDdcCi = true;
                                entry.SupportsHardwareBrightness = true;
                                entry.MinBrightness = minB;
                                entry.CurrentBrightness = curB;
                                entry.MaxBrightness = maxB;
                            }

                            Dxva2Native.DestroyPhysicalMonitors(numPhys, physMonitors);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.Debug($"DDC/CI probe failed for {monitor.DeviceName}: {ex.Message}");
                }
            }

            // 2. Probe WMI Brightness (Internal / Laptop displays)
            if (!entry.SupportsHardwareBrightness)
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightness");
                    using var results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        var instanceName = obj["InstanceName"]?.ToString() ?? "";
                        if (instanceName.Contains(monitor.Manufacturer, StringComparison.OrdinalIgnoreCase) ||
                            instanceName.Contains(monitor.Model, StringComparison.OrdinalIgnoreCase) ||
                            monitor.IsInternal)
                        {
                            entry.SupportsWmiBrightness = true;
                            entry.SupportsHardwareBrightness = true;
                            entry.MinBrightness = 0;
                            entry.MaxBrightness = 100;
                            if (obj["CurrentBrightness"] != null)
                            {
                                entry.CurrentBrightness = Convert.ToUInt32(obj["CurrentBrightness"]);
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogService.Debug($"WMI brightness probe for {monitor.DeviceName}: {ex.Message}");
                }
            }

            // 3. Probe GDI Gamma Support
            try
            {
                var hdc = GdiNative.CreateDC("DISPLAY", monitor.DeviceName, null, IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    if (GdiNative.GetDeviceGammaRamp(hdc, out _))
                    {
                        entry.SupportsGamma = true;
                    }
                    GdiNative.DeleteDC(hdc);
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"Gamma ramp probe for {monitor.DeviceName}: {ex.Message}");
            }

            _cache[key] = entry;
            ApplyCachedCapabilities(monitor, entry);

            LogService.Info($"Capabilities probed for [{monitor.FriendlyName}]: DDC/CI={monitor.SupportsDdcCi}, HwBrightness={monitor.SupportsHardwareBrightness}, WMI={monitor.SupportsWmiBrightness}, Gamma={monitor.SupportsGamma}");
        }

        private void ApplyCachedCapabilities(MonitorInfo monitor, MonitorCapabilityCache cached)
        {
            monitor.SupportsDdcCi = cached.SupportsDdcCi;
            monitor.SupportsHardwareBrightness = cached.SupportsHardwareBrightness;
            monitor.SupportsHardwareColorTemperature = cached.SupportsHardwareColorTemperature;
            monitor.SupportsRgb = cached.SupportsRgb;
            monitor.SupportsContrast = cached.SupportsContrast;
            monitor.SupportsWmiBrightness = cached.SupportsWmiBrightness;
            monitor.SupportsGamma = cached.SupportsGamma;
            monitor.SupportsSoftwareDimming = true; // Always supported via overlay fallback

            if (cached.MaxBrightness > 0)
            {
                monitor.HardwareBrightnessMin = cached.MinBrightness;
                monitor.HardwareBrightnessMax = cached.MaxBrightness;
                monitor.CurrentHardwareBrightness = cached.CurrentBrightness;
            }
        }
    }
}
