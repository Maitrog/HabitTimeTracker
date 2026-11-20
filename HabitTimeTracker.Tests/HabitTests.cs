using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class HabitTests
{
    [Fact]
    public void CompleteEpoch_RotatesId_AndAllowsBackfillIntoPreviousEpoch()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var habit = new Habit(Guid.NewGuid(), "Чтение", now.AddHours(-4));
        var first = habit.CurrentEpochId;
        Assert.NotEqual(Guid.Empty, first);
        Assert.True(habit.StartNewPeriod(now.AddHours(-3)));
        Assert.True(habit.FinishLastPeriod(now.AddHours(-2)));
        var book = Assert.IsType<Epoch>(habit.TryCompleteEpoch(first, "  Книга А  ", now));
        Assert.Equal(first, book.Id);
        Assert.Equal(habit.Id, book.HabitId);
        Assert.Equal("Книга А", book.Name);
        Assert.Equal(now, book.CompletedAtUtc);
        Assert.NotEqual(first, habit.CurrentEpochId);

        Assert.True(habit.AddMissedPeriod(now.AddHours(-2), now.AddMinutes(-90), book));
        Assert.False(habit.AddMissedPeriod(now.AddHours(-3), now.AddMinutes(-150)));
        Assert.Equal(5400L, habit.TimePeriods.Where(p => p.EpochId == book.Id).Sum(p => p.DurationInSecondes));

        Assert.True(habit.StartNewPeriod(now.AddMinutes(1)));
        Assert.Equal(habit.CurrentEpochId, habit.TimePeriods.Last().EpochId);
        Assert.True(habit.FinishLastPeriod(now.AddMinutes(10)));
        Assert.True(habit.EditLastPeriodFinish(now.AddMinutes(12)));
        Assert.Equal(habit.CurrentEpochId, habit.TimePeriods.Last().EpochId);
        Assert.Null(habit.TryCompleteEpoch(first, "Повтор", now.AddMinutes(13)));

        var other = new Habit(habit.UserId, "Другая", now);
        Assert.False(other.AddMissedPeriod(now, now.AddMinutes(30), book));
        Assert.Empty(other.TimePeriods);
    }

    [Fact]
    public void CompleteEpoch_RejectsOpenOrEmptyEpochAndInvalidName()
    {
        var now = DateTime.UtcNow.AddDays(-1);
        var habit = new Habit(Guid.NewGuid(), "Чтение", now);
        var first = habit.CurrentEpochId;
        Assert.Null(habit.TryCompleteEpoch(first, "Пустая", now));
        Assert.True(habit.StartNewPeriod(now));
        Assert.Null(habit.TryCompleteEpoch(first, "Таймер запущен", now.AddHours(1)));
        Assert.True(habit.AddMissedPeriod(now.AddHours(-2), now.AddHours(-1)));
        Assert.False(habit.StartNewPeriod(now.AddHours(1)));
        Assert.True(habit.FinishLastPeriod(now.AddHours(1)));
        Assert.Null(habit.TryCompleteEpoch(first, "  ", now.AddHours(1)));
        Assert.Null(habit.TryCompleteEpoch(first, new string('а', 121), now.AddHours(1)));
        Assert.Equal(first, habit.CurrentEpochId);
        Assert.NotNull(habit.TryCompleteEpoch(first, new string('а', 120), now.AddHours(1)));
        Assert.Null(habit.TryCompleteEpoch(habit.CurrentEpochId, "Снова пустая", now.AddHours(1)));
    }

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
