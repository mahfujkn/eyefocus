using System;

namespace EyeFocus.Automation
{
    public interface IAutoDayNightService : IDisposable
    {
        event Action<DayNightPeriod>? PeriodChanged;

        bool IsEnabled { get; }
        DayNightPeriod CurrentPeriod { get; }

        void Initialize();
        void Evaluate(bool forceApply = false);
        void SetEnabled(bool enabled);
    }
}
