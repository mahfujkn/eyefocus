using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using EyeFocus.Display.Native;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class MonitorManager : IMonitorManager
    {
        private readonly ICapabilityDetector _capabilityDetector;
        private readonly List<MonitorInfo> _monitors = new();
        private readonly object _lock = new();

        public event EventHandler? MonitorsChanged;

        public MonitorManager(ICapabilityDetector capabilityDetector)
        {
            _capabilityDetector = capabilityDetector;
            RefreshMonitors();
        }

        public IReadOnlyList<MonitorInfo> GetMonitors()
        {
            lock (_lock)
            {
                return _monitors.ToList();
            }
        }

        public MonitorInfo? GetMonitor(string stableMonitorId)
        {
            lock (_lock)
            {
                return _monitors.FirstOrDefault(m => m.StableMonitorId == stableMonitorId);
            }
        }

        public MonitorInfo? GetPrimaryMonitor()
        {
            lock (_lock)
            {
                return _monitors.FirstOrDefault(m => m.IsPrimary) ?? _monitors.FirstOrDefault();
            }
        }

        public void RefreshMonitors()
        {
            lock (_lock)
            {
                var discovered = new List<MonitorInfo>();
                int displayIndex = 1;

                User32Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref User32Native.RECT rect, IntPtr data) =>
                {
                    var mi = new User32Native.MONITORINFOEX();
                    mi.cbSize = Marshal.SizeOf(typeof(User32Native.MONITORINFOEX));

                    if (User32Native.GetMonitorInfo(hMonitor, ref mi))
                    {
                        var info = CreateMonitorInfo(hMonitor, mi, displayIndex++);
                        discovered.Add(info);
                    }
                    return true;
                }, IntPtr.Zero);

                // If Win32 enumeration didn't find any monitors (rare virtualized fallback), create fallback default
                if (discovered.Count == 0)
                {
                    discovered.Add(new MonitorInfo
                    {
                        StableMonitorId = "MONITOR_FALLBACK_PRIMARY",
                        DeviceName = @"\\.\DISPLAY1",
                        FriendlyName = "Main Display",
                        IsPrimary = true,
                        Left = 0,
                        Top = 0,
                        Width = 1920,
                        Height = 1080,
                        SupportsGamma = true,
                        SupportsSoftwareDimming = true
                    });
                }

                _monitors.Clear();
                _monitors.AddRange(discovered);
                LogService.Info($"Monitors refreshed. Found {_monitors.Count} display(s).");
            }

            MonitorsChanged?.Invoke(this, EventArgs.Empty);
        }

        private MonitorInfo CreateMonitorInfo(IntPtr hMonitor, User32Native.MONITORINFOEX mi, int index)
        {
            var deviceName = mi.szDevice;
            var isPrimary = (mi.dwFlags & User32Native.MONITORINFOF_PRIMARY) != 0;

            var monitor = new MonitorInfo
            {
                DeviceName = deviceName,
                DisplayIndex = index,
                IsPrimary = isPrimary,
                Left = mi.rcMonitor.Left,
                Top = mi.rcMonitor.Top,
                Width = mi.rcMonitor.Width,
                Height = mi.rcMonitor.Height
            };

            // Query DPI
            try
            {
                if (User32Native.GetDpiForMonitor(hMonitor, User32Native.MonitorDpiType.MDT_EFFECTIVE_DPI, out uint dpiX, out uint dpiY) == 0)
                {
                    monitor.DpiScaleX = dpiX / 96.0;
                    monitor.DpiScaleY = dpiY / 96.0;
                }
            }
            catch
            {
                monitor.DpiScaleX = 1.0;
                monitor.DpiScaleY = 1.0;
            }

            // Query EDID / Hardware device strings via EnumDisplayDevices
            PopulateDeviceDetails(monitor);

            // Probe hardware capabilities (DDC/CI, WMI, GDI Gamma)
            _capabilityDetector.ProbeCapabilities(monitor, hMonitor);

            return monitor;
        }

        private void PopulateDeviceDetails(MonitorInfo monitor)
        {
            try
            {
                var adapterDevice = new User32Native.DISPLAY_DEVICE();
                adapterDevice.cb = Marshal.SizeOf(typeof(User32Native.DISPLAY_DEVICE));

                if (User32Native.EnumDisplayDevices(monitor.DeviceName, 0, ref adapterDevice, 1))
                {
                    monitor.DeviceId = adapterDevice.DeviceID;
                    monitor.DeviceKey = adapterDevice.DeviceKey;

                    var displayString = adapterDevice.DeviceString;
                    if (!string.IsNullOrWhiteSpace(displayString))
                    {
                        monitor.FriendlyName = displayString;
                    }

                    // Parse hardware ID: e.g. DISPLAY\DEL41BB\4&...
                    if (!string.IsNullOrEmpty(adapterDevice.DeviceID))
                    {
                        var parts = adapterDevice.DeviceID.Split('\\');
                        if (parts.Length >= 2)
                        {
                            var modelCode = parts[1];
                            monitor.Model = modelCode;
                            monitor.StableMonitorId = $"{modelCode}_{parts.LastOrDefault()}";
                        }
                    }

                    // Check if internal laptop panel
                    if (adapterDevice.DeviceID.Contains("INTERNAL", StringComparison.OrdinalIgnoreCase) ||
                        adapterDevice.DeviceID.Contains("EDP", StringComparison.OrdinalIgnoreCase) ||
                        adapterDevice.DeviceID.Contains("LVDS", StringComparison.OrdinalIgnoreCase))
                    {
                        monitor.IsInternal = true;
                        monitor.FriendlyName = "Internal Display";
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"Error resolving device details for {monitor.DeviceName}: {ex.Message}");
            }

            if (string.IsNullOrEmpty(monitor.StableMonitorId))
            {
                monitor.StableMonitorId = $"MONITOR_{monitor.DeviceName.Replace(".", "").Replace(@"\", "")}_{monitor.Width}x{monitor.Height}";
            }

            if (string.IsNullOrEmpty(monitor.FriendlyName) || monitor.FriendlyName == "Display")
            {
                monitor.FriendlyName = $"Display {monitor.DisplayIndex} ({monitor.Width}x{monitor.Height})";
            }
        }
    }
}
