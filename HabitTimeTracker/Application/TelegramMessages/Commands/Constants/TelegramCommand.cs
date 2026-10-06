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

    public const string EditLastPeriodFinish = "/editlastperiodfinish";

    public const string AddMissedPeriod = "/addmissedperiod";

    public const string AddMissedPeriodHabit = "/addmissedperiodhabit";

    public const string TimeZone = "/timezone";

    public const string HeatmapHabit = "/heatmaphabit";

    public const string Heatmap = "/heatmap";
}