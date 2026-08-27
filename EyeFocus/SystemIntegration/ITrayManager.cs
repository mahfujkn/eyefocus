using System;

namespace EyeFocus.SystemIntegration
{
    public interface ITrayManager : IDisposable
    {
        void Initialize();
        void UpdateTrayState();
        void ShowNotification(string title, string message);

        event EventHandler? OpenRequested;
        event EventHandler? SettingsRequested;
        event EventHandler? ExitRequested;
    }
}
