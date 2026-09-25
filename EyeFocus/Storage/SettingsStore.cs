using System;
using System.IO;
using System.Text.Json;
using EyeFocus.Models;
using EyeFocus.Profiles;

namespace EyeFocus.Storage
{
    public class SettingsStore : ISettingsStore
    {
        private readonly string _settingsFilePath;
        private readonly string _backupFilePath;
        private readonly string _settingsDir;
        private readonly object _fileLock = new();
        private AppSettings? _cachedSettings;

        public event EventHandler<AppSettings>? SettingsChanged;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public SettingsStore(string? customPath = null)
        {
            if (!string.IsNullOrEmpty(customPath))
            {
                _settingsFilePath = customPath;
                _settingsDir = Path.GetDirectoryName(customPath) ?? AppContext.BaseDirectory;
            }
            else
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _settingsDir = Path.Combine(appData, "EyeFocus");
                _settingsFilePath = Path.Combine(_settingsDir, "settings.json");
            }
            _backupFilePath = Path.Combine(_settingsDir, "settings.backup.json");
        }

        public AppSettings Load()
        {
            lock (_fileLock)
            {
                if (_cachedSettings != null)
                {
                    return _cachedSettings;
                }

                _cachedSettings = LoadFromDisk();
                return _cachedSettings;
            }
        }

        private AppSettings LoadFromDisk()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        ValidateAndPopulateDefaults(settings);
                        LogService.Info("Loaded settings from settings.json successfully.");
                        return settings;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Warn($"Corrupted settings.json encountered: {ex.Message}. Attempting to restore from backup.");
                }
            }

            // Try backup if main file is corrupted or missing
            if (File.Exists(_backupFilePath))
            {
                try
                {
                    var backupJson = File.ReadAllText(_backupFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(backupJson, JsonOptions);
                    if (settings != null)
                    {
                        ValidateAndPopulateDefaults(settings);
                        LogService.Info("Loaded settings from settings.backup.json successfully.");
                        SaveToDisk(settings); // Restore primary file
                        return settings;
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error($"Failed to restore from backup settings: {ex.Message}");
                }
            }

            // Fresh defaults
            LogService.Info("Generating new default application settings.");
            var defaultSettings = CreateDefaultSettings();
            SaveToDisk(defaultSettings);
            return defaultSettings;
        }

        public void Save(AppSettings settings)
        {
            lock (_fileLock)
            {
                _cachedSettings = settings;
                SaveToDisk(settings);
            }
            SettingsChanged?.Invoke(this, settings);
        }

        public void Update(Action<AppSettings> updateAction)
        {
            AppSettings current;
            lock (_fileLock)
            {
                current = Load();
                updateAction(current);
                _cachedSettings = current;
                SaveToDisk(current);
            }
            SettingsChanged?.Invoke(this, current);
        }

        private void SaveToDisk(AppSettings settings)
        {
            try
            {
                if (!Directory.Exists(_settingsDir))
                {
                    Directory.CreateDirectory(_settingsDir);
                }

                var tempPath = Path.Combine(_settingsDir, $"settings_{Guid.NewGuid():N}.tmp");
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                
                File.WriteAllText(tempPath, json);

                // Atomic replace
                File.Move(tempPath, _settingsFilePath, true);

                // Keep backup copy of the valid state
                try
                {
                    File.Copy(_settingsFilePath, _backupFilePath, true);
                }
                catch
                {
                    // Ignore backup copy failure
                }

                LogService.Debug("Settings saved atomically.");
            }
            catch (Exception ex)
            {
                LogService.Error("Failed to save settings.", ex);
            }
        }

        public void ResetToDefaults()
        {
            var defaultSettings = CreateDefaultSettings();
            Save(defaultSettings);
        }

        private AppSettings CreateDefaultSettings()
        {
            var settings = new AppSettings
            {
                Theme = "System",
                StartWithWindows = false,
                StartMinimized = false,
                MinimizeToTray = true,
                RememberLastProfile = true,
                RestoreOnExit = true,
                ActiveProfileId = ProfileDefaults.IdComfort,
                DayProfileId = ProfileDefaults.IdOffice,
                NightProfileId = ProfileDefaults.IdNight,
                Hotkeys = ProfileDefaults.GetDefaultHotkeys(),
                CustomProfiles = new(),
                ProfileOverrides = new(),
                MonitorMappings = new()
            };
            return settings;
        }

        private void ValidateAndPopulateDefaults(AppSettings settings)
        {
            if (settings.Hotkeys == null || settings.Hotkeys.Count == 0)
            {
                settings.Hotkeys = ProfileDefaults.GetDefaultHotkeys();
            }

            settings.CustomProfiles ??= new();
            settings.ProfileOverrides ??= new();
            settings.MonitorMappings ??= new();
        }
    }
}
