using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HabitTimeTracker.Domain.Models;

public class Habit
{
    public Guid Id { get; private set; }

    public Guid CurrentEpochId { get; private set; }
    
    public string Name { get; private set; }

    public DateTime Created { get; private set; }

    public DateTime Updated { get; private set; }
    
    public Guid UserId { get; private set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; private set; }
    
    public uint Version { get; private set; }
    
    public bool Deleted { get; private set; }

    public virtual Collection<TimePeriod> TimePeriods { get; private set; } = [];
    
    private Habit() { }

    public Habit(Guid userId, string name, DateTime now)
    {
        Id = Guid.NewGuid();
        CurrentEpochId = Guid.NewGuid();
        Created = now;
        Updated = now;
        UserId = userId;
        Name = name;
    }

    public bool StartNewPeriod(DateTime now)
    {
        if (TimePeriods.Any(p => p.FinishAt is null))
            return false;

        TimePeriods.Add(TimePeriod.StartHabit(Id, CurrentEpochId, now));
        Updated = now;
        return true;
    }

    public bool FinishLastPeriod(DateTime now)
    {
        var open = TimePeriods.FirstOrDefault(p => p.FinishAt is null);
        if (open == null)
            return false;
        
        open.FinishHabit(now);
        return true;
    }

    public TimePeriod? GetLastPeriod() => TimePeriods.MaxBy(p => p.StartedAt);

    public bool EditLastPeriodFinish(DateTime finishAt)
    {
        var lastPeriod = GetLastPeriod();
        if (lastPeriod is not { FinishAt: not null })
            return false;

        if (finishAt <= lastPeriod.StartedAt || finishAt > DateTime.UtcNow)
            return false;

        lastPeriod.FinishHabit(finishAt);
        return true;
    }

    public bool AddMissedPeriod(DateTime startedAt, DateTime finishAt, Epoch? completedEpoch = null)
    {
        if (completedEpoch != null && completedEpoch.HabitId != Id)
            return false;

        if (startedAt >= finishAt || finishAt > DateTime.UtcNow)
            return false;

        // O(n) скан — периодов на привычку мало; пересечение [startedAt, finishAt) с [p.StartedAt, p.FinishAt ?? +inf)
        var hasOverlap = TimePeriods.Any(p =>
            p.FinishAt is { } finish
                ? startedAt < finish && p.StartedAt < finishAt
                : finishAt > p.StartedAt);
        if (hasOverlap)
            return false;

        var timePeriod = TimePeriod.StartHabit(Id, completedEpoch?.Id ?? CurrentEpochId, startedAt);
        timePeriod.FinishHabit(finishAt);
        TimePeriods.Add(timePeriod);
        Updated = DateTime.UtcNow;
        return true;
    }

    public EpochCompletionBlock GetEpochCompletionBlock(Guid expectedEpochId)
    {
        if (Deleted || expectedEpochId != CurrentEpochId)
            return EpochCompletionBlock.Stale;
        if (TimePeriods.Any(p => p.FinishAt is null))
            return EpochCompletionBlock.TimerRunning;
        return TimePeriods.Any(p => p.EpochId == CurrentEpochId)
            ? EpochCompletionBlock.None : EpochCompletionBlock.Empty;
    }

    public Epoch? TryCompleteEpoch(Guid expectedEpochId, string? name, DateTime nowUtc)
    {
        var trimmed = name?.Trim();
        if (trimmed is null || trimmed.Contains('\0') ||
            trimmed.EnumerateRunes().Count() is < 1 or > Epoch.MaxNameLength ||
            GetEpochCompletionBlock(expectedEpochId) != EpochCompletionBlock.None)
            return null;

        var epoch = new Epoch(CurrentEpochId, Id, trimmed, nowUtc);
        CurrentEpochId = Guid.NewGuid();
        Updated = nowUtc;
        return epoch;
    }

    public void Delete()
    {
        Deleted = true;
        Updated = DateTime.UtcNow;
    }
}
