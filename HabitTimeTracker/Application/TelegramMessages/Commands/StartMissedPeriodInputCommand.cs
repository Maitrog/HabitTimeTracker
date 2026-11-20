using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartMissedPeriodInputCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (!Guid.TryParse(data, out var habitId) || await repository.GetHabitAsync(habitId, user.Id, ct) is not { } habit)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var epochs = await repository.GetCompletedEpochsAsync(habit.Id, user.Id, ct);
        if (epochs.Count == 0)
        {
            if (!user.StartAddingMissedPeriod(habit.Id, habit.CurrentEpochId))
                return;
            await repository.UpdateUserAsync(user, ct);
            await SendPeriodPromptAsync(BotClient, user, ct);
            return;
        }

        var prompt = await BotClient.SendMessage(user.TelegramId.Id, "Выберите эпоху для пропущенного периода:",
            replyMarkup: SelectMissedPeriodEpochCommand.BuildMenu(habit, epochs, user.TimeZoneOffsetMinutes, 0),
            cancellationToken: ct);
        if (user.StartChoosingMissedPeriodEpoch(habit.Id, prompt.Id))
            await repository.UpdateUserAsync(user, ct);
    }

    internal static Task SendPeriodPromptAsync(ITelegramBotClient bot, User user, CancellationToken ct) =>
        bot.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Введите пропущенный период в формате:\n" +
                  "```\n" +
                  "06.10.2026 14:30; 45" +
                  "```\n" +
                  "Первая часть — дата и время начала, вторая — длительность в минутах\n" +
                  "Время интерпретируется как UTC",
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("Отмена", TelegramCommand.Reset)]]),
            cancellationToken: ct);
}
