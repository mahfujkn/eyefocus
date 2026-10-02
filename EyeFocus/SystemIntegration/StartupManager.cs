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
                using var hkcuKey = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                if (hkcuKey?.GetValue(AppName) != null) return true;

                using var hklmKey = Registry.LocalMachine.OpenSubKey(RunRegistryKey, false);
                if (hklmKey?.GetValue(AppName) != null) return true;

                return false;
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
                if (key != null)
                {
                    if (enable)
                    {
                        var exePath = Environment.ProcessPath ?? AppContext.BaseDirectory;
                        var command = $"\"{exePath}\"";
                        if (startMinimized)
                        {
                            command += " --minimized";
                        }
                        key.SetValue(AppName, command);
                        LogService.Info($"Enabled Windows startup (HKCU): {command}");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                        LogService.Info("Disabled Windows startup (HKCU).");
                    }
                }

                // If disabling, also attempt to clean up HKLM entry if present
                if (!enable)
                {
                    try
                    {
                        using var hklmKey = Registry.LocalMachine.OpenSubKey(RunRegistryKey, true);
                        hklmKey?.DeleteValue(AppName, false);
                    }
                    catch
                    {
                        // Ignore insufficient rights on HKLM if running as standard user
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"Failed to update Windows startup registry key: {ex.Message}", ex);
            }
        }
    }
}
