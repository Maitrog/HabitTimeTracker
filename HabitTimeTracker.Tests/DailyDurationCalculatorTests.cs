using HabitTimeTracker.Domain.Heatmaps;
using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class DailyDurationCalculatorTests
{
    private static TimePeriod Period(DateTime start, DateTime? finish)
    {
        var p = TimePeriod.StartHabit(Guid.NewGuid(), start);
        if (finish is { } f)
            p.FinishHabit(f);
        return p;
    }

    [Fact]
    public void SinglePeriodWithinDay()
    {
        var start = new DateTime(2026, 10, 6, 10, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(2))], 0, DateTime.UtcNow);
        Assert.Equal(7200, result[new DateOnly(2026, 10, 6)]);
    }

    [Fact]
    public void PeriodSpanningMidnight_SplitsAcrossDays()
    {
        var start = new DateTime(2026, 10, 6, 23, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(2))], 0, DateTime.UtcNow);
        Assert.Equal(3600, result[new DateOnly(2026, 10, 6)]);
        Assert.Equal(3600, result[new DateOnly(2026, 10, 7)]);
    }

    [Fact]
    public void PeriodEndingAtLocalMidnight_StaysOnSingleDay()
    {
        var start = new DateTime(2026, 10, 6, 23, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(1))], 0, DateTime.UtcNow);
        Assert.Equal(3600, result[new DateOnly(2026, 10, 6)]);
        Assert.False(result.ContainsKey(new DateOnly(2026, 10, 7)));
    }

    [Fact]
    public void OffsetMovesPeriodToNextLocalDay()
    {
        var start = new DateTime(2026, 10, 6, 22, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(start, start.AddHours(1))], 180, DateTime.UtcNow);
        var localDate = new DateOnly(2026, 10, 7);
        Assert.True(result.ContainsKey(localDate));
        Assert.Equal(3600, result[localDate]);
    }

    [Fact]
    public void OpenPeriodUsesNow()
    {
        var now = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        var result = DailyDurationCalculator.Calculate([Period(now.AddMinutes(-30), null)], 0, now);
        Assert.Equal(1800, result[new DateOnly(2026, 10, 6)]);
    }

    [Fact]
    public void Empty_ReturnsEmpty()
    {
        Assert.Empty(DailyDurationCalculator.Calculate([], 0, DateTime.UtcNow));
    }
}