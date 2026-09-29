using System;
using System.Collections.Generic;
using System.Linq;
using EyeFocus.Automation;
using EyeFocus.Display;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;
using EyeFocus.ViewModels;
using Xunit;

namespace EyeFocus.Tests
{
    public class QuickActionsAndRedesignTests
    {
        [Fact]
        public void ProfileDefaults_HasUpdatedDescriptionsAndIcons()
        {
            var profiles = ProfileDefaults.GetDefaultProfiles();

            var comfort = profiles.First(p => p.Id == ProfileDefaults.IdComfort);
            Assert.Equal("IconLeaf", comfort.IconKey);
            Assert.Contains("Balanced settings", comfort.Description);

            var office = profiles.First(p => p.Id == ProfileDefaults.IdOffice);
            Assert.Equal("IconBriefcase", office.IconKey);

            var game = profiles.First(p => p.Id == ProfileDefaults.IdGame);
            Assert.Contains("immersive", game.Description);

            var coding = profiles.First(p => p.Id == ProfileDefaults.IdCoding);
            Assert.Equal("IconCode", coding.IconKey);

            var reading = profiles.First(p => p.Id == ProfileDefaults.IdReading);
            Assert.Equal("IconBook", reading.IconKey);
        }

        [Fact]
        public void ProfileCardItemViewModel_HasDistinctColorBrushes()
        {
            var profiles = ProfileDefaults.GetDefaultProfiles();
            var comfortProfile = profiles.First(p => p.Id == ProfileDefaults.IdComfort);
            var gameProfile = profiles.First(p => p.Id == ProfileDefaults.IdGame);
            var officeProfile = profiles.First(p => p.Id == ProfileDefaults.IdOffice);

            var cardComfort = new ProfileCardItemViewModel(comfortProfile, true);
            var cardGame = new ProfileCardItemViewModel(gameProfile, false);
            var cardOffice = new ProfileCardItemViewModel(officeProfile, false);

            Assert.Equal("#E6F7F2", cardComfort.IconBgBrush);
            Assert.Equal("#00A88F", cardComfort.IconFgBrush);
            Assert.Equal("IconLeaf", cardComfort.IconKey);
            Assert.True(cardComfort.IsActive);

            Assert.Equal("#F3E8FF", cardGame.IconBgBrush);
            Assert.Equal("#8B5CF6", cardGame.IconFgBrush);
            Assert.Equal("IconGame", cardGame.IconKey);
            Assert.False(cardGame.IsActive);

            Assert.Equal("#E0F2FE", cardOffice.IconBgBrush);
            Assert.Equal("#0284C7", cardOffice.IconFgBrush);
            Assert.Equal("IconBriefcase", cardOffice.IconKey);
        }

