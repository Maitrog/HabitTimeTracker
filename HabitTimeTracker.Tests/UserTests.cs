using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class UserTests
{
    private static User NewUser() =>
        User.FromTelegramUser(new Telegram.Bot.Types.User { Id = 1, FirstName = "Test" });

    [Fact]
    public void EpochCompletion_OnlyAcceptsRepliesToCurrentPrompt()
    {
        var user = NewUser();
        var habitId = Guid.NewGuid();
        Assert.True(user.StartEpochCompletion(habitId, Guid.NewGuid(), 10));
        user.ResetState();
        Assert.True(user.StartEpochCompletion(habitId, Guid.NewGuid(), 11));
        Assert.False(user.AcceptsEpochDialogInput(false, null, 10));
        Assert.False(user.AcceptsEpochDialogInput(true, 11, null));
        Assert.True(user.AcceptsEpochDialogInput(false, null, 11));
    }

    [Fact]
    public void ChooseMissedPeriodEpoch_KeepsTheSelectedId()
    {
        var user = NewUser();
        var habitId = Guid.NewGuid();
        var epochId = Guid.NewGuid();
        Assert.True(user.StartChoosingMissedPeriodEpoch(habitId, 20));
        Assert.False(user.AcceptsEpochDialogInput(true, 19, null));
        Assert.True(user.AcceptsEpochDialogInput(true, 20, null));
        Assert.True(user.SelectMissedPeriodEpoch(epochId));
        Assert.Equal(ChatState.AddingMissedPeriod, user.ChatState);
        Assert.Equal(habitId, user.EditingHabit!.HabitId);
        Assert.Equal(epochId, user.EditingHabit.EpochId);
        Assert.False(user.AcceptsEpochDialogInput(true, 20, null));
    }

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
