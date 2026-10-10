namespace HabitTimeTracker.Domain.Models;

// Значения 2 и 6 пропущены намеренно: раньше там жили HabitDeletion и ChoosingHabitForMissedPeriod,
// их int-значения уже сохранены в БД — нумерацию остальных членов сдвигать нельзя.
public enum ChatState
{
    Default = 0,
    HabitCreation = 1,
    ChoosingHabitForStart = 3,
    HabitInProgress = 4,
    EditingLastPeriodFinish = 5,
    AddingMissedPeriod = 7,
    ChangingTimeZone = 8,
}
