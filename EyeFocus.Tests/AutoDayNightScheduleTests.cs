using System;
using EyeFocus.Automation;
using EyeFocus.Models;
using Xunit;

namespace EyeFocus.Tests
{
    public class AutoDayNightScheduleTests
    {
        [Theory]
        [InlineData("05:59:59", DayNightPeriod.Night)]
        [InlineData("06:00:00", DayNightPeriod.Day)]
        [InlineData("12:00:00", DayNightPeriod.Day)]
        [InlineData("20:59:59", DayNightPeriod.Day)]
        [InlineData("21:00:00", DayNightPeriod.Night)]
        [InlineData("23:59:59", DayNightPeriod.Night)]
        [InlineData("00:00:00", DayNightPeriod.Night)]
        public void CalculatePeriod_StandardSchedule_ReturnsCorrectPeriod(string timeStr, DayNightPeriod expected)
        {
            var currentTime = TimeSpan.Parse(timeStr);
            var dayStart = new TimeSpan(6, 0, 0);   // 06:00
            var nightStart = new TimeSpan(21, 0, 0); // 21:00

            var result = DayNightScheduleCalculator.CalculatePeriod(currentTime, dayStart, nightStart);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("05:59:59", DayNightPeriod.Day)]
        [InlineData("06:00:00", DayNightPeriod.Night)]
        [InlineData("12:00:00", DayNightPeriod.Night)]
        [InlineData("19:59:59", DayNightPeriod.Night)]
        [InlineData("20:00:00", DayNightPeriod.Day)]
        [InlineData("02:00:00", DayNightPeriod.Day)]
        public void CalculatePeriod_InvertedMidnightCrossingSchedule_ReturnsCorrectPeriod(string timeStr, DayNightPeriod expected)
        {
            var currentTime = TimeSpan.Parse(timeStr);
            var dayStart = new TimeSpan(20, 0, 0);  // 20:00
            var nightStart = new TimeSpan(6, 0, 0);  // 06:00

            var result = DayNightScheduleCalculator.CalculatePeriod(currentTime, dayStart, nightStart);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("08:29:59", DayNightPeriod.Night)]
        [InlineData("08:30:00", DayNightPeriod.Day)]
        [InlineData("14:00:00", DayNightPeriod.Day)]
        [InlineData("18:29:59", DayNightPeriod.Day)]
        [InlineData("18:30:00", DayNightPeriod.Night)]
        public void CalculatePeriod_CustomTimeSchedule_ReturnsCorrectPeriod(string timeStr, DayNightPeriod expected)
        {
            var currentTime = TimeSpan.Parse(timeStr);
            var dayStart = new TimeSpan(8, 30, 0);   // 08:30
            var nightStart = new TimeSpan(18, 30, 0); // 18:30

            var result = DayNightScheduleCalculator.CalculatePeriod(currentTime, dayStart, nightStart);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryParseTime_ValidAndInvalidFormats_HandledCorrectly()
        {
            Assert.True(DayNightScheduleCalculator.TryParseTime("06:00", out var t1));
            Assert.Equal(new TimeSpan(6, 0, 0), t1);

            Assert.True(DayNightScheduleCalculator.TryParseTime("21:30", out var t2));
            Assert.Equal(new TimeSpan(21, 30, 0), t2);

            Assert.False(DayNightScheduleCalculator.TryParseTime("25:00", out _));
            Assert.False(DayNightScheduleCalculator.TryParseTime("invalid", out _));
            Assert.False(DayNightScheduleCalculator.TryParseTime(string.Empty, out _));
        }

        [Fact]
        public void IdenticalStartTimes_DefaultsGracefullyToDay()
        {
            var same = new TimeSpan(12, 0, 0);
            var result = DayNightScheduleCalculator.CalculatePeriod(same, same, same);
            Assert.Equal(DayNightPeriod.Day, result);
        }

        [Fact]
        public void AppSettings_DefaultValues_PreserveExistingBehavior()
        {
            var settings = new AppSettings();

            Assert.False(settings.AutomaticDayNightEnabled);
            Assert.Equal("System", settings.TimeDetectionMode);
            Assert.Equal("06:00", settings.DayStartTime);
            Assert.Equal("21:00", settings.NightStartTime);
        }
    }
}
