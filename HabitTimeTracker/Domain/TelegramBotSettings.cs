using HabitTimeTracker.Domain.Models;
using HabitTimeTracker.Domain.ValueObjects;

namespace HabitTimeTracker.Domain;

public class TelegramBotSettings
{
    public string Token { get; set; } = string.Empty;

    public List<long> AllowedUsers { get; set; } = [];
}