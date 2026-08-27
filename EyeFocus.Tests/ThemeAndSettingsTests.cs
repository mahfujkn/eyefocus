using System;
using System.IO;
using System.Linq;
using EyeFocus.Models;
using EyeFocus.Profiles;
using EyeFocus.Storage;
using Xunit;

namespace EyeFocus.Tests
{
    public class ThemeAndSettingsTests
    {
        [Fact]
        public void SettingsStore_SavesAndLoadsMaterial3Theme()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"EyeFocus_Test_{Guid.NewGuid():N}");
            var settingsPath = Path.Combine(tempDir, "settings.json");
            var store = new SettingsStore(settingsPath);

            var settings = store.Load();
            Assert.Equal("System", settings.Theme);

            settings.Theme = "Dark";
            store.Save(settings);

            var reloaded = store.Load();
            Assert.Equal("Dark", reloaded.Theme);

            // Cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Fact]
        public void ProfileDefaults_ContainsAll9BuiltInProfiles_WithComfortAsDefault()
        {
            var defaults = ProfileDefaults.GetDefaultProfiles();
            Assert.Equal(9, defaults.Count);

            var comfort = defaults.FirstOrDefault(p => p.Id == ProfileDefaults.IdComfort);
            Assert.NotNull(comfort);
            Assert.Equal("Comfort", comfort.Name);
            Assert.Equal(4200, comfort.Kelvin);
            Assert.Equal(40, comfort.Brightness);
            Assert.Equal(5, comfort.SoftwareDim);

            // Check other profiles
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdGame && p.Kelvin == 6500 && p.Brightness == 100);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdMovie && p.Kelvin == 4500 && p.Brightness == 65);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdOffice && p.Kelvin == 5000 && p.Brightness == 70);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdEditing && p.Kelvin == 6500 && p.Brightness == 70);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdReading && p.Kelvin == 4000 && p.Brightness == 55);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdCoding && p.Kelvin == 4500 && p.Brightness == 60);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdNight && p.Kelvin == 3000 && p.Brightness == 40);
            Assert.Contains(defaults, p => p.Id == ProfileDefaults.IdCustom && p.Kelvin == 5000 && p.Brightness == 70);
        }

        [Fact]
        public void MainWindow_Xaml_CanBeInitialized()
        {
            var thread = new System.Threading.Thread(() =>
            {
                if (System.Windows.Application.Current == null)
                {
                    new System.Windows.Application();
                }
                
                // Merge resources as in App.xaml
                var app = System.Windows.Application.Current;
                app.Resources.MergedDictionaries.Clear();
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Colors.xaml") });
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Icons.xaml") });
                app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = new Uri("pack://application:,,,/EyeFocus;component/UI/Themes/Styles.xaml") });
                app.Resources["IconKeyToGeometryConverter"] = new EyeFocus.UI.Converters.IconKeyToGeometryConverter();
                app.Resources["TabToBooleanConverter"] = new EyeFocus.UI.Converters.TabToBooleanConverter();

                try
                {
                    var window = new EyeFocus.MainWindow(null!, null!, null!);
                    Assert.NotNull(window);
                    window.Measure(new System.Windows.Size(1000, 800));
                    window.Arrange(new System.Windows.Rect(0, 0, 1000, 800));
                    window.ApplyTemplate();
                }
                catch (Exception ex)
                {
                    var inner = ex;
                    while (inner.InnerException != null) inner = inner.InnerException;
                    throw new Exception($"XAML Error: {inner.Message} | Stack: {inner.StackTrace}", ex);
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
