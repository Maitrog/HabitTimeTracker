using System.ComponentModel.DataAnnotations.Schema;

namespace HabitTimeTracker.Domain.Models;

public class TimePeriod
{
    public Guid Id { get; private init; }

    public DateTime StartedAt { get; private init; }

    public DateTime? FinishAt { get; private set; }
    
    public long DurationInSecondes { get; private set; }
    
    public Guid HabitId { get; private set; }

    public Guid EpochId { get; private set; }
    
    [ForeignKey(nameof(HabitId))]
    public Habit? Habit { get; private set; }
    
    private TimePeriod() { }

    public static TimePeriod StartHabit(Guid habitId, Guid epochId, DateTime startedAt) => new()
    {
        HabitId = habitId,
        EpochId = epochId,
        StartedAt = startedAt,
    };

    public void FinishHabit(DateTime finishAt)
    {
        FinishAt = finishAt;
        DurationInSecondes = (long)(finishAt - StartedAt).TotalSeconds;
    }
}
