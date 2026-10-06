namespace HabitTimeTracker.Domain.Models;

public enum ChatState
{
    Default,
    HabitCreation,
    HabitDeletion,
    ChoosingHabitForStart,
    HabitInProgress,
    EditingLastPeriodFinish,
}