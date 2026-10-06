using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace HabitTimeTracker.Domain.Models;

public class Habit
{
    public Guid Id { get; private set; }
    
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
        Created = now;
        Updated = now;
        UserId = userId;
        Name = name;
    }

    public bool StartNewPeriod(DateTime now)
    {
        var lastPeriod = TimePeriods.LastOrDefault();
        if (lastPeriod is { FinishAt: null })
            return false;

        var timePeriod = TimePeriod.StartHabit(Id, now);
        TimePeriods.Add(timePeriod);
        return true;
    }

    public bool FinishLastPeriod(DateTime now)
    {
        var lastPeriod = TimePeriods.LastOrDefault();
        if (lastPeriod is not { FinishAt: null })
            return false;
        
        lastPeriod.FinishHabit(now);
        return true;
    }

    public bool EditLastPeriodFinish(DateTime finishAt)
    {
        var lastPeriod = TimePeriods.LastOrDefault();
        if (lastPeriod is not { FinishAt: not null })
            return false;

        if (finishAt <= lastPeriod.StartedAt || finishAt > DateTime.UtcNow)
            return false;

        lastPeriod.FinishHabit(finishAt);
        return true;
    }

    public bool AddMissedPeriod(DateTime startedAt, DateTime finishAt)
    {
        if (startedAt >= finishAt || finishAt > DateTime.UtcNow)
            return false;

        // O(n) скан — периодов на привычку мало; пересечение [startedAt, finishAt) с [p.StartedAt, p.FinishAt ?? +inf)
        var hasOverlap = TimePeriods.Any(p =>
            p.FinishAt is { } finish
                ? startedAt < finish && p.StartedAt < finishAt
                : finishAt > p.StartedAt);
        if (hasOverlap)
            return false;

        var timePeriod = TimePeriod.StartHabit(Id, startedAt);
        timePeriod.FinishHabit(finishAt);
        TimePeriods.Add(timePeriod);
        Updated = DateTime.UtcNow;
        return true;
    }

    public void Delete()
    {
        Deleted = true;
        Updated = DateTime.UtcNow;
    }
}