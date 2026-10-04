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
        private readonly ConcurrentDictionary<string, GdiNative.RgbRamp> _lastAppliedRamps = new();

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
            bool success = SetMonitorGammaRamp(monitor, ref ramp);
            if (success)
            {
                _lastAppliedRamps[monitor.DeviceName] = ramp;
            }
            return success;
        }

        public bool RestoreBaselineGamma(MonitorInfo monitor)
        {
            var key = monitor.DeviceName;
            if (_baselineRamps.TryGetValue(key, out var baseline) && IsNeutralRamp(baseline))
            {
                bool success = SetMonitorGammaRamp(monitor, ref baseline, isBaselineRestore: true);
                if (success)
                {
                    _lastAppliedRamps[key] = baseline;
                }
                return success;
            }
            return ResetToIdentityGamma(monitor);
        }

        public bool ResetToIdentityGamma(MonitorInfo monitor)
        {
            var identity = GdiNative.RgbRamp.CreateIdentity();
            bool success = SetMonitorGammaRamp(monitor, ref identity, isBaselineRestore: true);
            if (success)
            {
                _lastAppliedRamps[monitor.DeviceName] = identity;
            }
            return success;
        }

        public bool GetCurrentGammaRamp(MonitorInfo monitor, out GdiNative.RgbRamp ramp)
        {
            ramp = default;
            try
            {
                var hdc = GdiNative.CreateDC("DISPLAY", monitor.DeviceName, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    hdc = GdiNative.CreateDC("DISPLAY", null, null, IntPtr.Zero);
                }
                if (hdc == IntPtr.Zero)
                {
                    return false;
                }

                bool success = GdiNative.GetDeviceGammaRamp(hdc, out ramp);
                GdiNative.DeleteDC(hdc);
                return success;
            }
            catch (Exception ex)
            {
                LogService.Debug($"Error reading gamma ramp for {monitor.DeviceName}: {ex.Message}");
                return false;
            }
        }

        public bool IsGammaRampReset(MonitorInfo monitor)
        {
            var key = monitor.DeviceName;
            if (!_lastAppliedRamps.TryGetValue(key, out var lastApplied))
            {
                return false;
            }

            // If the last applied ramp was already neutral / 6500K identity (both red and blue are 65535 or near top),
            // then an identity ramp is expected and is not considered an invalid reset.
            if (lastApplied.Blue == null || lastApplied.Blue.Length < 256 ||
                lastApplied.Red == null || lastApplied.Red.Length < 256)
            {
                return false;
            }

            if (lastApplied.Blue[255] >= 64000 && lastApplied.Red[255] >= 64000)
            {
                return false;
            }

            if (!GetCurrentGammaRamp(monitor, out var currentRamp))
            {
                return false;
            }

            if (currentRamp.Blue == null || currentRamp.Blue.Length < 256 ||
                currentRamp.Red == null || currentRamp.Red.Length < 256)
            {
                return false;
            }

            int expectedBlue = lastApplied.Blue[255];
            int expectedRed = lastApplied.Red[255];

            int currentBlue = currentRamp.Blue[255];
            int currentRed = currentRamp.Red[255];

            // Windows Display Settings / DWM resets the ramp to neutral identity (where Blue[255]=65535 and Red[255]=65535).
            // Detect if any channel has jumped towards 65535 by more than 3000 units.
            if ((currentBlue - expectedBlue > 3000) || (currentRed - expectedRed > 3000))
            {
                return true;
            }

            return false;
        }

        public static bool IsNeutralRamp(GdiNative.RgbRamp ramp)
        {
            if (ramp.Red == null || ramp.Green == null || ramp.Blue == null ||
                ramp.Red.Length < 256 || ramp.Green.Length < 256 || ramp.Blue.Length < 256)
            {
                return false;
            }

            int r255 = ramp.Red[255];
            int g255 = ramp.Green[255];
            int b255 = ramp.Blue[255];

            // If channels are below reasonable brightness or Blue is significantly lower than Red (warm tint / night light)
            if (r255 < 50000 || g255 < 50000 || b255 < 50000)
            {
                return false;
            }

            if (b255 < r255 * 0.95)
            {
                return false;
            }

            if (Math.Abs(r255 - b255) > 4000 || Math.Abs(r255 - g255) > 4000)
            {
                return false;
            }

            return true;
        }

        private bool SetMonitorGammaRamp(MonitorInfo monitor, ref GdiNative.RgbRamp ramp, bool isBaselineRestore = false)
        {
            try
            {
                var hdc = GdiNative.CreateDC("DISPLAY", monitor.DeviceName, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                {
                    hdc = GdiNative.CreateDC("DISPLAY", null, null, IntPtr.Zero);
                }
                if (hdc == IntPtr.Zero)
                {
                    LogService.Warn($"Failed to create DC for monitor: {monitor.DeviceName}");
                    return false;
                }

                // Capture baseline on first access, only if it is genuinely neutral
                var key = monitor.DeviceName;
                if (!_baselineRamps.ContainsKey(key) && !isBaselineRestore)
                {
                    if (GdiNative.GetDeviceGammaRamp(hdc, out var initialRamp))
                    {
                        if (IsNeutralRamp(initialRamp))
                        {
                            _baselineRamps[key] = initialRamp;
                            LogService.Debug($"Captured neutral baseline gamma ramp for {monitor.DeviceName}");
                        }
                        else
                        {
                            _baselineRamps[key] = GdiNative.RgbRamp.CreateIdentity();
                            LogService.Debug($"Current gamma ramp on {monitor.DeviceName} is warm/tinted; stored identity ramp as baseline.");
                        }
                    }
                    else
                    {
                        _baselineRamps[key] = GdiNative.RgbRamp.CreateIdentity();
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
