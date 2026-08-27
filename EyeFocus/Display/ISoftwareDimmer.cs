using System;
using EyeFocus.Models;

namespace EyeFocus.Display
{
    public interface ISoftwareDimmer : IDisposable
    {
        void SetDimLevel(MonitorInfo monitor, int dimPercent);
        void SetDimLevelAll(int dimPercent);
        void UpdateMonitorsLayout();
        void ClearAll();
    }
}
