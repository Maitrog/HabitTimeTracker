using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using User = HabitTimeTracker.Domain.Models.User;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StatisticsCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    internal static InlineKeyboardMarkup BuildPeriodMenu(Guid habitId) => new([
        [InlineKeyboardButton.WithCallbackData("7 дней", $"{TelegramCommand.Heatmap} 7:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("30 дней", $"{TelegramCommand.Heatmap} 30:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Текущий месяц", $"{TelegramCommand.Heatmap} month:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Текущий год", $"{TelegramCommand.Heatmap} year:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Назад к привычкам", TelegramCommand.HabitManagement)]
    ]);

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !Guid.TryParse(data, out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habits = await repository.GetHabitTotalTimesAsync(user.Id, ct);
        var habit = habits.FirstOrDefault(h => h.Id == habitId);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Привычка «{habit.Name}»\nОбщее затраченное время: {FormatDuration(habit.DurationInSeconds)}",
            replyMarkup: BuildPeriodMenu(habit.Id),
            cancellationToken: ct);
    }
}
