namespace HabitTimeTracker.Domain.Models;

public class Epoch
{
    public const int MaxNameLength = 120;

    public Guid Id { get; private set; }
    public Guid HabitId { get; private set; }
    public string Name { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    internal Epoch(Guid id, Guid habitId, string name, DateTime completedAtUtc)
    {
        Id = id;
        HabitId = habitId;
        Name = name;
        CompletedAtUtc = completedAtUtc;
    }
}

public enum EpochCompletionBlock { None, Stale, TimerRunning, Empty }
