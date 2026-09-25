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
        private readonly Dictionary<string, DisplayProfile> _builtInDefaults;
        private readonly Dictionary<string, DisplayProfile> _activeProfilesMap = new();

        public event EventHandler<DisplayProfile>? ActiveProfileChanged;
        public event EventHandler? ProfilesListChanged;

        private AppSettings CurrentSettings => _settingsStore.Load();

        public ProfileManager(ISettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
            _builtInDefaults = ProfileDefaults.GetDefaultProfiles().ToDictionary(p => p.Id, p => p);
            RefreshProfilesMap();

            _settingsStore.SettingsChanged += (s, e) => RefreshProfilesMap();
        }

        private void RefreshProfilesMap()
        {
            _activeProfilesMap.Clear();
            var settings = CurrentSettings;

            // 1. Load built-in profiles (applying user overrides if any)
            foreach (var defaultProfile in _builtInDefaults.Values)
            {
                var copy = defaultProfile.Clone(defaultProfile.Name);
                copy.Id = defaultProfile.Id;
                copy.IsBuiltIn = true;

                var userOverride = settings.ProfileOverrides.FirstOrDefault(o => o.Id == defaultProfile.Id);
                if (userOverride != null)
                {
                    copy.CopyFrom(userOverride);
                    copy.IsUserModified = true;
                }

                _activeProfilesMap[copy.Id] = copy;
            }

            // 2. Load custom profiles
            foreach (var customProfile in settings.CustomProfiles)
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
            var activeId = CurrentSettings.ActiveProfileId;
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
                _settingsStore.Update(s => s.ActiveProfileId = id);
                LogService.Info($"Active profile switched to: {profile.Name} ({profile.Id})");
                ActiveProfileChanged?.Invoke(this, profile);
            }
        }

        public DisplayProfile SaveProfile(DisplayProfile profile)
        {
            _settingsStore.Update(settings =>
            {
                if (profile.IsBuiltIn)
                {
                    // Save as an override
                    var existingOverride = settings.ProfileOverrides.FirstOrDefault(o => o.Id == profile.Id);
                    if (existingOverride == null)
                    {
                        existingOverride = new DisplayProfile { Id = profile.Id };
                        settings.ProfileOverrides.Add(existingOverride);
                    }
                    existingOverride.CopyFrom(profile);
                }
                else
                {
                    // Save custom profile
                    var existing = settings.CustomProfiles.FirstOrDefault(p => p.Id == profile.Id);
                    if (existing == null)
                    {
                        settings.CustomProfiles.Add(profile);
                    }
                    else
                    {
                        existing.CopyFrom(profile);
                        existing.Name = profile.Name;
                    }
                }
            });

            RefreshProfilesMap();
            ProfilesListChanged?.Invoke(this, EventArgs.Empty);

            var saved = _activeProfilesMap[profile.Id];
            if (CurrentSettings.ActiveProfileId == profile.Id)
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

            _settingsStore.Update(settings =>
            {
                settings.CustomProfiles.Add(newProfile);
            });

            RefreshProfilesMap();
            ProfilesListChanged?.Invoke(this, EventArgs.Empty);
            LogService.Info($"Created new custom profile: {newProfile.Name} ({newProfile.Id})");
            return newProfile;
        }

        public DisplayProfile ResetProfileToDefault(string id)
        {
            if (_builtInDefaults.TryGetValue(id, out var originalDefault))
            {
                _settingsStore.Update(settings =>
                {
                    settings.ProfileOverrides.RemoveAll(o => o.Id == id);
                });

                RefreshProfilesMap();
                ProfilesListChanged?.Invoke(this, EventArgs.Empty);

                var resetProfile = _activeProfilesMap[id];
                if (CurrentSettings.ActiveProfileId == id)
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

            bool removed = false;
            _settingsStore.Update(settings =>
            {
                removed = settings.CustomProfiles.RemoveAll(p => p.Id == id) > 0;
                if (removed && settings.ActiveProfileId == id)
                {
                    settings.ActiveProfileId = ProfileDefaults.IdComfort;
                }
            });

            if (removed)
            {
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
