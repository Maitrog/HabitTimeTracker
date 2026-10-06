using System.Globalization;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class EditLastPeriodFinishCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    private const string Format = "dd.MM.yyyy HH:mm:ss";

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || user.EditingHabit is not { } editing ||
            !DateTime.TryParseExact(data.Trim(), Format, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var finishAt))
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: $"Некорректное время. Введите дату и время в формате {Format}",
                cancellationToken: ct);
            return;
        }

        var habit = await repository.GetHabitAsync(editing.HabitId, ct);
        if (habit == null || !habit.EditLastPeriodFinish(finishAt))
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "Время должно быть позже начала периода и не в будущем. Попробуйте ещё раз:",
                cancellationToken: ct);
            return;
        }

        user.ResetState();
        await repository.UpdateHabitAsync(user, habit, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: FinishHabitCommand.BuildConfirmationText(habit),
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: FinishHabitCommand.BuildMenu(habit.Id),
            cancellationToken: ct);
    }
}
