using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class FinishHabitCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    internal static string BuildConfirmationText(Habit habit)
    {
        var lastPeriod = habit.TimePeriods.Last();
        return $"Привычка записана:\n" +
               $"Начало: `{lastPeriod.StartedAt:dd.MM.yyyy HH:mm:ss}`\n" +
               $"Завершение: `{lastPeriod.FinishAt:dd.MM.yyyy HH:mm:ss}`\n" +
               $"Затраченное время: {lastPeriod.DurationInSecondes} секунд";
    }

    internal static InlineKeyboardMarkup BuildMenu(Guid habitId) => new([
        [InlineKeyboardButton.WithCallbackData("Изменить", $"{TelegramCommand.EditLastPeriodFinish} {habitId}")],
        [InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset)]
    ]);

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null)
            return;

        if (!Guid.TryParse(data, out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, user.Id, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        habit.FinishLastPeriod(DateTime.UtcNow);
        user.ResetState();

        await repository.UpdateHabitAsync(user, habit, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: BuildConfirmationText(habit),
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: BuildMenu(habit.Id),
            cancellationToken: ct);
    }
}
