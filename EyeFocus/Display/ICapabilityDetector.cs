using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface ICapabilityDetector
    {
        void ProbeCapabilities(MonitorInfo monitor, System.IntPtr hMonitor);
        void InvalidateCache();
    }
}
