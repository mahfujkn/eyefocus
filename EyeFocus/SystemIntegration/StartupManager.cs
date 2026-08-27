using System;
using Microsoft.Win32;
using EyeFocus.Storage;

namespace EyeFocus.SystemIntegration
{
    public interface IStartupManager
    {
        bool IsStartWithWindowsEnabled();
        void SetStartWithWindows(bool enable, bool startMinimized = false);
    }

    public class StartupManager : IStartupManager
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "EyeFocus";

        public bool IsStartWithWindowsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                LogService.Debug($"Failed to read startup registry key: {ex.Message}");
                return false;
            }
        }

        public void SetStartWithWindows(bool enable, bool startMinimized = false)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
                    var command = $"\"{exePath}\"";
                    if (startMinimized)
                    {
                        command += " --minimized";
                    }
                    key.SetValue(AppName, command);
                    LogService.Info($"Enabled Windows startup: {command}");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    LogService.Info("Disabled Windows startup.");
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to update Windows startup registry key: {ex.Message}", ex);
            }
        }
    }
}
