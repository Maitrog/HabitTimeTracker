using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class UserTests
{
    private static User NewUser() =>
        User.FromTelegramUser(new Telegram.Bot.Types.User { Id = 1, FirstName = "Test" });

    [Fact]
    public void StartAddingMissedPeriod_FromDefault_Succeeds()
    {
        var user = NewUser();

        Assert.True(user.StartAddingMissedPeriod(Guid.NewGuid()));
        Assert.Equal(ChatState.AddingMissedPeriod, user.ChatState);
    }

    [Fact]
    public void StartAddingMissedPeriod_FromBusyState_IsRejected()
    {
        var user = NewUser();
        Assert.True(user.StartHabitCreation());

        Assert.False(user.StartAddingMissedPeriod(Guid.NewGuid()));
        Assert.Equal(ChatState.HabitCreation, user.ChatState);
    }

    [Fact]
    public void ResetState_ClearsStateAndEditingHabit()
    {
        var user = NewUser();
        Assert.True(user.StartAddingMissedPeriod(Guid.NewGuid()));

        user.ResetState();

        Assert.Equal(ChatState.Default, user.ChatState);
        Assert.Null(user.EditingHabit);
    }
}
