using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartHeatmapCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    internal static InlineKeyboardMarkup BuildPeriodMenu(Guid habitId) => new([
        [InlineKeyboardButton.WithCallbackData("7 дней", $"{TelegramCommand.Heatmap} 7:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("30 дней", $"{TelegramCommand.Heatmap} 30:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Текущий месяц", $"{TelegramCommand.Heatmap} month:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Текущий год", $"{TelegramCommand.Heatmap} year:{habitId}")],
        [InlineKeyboardButton.WithCallbackData("Назад в статистику", TelegramCommand.Statistics)]
    ]);

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !Guid.TryParse(data, out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Выберите период для привычки «{habit.Name}»",
            replyMarkup: BuildPeriodMenu(habit.Id),
            cancellationToken: ct);
    }
}
