using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class HabitTests
{
    [Fact]
    public void AddMissedPeriod_CreatesFinishedPeriod()
    {
        var habit = new Habit(Guid.NewGuid(), "test", DateTime.UtcNow);
        var start = DateTime.UtcNow.AddHours(-2);
        var finish = start.AddMinutes(30);

        Assert.True(habit.AddMissedPeriod(start, finish));

        var period = habit.TimePeriods.Single();
        Assert.Equal(start, period.StartedAt);
        Assert.Equal(30 * 60, period.DurationInSecondes);
    }

    [Fact]
    public void AddMissedPeriod_RejectsFutureAndInverted()
    {
        var habit = new Habit(Guid.NewGuid(), "test", DateTime.UtcNow);

        Assert.False(habit.AddMissedPeriod(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1)));
        Assert.False(habit.AddMissedPeriod(DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(2)));

        Assert.Empty(habit.TimePeriods);
    }

    [Fact]
    public void AddMissedPeriod_RejectsOverlaps_AllowsAdjacent()
    {
        var habit = new Habit(Guid.NewGuid(), "test", DateTime.UtcNow);
        var start = DateTime.UtcNow.AddHours(-3);

        Assert.True(habit.AddMissedPeriod(start, start.AddMinutes(60)));

        Assert.False(habit.AddMissedPeriod(start.AddMinutes(30), start.AddMinutes(90)));
        Assert.False(habit.AddMissedPeriod(start.AddMinutes(-30), start.AddMinutes(30)));

        Assert.True(habit.AddMissedPeriod(start.AddMinutes(-30), start));
        Assert.True(habit.AddMissedPeriod(start.AddMinutes(60), start.AddMinutes(120)));
    }

    [Fact]
    public void AddMissedPeriod_RejectsOverlapWithOpenPeriod()
    {
        var habit = new Habit(Guid.NewGuid(), "test", DateTime.UtcNow);
        Assert.True(habit.StartNewPeriod(DateTime.UtcNow.AddHours(-2)));

        Assert.False(habit.AddMissedPeriod(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(-30)));
    }
}
