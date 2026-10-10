using HabitTimeTracker.Domain.ValueObjects;

namespace HabitTimeTracker.Domain.Models;

public class User
{
    public Guid Id { get; private set; }

    public DateTime? Created { get; private set; }

    public DateTime? Updated { get; private set; }

    public TelegramId TelegramId { get; private set; }

    public TelegramName Name { get; private set; }
    
    public ChatState ChatState { get; private set; }

    public EditingHabit? EditingHabit { get; private set; }
    
    public uint Version { get; private set; }

    public int TimeZoneOffsetMinutes { get; private set; } = 180;

    private  User() {}

    public static User FromTelegramUser(Telegram.Bot.Types.User user) => new()
    {
        Id = Guid.NewGuid(),
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        TelegramId = new TelegramId(user.Id),
        Name = new TelegramName(user.FirstName, user.LastName, user.Username)
    };

    public bool StartHabitCreation()
    {
        if (ChatState != ChatState.Default)
            return false;

        ChatState = ChatState.HabitCreation;
        Updated = DateTime.UtcNow;
        return true;
    }

    public bool ChooseHabitForTracking()
    {
        if (ChatState != ChatState.Default)
            return false;

        ChatState = ChatState.ChoosingHabitForStart;
        Updated = DateTime.UtcNow;
        return true;
    }
    
    public bool StartHabitTracking()
    {
        if (ChatState != ChatState.ChoosingHabitForStart)
            return false;

        ChatState = ChatState.HabitInProgress;
        Updated = DateTime.UtcNow;
        return true;
    }
    
    public bool StartEditLastPeriodFinish(Guid habitId)
    {
        if (ChatState != ChatState.Default)
            return false;

        ChatState = ChatState.EditingLastPeriodFinish;
        EditingHabit = new EditingHabit(habitId);
        Updated = DateTime.UtcNow;
        return true;
    }

    public bool StartAddingMissedPeriod(Guid habitId)
    {
        if (ChatState != ChatState.Default)
            return false;

        ChatState = ChatState.AddingMissedPeriod;
        EditingHabit = new EditingHabit(habitId);
        Updated = DateTime.UtcNow;
        return true;
    }

    public bool StartChangingTimeZone()
    {
        if (ChatState != ChatState.Default)
            return false;

        ChatState = ChatState.ChangingTimeZone;
        Updated = DateTime.UtcNow;
        return true;
    }

    public bool SetTimeZoneOffsetMinutes(int minutes)
    {
        if (minutes is < -720 or > 840)
            return false;

        TimeZoneOffsetMinutes = minutes;
        ResetState();
        return true;
    }

    public void ResetState()
    {
        ChatState = ChatState.Default;
        EditingHabit = null;
        Updated = DateTime.UtcNow;
    }
}