        [Fact]
        public void DisplayDashboardViewModel_ProfileCards_Contains8ProfilesExcludingNight()
        {
            var fakeProfileManager = new FakeProfileManager();
            var fakeEngine = new FakeDisplayEngine();
            var fakeSettingsStore = new FakeSettingsStore();
            var fakeAutoService = new FakeAutoDayNightService();

            var vm = new DisplayDashboardViewModel(fakeEngine, fakeProfileManager, fakeSettingsStore, fakeAutoService);

            Assert.Equal(8, vm.ProfileCards.Count);
            Assert.DoesNotContain(vm.ProfileCards, c => c.Id == ProfileDefaults.IdNight);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdComfort);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdGame);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdMovie);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdOffice);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdEditing);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdReading);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdCoding);
            Assert.Contains(vm.ProfileCards, c => c.Id == ProfileDefaults.IdCustom);
        }

        [Fact]
        public void DisplayDashboardViewModel_StatusPill_ReflectsPausedState()
        {
            var fakeProfileManager = new FakeProfileManager();
            var fakeEngine = new FakeDisplayEngine();
            var fakeSettingsStore = new FakeSettingsStore();
            var fakeAutoService = new FakeAutoDayNightService();

            var vm = new DisplayDashboardViewModel(fakeEngine, fakeProfileManager, fakeSettingsStore, fakeAutoService);

            Assert.False(vm.IsPaused);
            Assert.Equal("EyeFocus is active", vm.StatusPillText);
            Assert.Equal("#10B981", vm.StatusPillDotBrush);

            vm.IsPaused = true;
            Assert.Equal("EyeFocus is paused", vm.StatusPillText);
            Assert.Equal("#F59E0B", vm.StatusPillDotBrush);
        }

        [Fact]
        public void DisplayDashboardViewModel_ScheduleToggleText_ReflectsEnabledState()
        {
            var fakeProfileManager = new FakeProfileManager();
            var fakeEngine = new FakeDisplayEngine();
            var fakeSettingsStore = new FakeSettingsStore();
            var fakeAutoService = new FakeAutoDayNightService();

            var vm = new DisplayDashboardViewModel(fakeEngine, fakeProfileManager, fakeSettingsStore, fakeAutoService);

            vm.IsAutomaticDayNightEnabled = true;
            Assert.Equal("Schedule enabled", vm.ScheduleToggleText);

            vm.IsAutomaticDayNightEnabled = false;
            Assert.Equal("Schedule disabled", vm.ScheduleToggleText);
        }

        [Fact]
        public void EyeExerciseViewModel_OpenAndReset_OperatesCorrectly()
        {
            var vm = new EyeExerciseViewModel();
            Assert.False(vm.IsOpen);
            Assert.Equal(20, vm.SecondsRemaining);
            Assert.Equal("20s", vm.TimerDisplay);

            vm.Open();
            Assert.True(vm.IsOpen);
            Assert.True(vm.IsActive);

            vm.CloseCommand.Execute(null);
            Assert.False(vm.IsOpen);
            Assert.False(vm.IsActive);
        }

        private class FakeSettingsStore : ISettingsStore
        {
            private AppSettings _settings = new();
            public event EventHandler<AppSettings>? SettingsChanged;

            public AppSettings Load() => _settings;
            public void Save(AppSettings settings) => _settings = settings;
            public void Update(Action<AppSettings> updateAction)
            {
                updateAction(_settings);
                SettingsChanged?.Invoke(this, _settings);
            }
            public void ResetToDefaults()
            {
                _settings = new AppSettings();
                SettingsChanged?.Invoke(this, _settings);
            }
        }

        private class FakeProfileManager : IProfileManager
        {
            private readonly List<DisplayProfile> _profiles = ProfileDefaults.GetDefaultProfiles();
            private DisplayProfile _active;

            public event EventHandler<DisplayProfile>? ActiveProfileChanged;
            public event EventHandler? ProfilesListChanged;

            public FakeProfileManager()
            {
                _active = _profiles.First();
            }

            public IReadOnlyList<DisplayProfile> GetAllProfiles() => _profiles;
            public DisplayProfile? GetProfile(string id) => _profiles.FirstOrDefault(p => p.Id == id);
            public DisplayProfile GetActiveProfile() => _active;
            public void SetActiveProfile(string id)
            {
                var found = GetProfile(id);
                if (found != null)
                {
                    _active = found;
                    ActiveProfileChanged?.Invoke(this, found);
                }
            }
            public DisplayProfile SaveProfile(DisplayProfile profile) => profile;
            public DisplayProfile SaveAsNew(DisplayProfile profile, string newName) => profile;
            public DisplayProfile ResetProfileToDefault(string id) => _active;
            public bool DeleteProfile(string id) => false;
            public DisplayProfile DuplicateProfile(string id, string? newName = null) => _active;
        }

        private class FakeDisplayEngine : IDisplayEngine
        {
            public IMonitorManager MonitorManager { get; } = new FakeMonitorManager();
            public ISoftwareDimmer SoftwareDimmer => null!;
            public IGammaController GammaController => null!;

            public int CurrentKelvin { get; set; } = 4200;
            public int CurrentBrightness { get; set; } = 70;
            public int CurrentSoftwareDim { get; set; } = 0;

            public void ApplyProfile(DisplayProfile profile, string? targetMonitorId = null)
            {
                CurrentKelvin = profile.Kelvin;
                CurrentBrightness = profile.Brightness;
                CurrentSoftwareDim = profile.SoftwareDim;
            }

            public void SetColorTemperature(int kelvin, string? targetMonitorId = null) => CurrentKelvin = kelvin;
            public void SetBrightness(int brightnessPercent, string? targetMonitorId = null) => CurrentBrightness = brightnessPercent;
            public void SetSoftwareDim(int dimPercent, string? targetMonitorId = null) => CurrentSoftwareDim = dimPercent;
            public void ResetDisplay(string? targetMonitorId = null) { }
            public void RestoreInitialState() { }
        }

        private class FakeMonitorManager : IMonitorManager
        {
            public event EventHandler? MonitorsChanged;
            private readonly List<MonitorInfo> _monitors = new()
            {
                new MonitorInfo
                {
                    DeviceName = @"\\.\DISPLAY1",
                    FriendlyName = "Main Display",
                    IsPrimary = true,
                    SupportsDdcCi = true,
                    SupportsHardwareBrightness = true
                }
            };

            public IReadOnlyList<MonitorInfo> GetMonitors() => _monitors;
            public MonitorInfo? GetMonitor(string stableMonitorId) => _monitors.FirstOrDefault();
            public MonitorInfo? GetPrimaryMonitor() => _monitors.FirstOrDefault();
            public void RefreshMonitors() { }
        }

        private class FakeAutoDayNightService : IAutoDayNightService
        {
            public event Action<DayNightPeriod>? PeriodChanged;
            public event Action<bool>? EnabledChanged;

            public bool IsEnabled { get; set; } = false;
            public DayNightPeriod CurrentPeriod { get; set; } = DayNightPeriod.Day;

            public void Initialize() { }
            public void Evaluate(bool forceApply = false) { }
            public void SetEnabled(bool enabled)
            {
                IsEnabled = enabled;
                EnabledChanged?.Invoke(enabled);
            }

            public void Dispose() { }
        }
    }
}
