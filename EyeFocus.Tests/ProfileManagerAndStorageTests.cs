using System;
using System.IO;
using System.Linq;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;
using Xunit;

namespace EyeFocus.Tests
{
    public class ProfileManagerAndStorageTests
    {
        [Fact]
        public void SettingsStore_SavesAndLoadsCorrectly()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            var settings = store.Load();
            Assert.NotNull(settings);
            Assert.NotEmpty(settings.Hotkeys);

            settings.Theme = "Light";
            settings.DefaultTransitionDuration = 15;
            store.Save(settings);

            var reloaded = store.Load();
            Assert.Equal("Light", reloaded.Theme);
            Assert.Equal(15, reloaded.DefaultTransitionDuration);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void SettingsStore_CorruptedPrimaryFile_RecoversFromBackup()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            var settings = store.Load();
            settings.Theme = "System";
            store.Save(settings); // Creates backup copy

            // Corrupt primary file
            File.WriteAllText(settingsPath, "INVALID JSON CONTENT { [");

            var recovered = store.Load();
            Assert.NotNull(recovered);
            Assert.Equal("System", recovered.Theme);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ProfileManager_ModifyingBuiltInProfile_KeepsOriginalTemplateSafe()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);
            var manager = new ProfileManager(store);

            var comfortProfile = manager.GetProfile(ProfileDefaults.IdComfort);
            Assert.NotNull(comfortProfile);
            Assert.Equal(4200, comfortProfile.Kelvin);

            // Edit built-in profile
            comfortProfile.Kelvin = 3500;
            manager.SaveProfile(comfortProfile);

            // Verify manager returns modified version
            var modified = manager.GetProfile(ProfileDefaults.IdComfort);
            Assert.Equal(3500, modified!.Kelvin);
            Assert.True(modified.IsUserModified);

