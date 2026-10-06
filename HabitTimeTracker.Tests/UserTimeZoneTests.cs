using HabitTimeTracker.Domain.Models;

namespace HabitTimeTracker.Tests;

public class UserTimeZoneTests
{
    private static User NewUser() =>
        User.FromTelegramUser(new Telegram.Bot.Types.User { Id = 1, FirstName = "Test" });

    [Fact]
    public void DefaultOffsetIs180()
    {
        Assert.Equal(180, NewUser().TimeZoneOffsetMinutes);
    }

    [Fact]
    public void SetValidOffset_ChangesAndResetsState()
    {
        var user = NewUser();
        Assert.True(user.StartChangingTimeZone());
        Assert.True(user.SetTimeZoneOffsetMinutes(-300));
        Assert.Equal(-300, user.TimeZoneOffsetMinutes);
        Assert.Equal(ChatState.Default, user.ChatState);
    }

    [Theory]
    [InlineData(-721)]
    [InlineData(841)]
    public void SetInvalidOffset_IsRejected(int minutes)
    {
        var user = NewUser();
        Assert.False(user.SetTimeZoneOffsetMinutes(minutes));
        Assert.Equal(180, user.TimeZoneOffsetMinutes);
    }
}
