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

        public void SetBrightness(int brightnessPercent, string? targetMonitorId = null)
        {
            CurrentBrightness = brightnessPercent;
            var settings = _settingsStore.Load();
            var monitors = GetTargetMonitors(targetMonitorId);

            foreach (var monitor in monitors)
            {
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
                    // Map brightness 0-100 to software dimming 100-0%
                    int dimLevel = 100 - brightnessPercent;
                    SoftwareDimmer.SetDimLevel(monitor, dimLevel);
                }
                else
                {
                    // If hardware brightness worked, clear software dim if no explicit dimming was requested
                    if (CurrentSoftwareDim == 0)
                    {
                        SoftwareDimmer.SetDimLevel(monitor, 0);
                    }
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
                GammaController.RestoreBaselineGamma(monitor);
                SoftwareDimmer.SetDimLevel(monitor, 0);
            }
            SoftwareDimmer.ClearAll();
            LogService.Info("Restored baseline display state.");
        }

        private void ApplyToSingleMonitor(MonitorInfo monitor, int kelvin, int brightness, int red, int green, int blue, int softwareDim, int? contrast)
        {
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