            // Reset back to default
            var reset = manager.ResetProfileToDefault(ProfileDefaults.IdComfort);
            Assert.Equal(4200, reset.Kelvin);
            Assert.False(reset.IsUserModified);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ProfileManager_CustomProfile_CanBeCreatedAndDeleted()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);
            var manager = new ProfileManager(store);

            var baseProfile = manager.GetProfile(ProfileDefaults.IdComfort)!;
            var custom = manager.SaveAsNew(baseProfile, "My Custom Ultra Warm");

            Assert.NotNull(custom);
            Assert.Equal("My Custom Ultra Warm", custom.Name);
            Assert.False(custom.IsBuiltIn);

            var retrieved = manager.GetProfile(custom.Id);
            Assert.NotNull(retrieved);

            // Delete custom profile
            bool deleted = manager.DeleteProfile(custom.Id);
            Assert.True(deleted);
            Assert.Null(manager.GetProfile(custom.Id));

            // Built-in profiles cannot be deleted
            bool deleteBuiltIn = manager.DeleteProfile(ProfileDefaults.IdGame);
            Assert.False(deleteBuiltIn);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ProfileManager_SetActiveProfile_FiresActiveProfileChangedEvent()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);
            var manager = new ProfileManager(store);

            DisplayProfile? changedProfile = null;
            manager.ActiveProfileChanged += (s, p) => changedProfile = p;

            manager.SetActiveProfile(ProfileDefaults.IdNight);

            Assert.NotNull(changedProfile);
            Assert.Equal(ProfileDefaults.IdNight, changedProfile.Id);
            Assert.Equal(3000, changedProfile.Kelvin);
            Assert.Equal(40, changedProfile.Brightness);

            var active = manager.GetActiveProfile();
            Assert.Equal(ProfileDefaults.IdNight, active.Id);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void SmoothstepInterpolation_PropertiesHold()
        {
            // Smoothstep formula: S(t) = 3t^2 - 2t^3
            Func<double, double> smoothstep = t => t * t * (3.0 - 2.0 * t);

            Assert.Equal(0.0, smoothstep(0.0));
            Assert.Equal(0.5, smoothstep(0.5));
            Assert.Equal(1.0, smoothstep(1.0));

            // Verify monotonicity on [0, 1]
            double prev = 0.0;
            for (double t = 0.05; t <= 1.0; t += 0.05)
            {
                double val = smoothstep(t);
                Assert.True(val >= prev, $"Smoothstep failed monotonicity at t={t}");
                prev = val;
            }
        }

        [Fact]
        public void SettingsStore_Update_FiresSettingsChanged_AndPersistsAtomically()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            bool eventFired = false;
            AppSettings? capturedSettings = null;
            store.SettingsChanged += (s, args) =>
            {
                eventFired = true;
                capturedSettings = args;
            };

            store.Update(s =>
            {
                s.AutomaticDayNightEnabled = false;
                s.StartWithWindows = true;
                s.MinimizeToTray = false;
                s.SoftwareDimFallback = true;
            });

            Assert.True(eventFired);
            Assert.NotNull(capturedSettings);
            Assert.False(capturedSettings.AutomaticDayNightEnabled);
            Assert.True(capturedSettings.StartWithWindows);
            Assert.False(capturedSettings.MinimizeToTray);
            Assert.True(capturedSettings.SoftwareDimFallback);

            // Re-read directly from disk via a brand-new store instance
            var freshStore = new SettingsStore(settingsPath);
            var freshSettings = freshStore.Load();
            Assert.False(freshSettings.AutomaticDayNightEnabled);
            Assert.True(freshSettings.StartWithWindows);
            Assert.False(freshSettings.MinimizeToTray);
            Assert.True(freshSettings.SoftwareDimFallback);

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ProfileManager_SetActiveProfile_PreservesSettingsSwitches()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            // Setup custom switch states
            store.Update(s =>
            {
                s.AutomaticDayNightEnabled = false;
                s.StartWithWindows = true;
                s.MinimizeToTray = false;
                s.RememberLastProfile = true;
            });

            var manager = new ProfileManager(store);

            // Switch profile and save changes
            manager.SetActiveProfile(ProfileDefaults.IdGame);
            var activeProfile = manager.GetActiveProfile();
            activeProfile.Kelvin = 6000;
            manager.SaveProfile(activeProfile);

            // Verify with a new store instance that the switches were NOT overwritten
            var freshStore = new SettingsStore(settingsPath);
            var freshSettings = freshStore.Load();

            Assert.False(freshSettings.AutomaticDayNightEnabled);
            Assert.True(freshSettings.StartWithWindows);
            Assert.False(freshSettings.MinimizeToTray);
            Assert.True(freshSettings.RememberLastProfile);
            Assert.Equal(ProfileDefaults.IdGame, freshSettings.ActiveProfileId);

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void AutoDayNightService_WhenDisabled_DoesNotEvaluateOrSwitchProfile()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            store.Update(s =>
            {
                s.AutomaticDayNightEnabled = false;
                s.DayProfileId = ProfileDefaults.IdComfort;
                s.NightProfileId = ProfileDefaults.IdNight;
                s.ActiveProfileId = ProfileDefaults.IdGame;
            });

            var manager = new ProfileManager(store);
            var fakeDisplayEngine = new FakeDisplayEngine();
            var service = new EyeFocus.Automation.AutoDayNightService(manager, fakeDisplayEngine, store);
            service.Initialize();

            Assert.False(service.IsEnabled);

            // Trigger evaluate while disabled
            service.Evaluate();

            // Profile must remain unchanged (Game)
            Assert.Equal(ProfileDefaults.IdGame, manager.GetActiveProfile().Id);

            service.Dispose();
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        private class FakeDisplayEngine : EyeFocus.Display.IDisplayEngine
        {
            public EyeFocus.Display.IMonitorManager MonitorManager => null!;
            public EyeFocus.Display.ISoftwareDimmer SoftwareDimmer => null!;
            public EyeFocus.Display.IGammaController GammaController => null!;
            public int CurrentKelvin => 6500;
            public int CurrentBrightness => 100;
            public int CurrentSoftwareDim => 0;

            public void ApplyProfile(DisplayProfile profile, string? targetMonitorId = null) { }
            public void ResetDisplay(string? targetMonitorId = null) { }
            public void RestoreInitialState() { }
            public void SetBrightness(int brightnessPercent, string? targetMonitorId = null) { }
            public void SetColorTemperature(int kelvin, string? targetMonitorId = null) { }
            public void SetSoftwareDim(int dimPercent, string? targetMonitorId = null) { }
        }
    }
}
