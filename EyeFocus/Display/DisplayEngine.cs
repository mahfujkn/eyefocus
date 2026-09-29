using System;
using System.Collections.Generic;
using System.Linq;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class DisplayEngine : IDisplayEngine
    {
        public IMonitorManager MonitorManager { get; }
        public ISoftwareDimmer SoftwareDimmer { get; }
        public IGammaController GammaController { get; }
        public IDdcCiController DdcCiController { get; }
        public IWmiBrightnessController WmiBrightnessController { get; }
        public IHardwareColorController HardwareColorController { get; }
        private readonly ISettingsStore _settingsStore;

        public int CurrentKelvin { get; private set; } = 5500;
        public int CurrentBrightness { get; private set; } = 75;
        public int CurrentSoftwareDim { get; private set; } = 0;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, uint> _baselineBrightness = new();

        public DisplayEngine(
            IMonitorManager monitorManager,
            ISoftwareDimmer softwareDimmer,
            IGammaController gammaController,
            IDdcCiController ddcCiController,
            IWmiBrightnessController wmiBrightnessController,
            IHardwareColorController hardwareColorController,
            ISettingsStore settingsStore)
        {
            MonitorManager = monitorManager;
            SoftwareDimmer = softwareDimmer;
            GammaController = gammaController;
            DdcCiController = ddcCiController;
            WmiBrightnessController = wmiBrightnessController;
            HardwareColorController = hardwareColorController;
            _settingsStore = settingsStore;
        }

        public void ApplyProfile(DisplayProfile profile, string? targetMonitorId = null)
        {
            CurrentKelvin = profile.Kelvin;
            CurrentBrightness = profile.Brightness;
            CurrentSoftwareDim = profile.SoftwareDim;

            var monitors = GetTargetMonitors(targetMonitorId);

            foreach (var monitor in monitors)
            {
                ApplyToSingleMonitor(monitor, profile.Kelvin, profile.Brightness, profile.Red, profile.Green, profile.Blue, profile.SoftwareDim, profile.Contrast);
            }

            LogService.Info($"Applied profile [{profile.Name}] (Kelvin: {profile.Kelvin}K, Brightness: {profile.Brightness}%, Dim: {profile.SoftwareDim}%) to {monitors.Count} monitor(s).");
        }

        public void SetColorTemperature(int kelvin, string? targetMonitorId = null)
        {
            CurrentKelvin = kelvin;
            var monitors = GetTargetMonitors(targetMonitorId);

            foreach (var monitor in monitors)
            {
                // Try hardware color temp first if supported, otherwise fallback to GammaController
                bool hwSuccess = false;
                if (monitor.SupportsHardwareColorTemperature)
                {
                    hwSuccess = HardwareColorController.ApplyHardwareColorTemperature(monitor, kelvin);
                }

                if (!hwSuccess && monitor.SupportsGamma)
                {
                    GammaController.ApplyColorSettings(monitor, kelvin, 100, 100, 100);
                }
            }
        }

        private readonly object _hwBrightnessSync = new();
        private int _queuedBrightness;
        private string? _queuedTargetMonitorId;
        private bool _isHwBrightnessRunning;

        public void SetBrightness(int brightnessPercent, string? targetMonitorId = null)
        {
            CurrentBrightness = brightnessPercent;
            var settings = _settingsStore.Load();
            var monitors = GetTargetMonitors(targetMonitorId);

            // Fast path for monitors that only support Software Dimming fallback
            foreach (var monitor in monitors)
            {
                bool hasHardware = settings.HardwareBrightnessPreference != "PreferSoftware" &&
                                   ((monitor.SupportsDdcCi && monitor.SupportsHardwareBrightness) ||
                                    (monitor.SupportsWmiBrightness || monitor.IsInternal));

                if (!hasHardware && settings.SoftwareDimFallback)
                {
                    int dimLevel = 100 - brightnessPercent;
                    SoftwareDimmer.SetDimLevel(monitor, dimLevel);
                }
            }

            // Asynchronously dispatch hardware brightness with coalescing to keep UI 60+ FPS
            QueueHardwareBrightness(brightnessPercent, targetMonitorId);
        }

        private void QueueHardwareBrightness(int brightnessPercent, string? targetMonitorId)
        {
            lock (_hwBrightnessSync)
            {
                _queuedBrightness = brightnessPercent;
                _queuedTargetMonitorId = targetMonitorId;

                if (!_isHwBrightnessRunning)
                {
                    _isHwBrightnessRunning = true;
                    System.Threading.Tasks.Task.Run(ProcessHardwareBrightnessQueue);
                }
            }
        }

        private void ProcessHardwareBrightnessQueue()
        {
            while (true)
            {
                int targetBrightness;
                string? targetMonitorId;

                lock (_hwBrightnessSync)
                {
                    targetBrightness = _queuedBrightness;
                    targetMonitorId = _queuedTargetMonitorId;
                }

                try
                {
                    ApplyHardwareBrightnessCore(targetBrightness, targetMonitorId);
                }
                catch (Exception ex)
                {
                    LogService.Debug($"Background hardware brightness error: {ex.Message}");
                }

                lock (_hwBrightnessSync)
                {
                    if (_queuedBrightness == targetBrightness && _queuedTargetMonitorId == targetMonitorId)
                    {
                        _isHwBrightnessRunning = false;
                        break;
                    }
                }
            }
        }

        private void ApplyHardwareBrightnessCore(int brightnessPercent, string? targetMonitorId)
        {
            var settings = _settingsStore.Load();
            var monitors = GetTargetMonitors(targetMonitorId);

            foreach (var monitor in monitors)
            {
                CaptureBaselineBrightnessIfNeeded(monitor);
                bool hwSuccess = false;

                if (settings.HardwareBrightnessPreference != "PreferSoftware")
                {
                    if (monitor.SupportsDdcCi && monitor.SupportsHardwareBrightness)
                    {
                        hwSuccess = DdcCiController.SetBrightness(monitor, (uint)brightnessPercent);
                    }
                    else if (monitor.SupportsWmiBrightness || monitor.IsInternal)
                    {
                        hwSuccess = WmiBrightnessController.SetBrightness(monitor, (uint)brightnessPercent);
                    }
                }

                // If hardware brightness failed or software preference is selected, use software dimming fallback
                if (!hwSuccess && settings.SoftwareDimFallback)
                {
                    int dimLevel = 100 - brightnessPercent;
                    SoftwareDimmer.SetDimLevel(monitor, dimLevel);
                }
                else if (hwSuccess && CurrentSoftwareDim == 0)
                {
                    SoftwareDimmer.SetDimLevel(monitor, 0);
                }
            }
        }

        public void SetSoftwareDim(int dimPercent, string? targetMonitorId = null)
        {
            CurrentSoftwareDim = dimPercent;
            var monitors = GetTargetMonitors(targetMonitorId);
            foreach (var monitor in monitors)
            {
                SoftwareDimmer.SetDimLevel(monitor, dimPercent);
            }
        }

        public void ResetDisplay(string? targetMonitorId = null)
        {
            var monitors = GetTargetMonitors(targetMonitorId);
            foreach (var monitor in monitors)
            {
                // Reset Gamma
                GammaController.ResetToIdentityGamma(monitor);

                // Reset Software Dim
                SoftwareDimmer.SetDimLevel(monitor, 0);

                // Reset Brightness to 100%
                if (monitor.SupportsDdcCi && monitor.SupportsHardwareBrightness)
                {
                    DdcCiController.SetBrightness(monitor, 100);
                }
                else if (monitor.SupportsWmiBrightness || monitor.IsInternal)
                {
                    WmiBrightnessController.SetBrightness(monitor, 100);
                }
            }

            CurrentKelvin = 6500;
            CurrentBrightness = 100;
            CurrentSoftwareDim = 0;
            LogService.Info("Display reset to standard defaults.");
        }

        public void RestoreInitialState()
        {
            var monitors = MonitorManager.GetMonitors();
            foreach (var monitor in monitors)
            {
                try
                {
                    // 1. Restore baseline gamma ramp
                    GammaController.RestoreBaselineGamma(monitor);

                    // 2. Clear software dimming
                    SoftwareDimmer.SetDimLevel(monitor, 0);

                    // 3. Restore baseline hardware brightness
                    uint targetBrightness = _baselineBrightness.TryGetValue(monitor.DeviceName, out var b) ? b : 100;
                    if (targetBrightness == 0) targetBrightness = 100;

                    if (monitor.SupportsDdcCi && monitor.SupportsHardwareBrightness)
                    {
                        DdcCiController.SetBrightness(monitor, targetBrightness);
                    }
                    else if (monitor.SupportsWmiBrightness || monitor.IsInternal)
                    {
                        WmiBrightnessController.SetBrightness(monitor, targetBrightness);
                    }

                    // 4. Restore hardware color temp if supported
                    if (monitor.SupportsHardwareColorTemperature)
                    {
                        HardwareColorController.ApplyHardwareColorTemperature(monitor, 6500);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"Error restoring display state on monitor {monitor.DeviceName}", ex);
                }
            }

            SoftwareDimmer.ClearAll();
            CurrentKelvin = 6500;
            CurrentBrightness = 100;
            CurrentSoftwareDim = 0;
            LogService.Info("Restored baseline display state.");
        }

        private void CaptureBaselineBrightnessIfNeeded(MonitorInfo monitor)
        {
            if (_baselineBrightness.ContainsKey(monitor.DeviceName)) return;

            try
            {
                if (monitor.SupportsDdcCi && monitor.SupportsHardwareBrightness)
                {
                    if (DdcCiController.GetBrightness(monitor, out uint curB) && curB > 0)
                    {
                        _baselineBrightness[monitor.DeviceName] = curB;
                        LogService.Debug($"Captured baseline hardware brightness for {monitor.DeviceName}: {curB}%");
                        return;
                    }
                }
                else if (monitor.SupportsWmiBrightness || monitor.IsInternal)
                {
                    if (WmiBrightnessController.GetBrightness(monitor, out uint curB) && curB > 0)
                    {
                        _baselineBrightness[monitor.DeviceName] = curB;
                        LogService.Debug($"Captured baseline WMI brightness for {monitor.DeviceName}: {curB}%");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Debug($"Could not query baseline brightness for {monitor.DeviceName}: {ex.Message}");
            }

            _baselineBrightness[monitor.DeviceName] = 100;
        }

        private void ApplyToSingleMonitor(MonitorInfo monitor, int kelvin, int brightness, int red, int green, int blue, int softwareDim, int? contrast)
        {
            CaptureBaselineBrightnessIfNeeded(monitor);
            var settings = _settingsStore.Load();

            // 1. Color / Temperature (Hardware -> Gamma Fallback)
            bool hwColorApplied = false;
            if (monitor.SupportsHardwareColorTemperature && red == 100 && green == 100 && blue == 100)
            {
                hwColorApplied = HardwareColorController.ApplyHardwareColorTemperature(monitor, kelvin);
            }

            if (!hwColorApplied && monitor.SupportsGamma && settings.GammaFallbackEnabled)
            {
                GammaController.ApplyColorSettings(monitor, kelvin, red, green, blue, contrast);
            }

            // 2. Brightness (Hardware -> Software Fallback)
            bool hwBrightnessApplied = false;
            if (settings.HardwareBrightnessPreference != "PreferSoftware")
            {
                if (monitor.SupportsDdcCi && monitor.SupportsHardwareBrightness)
                {
                    hwBrightnessApplied = DdcCiController.SetBrightness(monitor, (uint)brightness);
                }
                else if (monitor.SupportsWmiBrightness || monitor.IsInternal)
                {
                    hwBrightnessApplied = WmiBrightnessController.SetBrightness(monitor, (uint)brightness);
                }
            }

            // 3. Software Dimming
            int effectiveDim = softwareDim;
            if (!hwBrightnessApplied && settings.SoftwareDimFallback)
            {
                // If hardware brightness couldn't apply, scale software dim to reflect requested brightness
                int brightnessDim = 100 - brightness;
                effectiveDim = Math.Max(softwareDim, brightnessDim);
            }

            SoftwareDimmer.SetDimLevel(monitor, effectiveDim);
        }

        private List<MonitorInfo> GetTargetMonitors(string? targetMonitorId)
        {
            var all = MonitorManager.GetMonitors().ToList();
            if (string.IsNullOrEmpty(targetMonitorId) || targetMonitorId == "ALL")
            {
                return all;
            }

            var selected = all.Where(m => m.StableMonitorId == targetMonitorId).ToList();
            return selected.Count > 0 ? selected : all;
        }
    }
}
