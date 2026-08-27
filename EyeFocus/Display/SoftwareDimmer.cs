using System;
using System.Collections.Concurrent;
using System.Windows;
using EyeFocus.Models;
using EyeFocus.Storage;
using EyeFocus.UI.Overlays;

namespace EyeFocus.Display
{
    public class SoftwareDimmer : ISoftwareDimmer
    {
        private readonly IMonitorManager _monitorManager;
        private readonly ConcurrentDictionary<string, DimmerOverlayWindow> _overlays = new();
        private readonly object _lock = new();

        public SoftwareDimmer(IMonitorManager monitorManager)
        {
            _monitorManager = monitorManager;
            _monitorManager.MonitorsChanged += (s, e) => UpdateMonitorsLayout();
        }

        public void SetDimLevel(MonitorInfo monitor, int dimPercent)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                lock (_lock)
                {
                    var window = GetOrCreateOverlay(monitor);
                    window.SetDimLevel(dimPercent);
                    monitor.CurrentSoftwareDim = dimPercent;
                }
            });
        }

        public void SetDimLevelAll(int dimPercent)
        {
            var monitors = _monitorManager.GetMonitors();
            foreach (var monitor in monitors)
            {
                SetDimLevel(monitor, dimPercent);
            }
        }

        public void UpdateMonitorsLayout()
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                lock (_lock)
                {
                    var monitors = _monitorManager.GetMonitors();
                    foreach (var monitor in monitors)
                    {
                        var window = GetOrCreateOverlay(monitor);
                        window.UpdateBoundsAndDpi(monitor);
                    }
                }
            });
        }

        private DimmerOverlayWindow GetOrCreateOverlay(MonitorInfo monitor)
        {
            var key = monitor.StableMonitorId;
            if (!_overlays.TryGetValue(key, out var window) || !window.IsLoaded)
            {
                window = new DimmerOverlayWindow
                {
                    StableMonitorId = key
                };
                window.UpdateBoundsAndDpi(monitor);
                window.Show();
                _overlays[key] = window;
            }
            return window;
        }

        public void ClearAll()
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                lock (_lock)
                {
                    foreach (var kvp in _overlays)
                    {
                        try
                        {
                            kvp.Value.SetDimLevel(0);
                            kvp.Value.Close();
                        }
                        catch
                        {
                            // Ignore close errors
                        }
                    }
                    _overlays.Clear();
                }
            });
        }

        public void Dispose()
        {
            ClearAll();
        }
    }
}
