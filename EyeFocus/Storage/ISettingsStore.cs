using EyeFocus.Models;

namespace EyeFocus.Storage
{
    public interface ISettingsStore
    {
        AppSettings Load();
        void Save(AppSettings settings);
        void ResetToDefaults();
    }
}
