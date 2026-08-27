using System;
using System.Collections.Generic;
using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface IMonitorManager
    {
        IReadOnlyList<MonitorInfo> GetMonitors();
        MonitorInfo? GetMonitor(string stableMonitorId);
        MonitorInfo? GetPrimaryMonitor();
        void RefreshMonitors();

        event EventHandler? MonitorsChanged;
    }
}
