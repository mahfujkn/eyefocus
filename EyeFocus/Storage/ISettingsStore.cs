using System;
using EyeFocus.Models;

namespace EyeFocus.Storage
{
    public interface ISettingsStore
    {
        event EventHandler<AppSettings>? SettingsChanged;
        AppSettings Load();
        void Save(AppSettings settings);
        void Update(Action<AppSettings> updateAction);
        void ResetToDefaults();
    }
}
