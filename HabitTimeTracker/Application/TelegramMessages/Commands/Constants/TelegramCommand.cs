namespace HabitTimeTracker.Application.TelegramMessages.Commands.Constants;

public abstract class TelegramCommand
{
    public const string Start = "/start";

    public const string Reset = "/reset";

    public const string CreateHabit = "/createhabit";
    
    public const string DeleteHabit = "/deletehabit";
    
    public const string ChooseHabit = "/choosehabit";
    
    public const string StartHabit = "/starthabit";
    
    public const string FinishHabit = "/finishhabit";
    
    public const string Statistics = "/statistics";
}