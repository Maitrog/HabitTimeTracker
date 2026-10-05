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

    public void Delete()
    {
        Deleted = true;
        Updated = DateTime.UtcNow;
    }
}