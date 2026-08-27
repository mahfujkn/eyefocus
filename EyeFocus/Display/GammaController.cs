using System;
using System.Collections.Concurrent;
using EyeFocus.Display.Native;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Display
{
    public class GammaController : IGammaController
    {
        private readonly ConcurrentDictionary<string, GdiNative.RgbRamp> _baselineRamps = new();

        public (double R, double G, double B) KelvinToRgbMultipliers(int kelvin)
        {
            kelvin = Math.Clamp(kelvin, 2000, 10000);
            double temp = kelvin / 100.0;
            double r, g, b;

            // Calculate Red
            if (temp <= 66)
            {
                r = 255.0;
            }
            else
            {
                r = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
            }

            // Calculate Green
            if (temp <= 66)
            {
                g = 99.4708025861 * Math.Log(temp) - 161.1195681661;
            }
            else
            {
                g = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
            }

            // Calculate Blue
            if (temp >= 66)
            {
                b = 255.0;
            }
            else if (temp <= 19)
            {
                b = 0.0;
            }
            else
            {
                b = 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;
            }

            r = Math.Clamp(r, 0.0, 255.0) / 255.0;
            g = Math.Clamp(g, 0.0, 255.0) / 255.0;
            b = Math.Clamp(b, 0.0, 255.0) / 255.0;

            return (r, g, b);
        }

        public GdiNative.RgbRamp GenerateGammaRamp(int kelvin, int redPercent, int greenPercent, int bluePercent, int? contrastPercent = null)
        {
            var (kR, kG, kB) = KelvinToRgbMultipliers(kelvin);

            double rScale = kR * (Math.Clamp(redPercent, 0, 100) / 100.0);
            double gScale = kG * (Math.Clamp(greenPercent, 0, 100) / 100.0);
            double bScale = kB * (Math.Clamp(bluePercent, 0, 100) / 100.0);

            // Contrast adjustment factor (default 50% is linear 1.0)
            double gammaPower = 1.0;
            if (contrastPercent.HasValue)
            {
                int c = Math.Clamp(contrastPercent.Value, 10, 90);
                gammaPower = Math.Pow(2.0, (50 - c) / 50.0); // 0.5 to 2.0
            }

            var ramp = new GdiNative.RgbRamp
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            ushort lastR = 0;
            ushort lastG = 0;
            ushort lastB = 0;

            for (int i = 0; i < 256; i++)
            {
                double normalized = i / 255.0;
                if (Math.Abs(gammaPower - 1.0) > 0.001)
                {
                    normalized = Math.Pow(normalized, gammaPower);
                }

                ushort rVal = (ushort)Math.Clamp(Math.Round(normalized * rScale * 65535.0), 0, 65535);
                ushort gVal = (ushort)Math.Clamp(Math.Round(normalized * gScale * 65535.0), 0, 65535);
                ushort bVal = (ushort)Math.Clamp(Math.Round(normalized * bScale * 65535.0), 0, 65535);

                // Enforce monotonic non-decreasing ramp
                if (rVal < lastR) rVal = lastR;
                if (gVal < lastG) gVal = lastG;
                if (bVal < lastB) bVal = lastB;

                ramp.Red[i] = rVal;
                ramp.Green[i] = gVal;
                ramp.Blue[i] = bVal;

                lastR = rVal;
                lastG = gVal;
                lastB = bVal;
            }

            return ramp;
        }

        public bool ApplyColorSettings(MonitorInfo monitor, int kelvin, int redPercent, int greenPercent, int bluePercent, int? contrastPercent = null)
        {
            var ramp = GenerateGammaRamp(kelvin, redPercent, greenPercent, bluePercent, contrastPercent);
            return SetMonitorGammaRamp(monitor, ref ramp);
        }

        public bool RestoreBaselineGamma(MonitorInfo monitor)
        {
            var key = monitor.DeviceName;
            if (_baselineRamps.TryGetValue(key, out var baseline))
            {
                return SetMonitorGammaRamp(monitor, ref baseline, isBaselineRestore: true);
            }
            return ResetToIdentityGamma(monitor);
        }

        public bool ResetToIdentityGamma(MonitorInfo monitor)
        {
            var identity = GdiNative.RgbRamp.CreateIdentity();
            return SetMonitorGammaRamp(monitor, ref identity, isBaselineRestore: true);
        }

        private bool SetMonitorGammaRamp(MonitorInfo monitor, ref GdiNative.RgbRamp ramp, bool isBaselineRestore = false)
        {
            try
            {
                var hdc = GdiNative.CreateDC("DISPLAY", monitor.DeviceName, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    LogService.Warn($"Failed to create DC for monitor: {monitor.DeviceName}");
                    return false;
                }

                // Capture baseline on first access
                var key = monitor.DeviceName;
                if (!_baselineRamps.ContainsKey(key) && !isBaselineRestore)
                {
                    if (GdiNative.GetDeviceGammaRamp(hdc, out var initialRamp))
                    {
                        _baselineRamps[key] = initialRamp;
                        LogService.Debug($"Captured baseline gamma ramp for {monitor.DeviceName}");
                    }
                }

                var success = GdiNative.SetDeviceGammaRamp(hdc, ref ramp);
                GdiNative.DeleteDC(hdc);

                if (!success)
                {
                    LogService.Debug($"SetDeviceGammaRamp returned false on {monitor.DeviceName}");
                }
                return success;
            }
            catch (Exception ex)
            {
                LogService.Debug($"Error setting gamma ramp for {monitor.DeviceName}: {ex.Message}");
                return false;
            }
        }
    }
}
