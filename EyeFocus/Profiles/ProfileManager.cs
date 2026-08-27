using System;
using System.Collections.Generic;
using System.Linq;
using EyeFocus.Models;
using EyeFocus.Storage;

namespace EyeFocus.Profiles
{
    public class ProfileManager : IProfileManager
    {
        private readonly ISettingsStore _settingsStore;
        private AppSettings _settings;
        private readonly Dictionary<string, DisplayProfile> _builtInDefaults;
        private readonly Dictionary<string, DisplayProfile> _activeProfilesMap = new();

        public event EventHandler<DisplayProfile>? ActiveProfileChanged;
        public event EventHandler? ProfilesListChanged;

        public ProfileManager(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
            _settings = _settingsStore.Load();

            _builtInDefaults = ProfileDefaults.GetDefaultProfiles().ToDictionary(p => p.Id, p => p);
            RefreshProfilesMap();
        }

        private void RefreshProfilesMap()
        {
            _activeProfilesMap.Clear();

            // 1. Load built-in profiles (applying user overrides if any)
            foreach (var defaultProfile in _builtInDefaults.Values)
            {
                var copy = defaultProfile.Clone(defaultProfile.Name);
                copy.Id = defaultProfile.Id;
                copy.IsBuiltIn = true;

                var userOverride = _settings.ProfileOverrides.FirstOrDefault(o => o.Id == defaultProfile.Id);
                if (userOverride != null)
                {
                    copy.CopyFrom(userOverride);
                    copy.IsUserModified = true;
                }

                _activeProfilesMap[copy.Id] = copy;
            }

            // 2. Load custom profiles
            foreach (var customProfile in _settings.CustomProfiles)
            {
                _activeProfilesMap[customProfile.Id] = customProfile;
            }
        }

        public IReadOnlyList<DisplayProfile> GetAllProfiles()
        {
            return _activeProfilesMap.Values.ToList();
        }

        public DisplayProfile? GetProfile(string id)
        {
            if (_activeProfilesMap.TryGetValue(id, out var profile))
            {
                return profile;
            }
            return null;
        }

        public DisplayProfile GetActiveProfile()
        {
            var activeId = _settings.ActiveProfileId;
            if (string.IsNullOrEmpty(activeId) || !_activeProfilesMap.TryGetValue(activeId, out var profile))
            {
                return _activeProfilesMap.Values.FirstOrDefault() ?? _builtInDefaults[ProfileDefaults.IdComfort];
            }
            return profile;
        }

        public void SetActiveProfile(string id)
        {
            if (_activeProfilesMap.TryGetValue(id, out var profile))
            {
                _settings.ActiveProfileId = id;
                _settingsStore.Save(_settings);
                LogService.Info($"Active profile switched to: {profile.Name} ({profile.Id})");
                ActiveProfileChanged?.Invoke(this, profile);
            }
        }

        public DisplayProfile SaveProfile(DisplayProfile profile)
        {
            if (profile.IsBuiltIn)
            {
                // Save as an override
                var existingOverride = _settings.ProfileOverrides.FirstOrDefault(o => o.Id == profile.Id);
                if (existingOverride == null)
                {
                    existingOverride = new DisplayProfile { Id = profile.Id };
                    _settings.ProfileOverrides.Add(existingOverride);
                }
                existingOverride.CopyFrom(profile);
            }
            else
            {
                // Save custom profile
                var existing = _settings.CustomProfiles.FirstOrDefault(p => p.Id == profile.Id);
                if (existing == null)
                {
                    _settings.CustomProfiles.Add(profile);
                }
                else
                {
                    existing.CopyFrom(profile);
                    existing.Name = profile.Name;
                }
            }

            _settingsStore.Save(_settings);
            RefreshProfilesMap();
            ProfilesListChanged?.Invoke(this, EventArgs.Empty);

            var saved = _activeProfilesMap[profile.Id];
            if (_settings.ActiveProfileId == profile.Id)
            {
                ActiveProfileChanged?.Invoke(this, saved);
            }
            return saved;
        }

        public DisplayProfile SaveAsNew(DisplayProfile profile, string newName)
        {
            var newProfile = profile.Clone(newName);
            newProfile.IsBuiltIn = false;
            newProfile.IsUserModified = false;

            _settings.CustomProfiles.Add(newProfile);
            _settingsStore.Save(_settings);

            RefreshProfilesMap();
            ProfilesListChanged?.Invoke(this, EventArgs.Empty);
            LogService.Info($"Created new custom profile: {newProfile.Name} ({newProfile.Id})");
            return newProfile;
        }

        public DisplayProfile ResetProfileToDefault(string id)
        {
            if (_builtInDefaults.TryGetValue(id, out var originalDefault))
            {
                _settings.ProfileOverrides.RemoveAll(o => o.Id == id);
                _settingsStore.Save(_settings);

                RefreshProfilesMap();
                ProfilesListChanged?.Invoke(this, EventArgs.Empty);

                var resetProfile = _activeProfilesMap[id];
                if (_settings.ActiveProfileId == id)
                {
                    ActiveProfileChanged?.Invoke(this, resetProfile);
                }
                LogService.Info($"Reset built-in profile to default: {resetProfile.Name}");
                return resetProfile;
            }

            return _activeProfilesMap[id];
        }

        public bool DeleteProfile(string id)
        {
            if (_builtInDefaults.ContainsKey(id))
            {
                // Built-in profiles cannot be deleted
                return false;
            }

            var removed = _settings.CustomProfiles.RemoveAll(p => p.Id == id) > 0;
            if (removed)
            {
                if (_settings.ActiveProfileId == id)
                {
                    _settings.ActiveProfileId = ProfileDefaults.IdComfort;
                }
                _settingsStore.Save(_settings);
                RefreshProfilesMap();
                ProfilesListChanged?.Invoke(this, EventArgs.Empty);
                LogService.Info($"Deleted custom profile: {id}");
            }
            return removed;
        }

        public DisplayProfile DuplicateProfile(string id, string? newName = null)
        {
            var source = GetProfile(id) ?? _builtInDefaults[ProfileDefaults.IdComfort];
            var cloneName = newName ?? $"{source.Name} (Copy)";
            return SaveAsNew(source, cloneName);
        }
    }
}
