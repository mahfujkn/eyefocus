using System;
using System.Runtime.InteropServices;

namespace EyeFocus.Display.Native
{
    public static class Dxva2Native
    {
        public const uint MC_CAPS_BRIGHTNESS = 0x00000002;
        public const uint MC_CAPS_CONTRAST = 0x00000004;
        public const uint MC_CAPS_COLOR_TEMPERATURE = 0x00000008;
        public const uint MC_CAPS_RED_GREEN_BLUE_GAIN = 0x00000010;

        public const uint MC_RED_GAIN = 0;
        public const uint MC_GREEN_GAIN = 1;
        public const uint MC_BLUE_GAIN = 2;

        public const uint MC_COLOR_TEMPERATURE_4000K = 0x00000001;
        public const uint MC_COLOR_TEMPERATURE_5000K = 0x00000002;
        public const uint MC_COLOR_TEMPERATURE_6500K = 0x00000004;
        public const uint MC_COLOR_TEMPERATURE_7500K = 0x00000008;
        public const uint MC_COLOR_TEMPERATURE_8200K = 0x00000010;
        public const uint MC_COLOR_TEMPERATURE_9300K = 0x00000020;
        public const uint MC_COLOR_TEMPERATURE_10000K = 0x00000040;
        public const uint MC_COLOR_TEMPERATURE_11500K = 0x00000080;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
            IntPtr hMonitor,
            out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(
            IntPtr hMonitor,
            uint dwPhysicalMonitorArraySize,
            [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(
            uint dwPhysicalMonitorArraySize,
            [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorBrightness(
            IntPtr hMonitor,
            out uint pdwMinimumBrightness,
            out uint pdwCurrentBrightness,
            out uint pdwMaximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorBrightness(
            IntPtr hMonitor,
            uint dwNewBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorCapabilities(
            IntPtr hMonitor,
            out uint pdwMonitorCapabilities,
            out uint pdwSupportedColorTemperatures);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorColorTemperature(
            IntPtr hMonitor,
            out uint pctCurrentColorTemperature);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorColorTemperature(
            IntPtr hMonitor,
            uint ctCurrentColorTemperature);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorRedGreenOrBlueGain(
            IntPtr hMonitor,
            uint gainType,
            out uint pdwMinimumGain,
            out uint pdwCurrentGain,
            out uint pdwMaximumGain);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorRedGreenOrBlueGain(
            IntPtr hMonitor,
            uint gainType,
            uint dwNewGain);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorContrast(
            IntPtr hMonitor,
            out uint pdwMinimumContrast,
            out uint pdwCurrentContrast,
            out uint pdwMaximumContrast);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorContrast(
            IntPtr hMonitor,
            uint dwNewContrast);
    }
}
