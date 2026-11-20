namespace HabitTimeTracker.Domain.ValueObjects;

public record EditingHabit(Guid HabitId, Guid? EpochId = null, int? PromptMessageId = null);
