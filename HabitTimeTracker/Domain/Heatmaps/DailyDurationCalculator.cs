using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Domain.Heatmaps;

public static class DailyDurationCalculator
{
    // ponytail: фиксированный UTC-offset, DST не учитывается
    public static Dictionary<DateOnly, long> Calculate(
        IEnumerable<TimePeriod> periods, int offsetMinutes, DateTime nowUtc)
    {
        var result = new Dictionary<DateOnly, long>();
        var offset = TimeSpan.FromMinutes(offsetMinutes);

        foreach (var period in periods)
        {
            var start = period.StartedAt;
            var end = period.FinishAt ?? nowUtc;
            if (end <= start)
                continue;

            while (start < end)
            {
                var localDate = DateOnly.FromDateTime(start + offset);
                var nextLocalMidnightUtc = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue) - offset;
                var segmentEnd = end < nextLocalMidnightUtc ? end : nextLocalMidnightUtc;
                var seconds = (long)(segmentEnd - start).TotalSeconds;

                result[localDate] = result.GetValueOrDefault(localDate) + seconds;
                start = segmentEnd;
            }
        }

        return result;
    }
}