using System;
using EyeFocus.Display;
using Xunit;

namespace EyeFocus.Tests
{
    public class DisplayEngineTests
    {
        private readonly GammaController _gammaController = new();

        [Theory]
        [InlineData(2500)]
        [InlineData(3000)]
        [InlineData(4500)]
        [InlineData(5000)]
        [InlineData(6000)]
        [InlineData(6500)]
        [InlineData(7500)]
        public void KelvinToRgbMultipliers_ReturnsValidNormalizedValues(int kelvin)
        {
            var (r, g, b) = _gammaController.KelvinToRgbMultipliers(kelvin);

            Assert.InRange(r, 0.0, 1.0);
            Assert.InRange(g, 0.0, 1.0);
            Assert.InRange(b, 0.0, 1.0);
            Assert.False(double.IsNaN(r));
            Assert.False(double.IsNaN(g));
            Assert.False(double.IsNaN(b));
        }

        [Fact]
        public void KelvinToRgb_WarmerKelvin_HasLowerBlueThanRed()
        {
            var warm = _gammaController.KelvinToRgbMultipliers(2700);
            Assert.True(warm.R > warm.B, "Warm Kelvin (2700K) should have significantly more Red than Blue.");
        }

        [Fact]
        public void KelvinToRgb_CoolerKelvin_HasHighBlue()
        {
            var cool = _gammaController.KelvinToRgbMultipliers(7500);
            Assert.True(cool.B >= 0.95, "Cool Kelvin (7500K) should have high Blue channel multiplier.");
        }

        [Theory]
        [InlineData(2500, 100, 100, 100, 50)]
        [InlineData(3000, 100, 88, 72, 50)]
        [InlineData(5500, 100, 100, 100, 50)]
        [InlineData(6500, 100, 100, 100, 50)]
        [InlineData(7500, 100, 100, 100, 50)]
        public void GenerateGammaRamp_RampsAreMonotonicNonDecreasing(int kelvin, int r, int g, int b, int contrast)
        {
            var ramp = _gammaController.GenerateGammaRamp(kelvin, r, g, b, contrast);

            Assert.NotNull(ramp.Red);
            Assert.NotNull(ramp.Green);
            Assert.NotNull(ramp.Blue);
            Assert.Equal(256, ramp.Red.Length);
            Assert.Equal(256, ramp.Green.Length);
            Assert.Equal(256, ramp.Blue.Length);

            // Verify monotonicity
            for (int i = 1; i < 256; i++)
            {
                Assert.True(ramp.Red[i] >= ramp.Red[i - 1], $"Red ramp at step {i} is not monotonic.");
                Assert.True(ramp.Green[i] >= ramp.Green[i - 1], $"Green ramp at step {i} is not monotonic.");
                Assert.True(ramp.Blue[i] >= ramp.Blue[i - 1], $"Blue ramp at step {i} is not monotonic.");
            }

            // Verify baseline minimum
            Assert.True(ramp.Red[0] <= 100);
            Assert.True(ramp.Green[0] <= 100);
            Assert.True(ramp.Blue[0] <= 100);
        }
    }
}
