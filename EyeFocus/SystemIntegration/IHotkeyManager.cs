using System;
using System.Collections.Generic;
using EyeFocus.Models;

namespace EyeFocus.SystemIntegration
{
    public interface IHotkeyManager : IDisposable
    {
        void Initialize(IntPtr windowHandle);
        void ReloadHotkeys();
        bool RegisterBinding(HotkeyBinding binding, out string? errorMessage);
        void UnregisterAll();

        event EventHandler<HotkeyAction>? HotkeyTriggered;
    }
}
