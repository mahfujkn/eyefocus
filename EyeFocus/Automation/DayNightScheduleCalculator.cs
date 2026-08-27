using System;
using System.Globalization;
using EyeFocus.Models;

namespace EyeFocus.Automation
{
    public enum DayNightPeriod
    {
        Day,
        Night
    }

    public static class DayNightScheduleCalculator
    {
        public static readonly TimeSpan DefaultDayStart = new(6, 0, 0);   // 06:00
        public static readonly TimeSpan DefaultNightStart = new(21, 0, 0); // 21:00

        public static bool TryParseTime(string? timeStr, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(timeStr)) return false;

            if (TimeSpan.TryParseExact(timeStr.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out time) ||
                TimeSpan.TryParseExact(timeStr.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out time) ||
                TimeSpan.TryParse(timeStr.Trim(), CultureInfo.InvariantCulture, out time))
            {
                if (time >= TimeSpan.Zero && time < TimeSpan.FromHours(24))
                {
                    return true;
                }
            }

            return false;
        }

        public static TimeSpan ParseTimeOrDefault(string? timeStr, TimeSpan defaultTime)
        {
            return TryParseTime(timeStr, out var time) ? time : defaultTime;
        }

        public static DateTime GetCurrentEffectiveDateTime(string timeDetectionMode, string? manualTimeZoneId)
        {
            if (string.Equals(timeDetectionMode, "Manual", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(manualTimeZoneId))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(manualTimeZoneId);
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch
                {
                    // Fallback to local Windows time if timezone ID is invalid or not found
                }
            }

            return DateTime.Now;
        }

        public static DayNightPeriod CalculateCurrentPeriod(AppSettings settings)
        {
            var effectiveNow = GetCurrentEffectiveDateTime(settings.TimeDetectionMode, settings.ManualTimeZoneId);
            var dayStart = ParseTimeOrDefault(settings.DayStartTime, DefaultDayStart);
            var nightStart = ParseTimeOrDefault(settings.NightStartTime, DefaultNightStart);

            return CalculatePeriod(effectiveNow.TimeOfDay, dayStart, nightStart);
        }

        public static DayNightPeriod CalculatePeriod(TimeSpan currentTime, TimeSpan dayStart, TimeSpan nightStart)
        {
            if (dayStart == nightStart)
            {
                // Edge case: if start times are identical, default to Day
                return DayNightPeriod.Day;
            }

            if (dayStart < nightStart)
            {
                // Standard schedule: e.g. Day from 06:00 to 21:00, Night from 21:00 to 06:00
                if (currentTime >= dayStart && currentTime < nightStart)
                {
                    return DayNightPeriod.Day;
                }
                else
                {
                    return DayNightPeriod.Night;
                }
            }
            else
            {
                // Inverted schedule: e.g. Day from 20:00 to 06:00, Night from 06:00 to 20:00
                if (currentTime >= dayStart || currentTime < nightStart)
                {
                    return DayNightPeriod.Day;
                }
                else
                {
                    return DayNightPeriod.Night;
                }
            }
        }

        public static string GetTimeZoneSummary(string timeDetectionMode, string? manualTimeZoneId)
        {
            if (string.Equals(timeDetectionMode, "Manual", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(manualTimeZoneId))
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(manualTimeZoneId);
                    var offset = tz.GetUtcOffset(DateTime.UtcNow);
                    var offsetStr = (offset >= TimeSpan.Zero ? "+" : "-") + offset.ToString(@"hh\:mm");
                    return $"{tz.DisplayName} (UTC{offsetStr})";
                }
                catch
                {
                    // Ignore fallback
                }
            }

            var localTz = TimeZoneInfo.Local;
            var localOffset = localTz.GetUtcOffset(DateTime.UtcNow);
            var localOffsetStr = (localOffset >= TimeSpan.Zero ? "+" : "-") + localOffset.ToString(@"hh\:mm");
            return $"{localTz.DisplayName} (UTC{localOffsetStr})";
        }
    }
}
