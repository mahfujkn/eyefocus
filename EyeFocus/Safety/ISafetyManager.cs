using System;

namespace EyeFocus.Safety
{
    public interface ISafetyManager : IDisposable
    {
        void Initialize();
        void SafeExit();
    }
}
