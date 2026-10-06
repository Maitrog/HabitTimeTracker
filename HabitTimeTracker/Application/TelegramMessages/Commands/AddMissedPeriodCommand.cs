using System.Globalization;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class AddMissedPeriodCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    private const string DateTimeFormat = "dd.MM.yyyy HH:mm";

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        var parts = data?.Trim().Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (parts.Length != 2 ||
            !DateTime.TryParseExact(parts[0], DateTimeFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var startedAt) ||
            !uint.TryParse(parts[1], out var minutes) || minutes == 0)
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: $"Некорректный формат. Введите: {DateTimeFormat}; длительность_в_минутах",
                cancellationToken: ct);
            return;
        }

        var habit = await repository.GetHabitAsync(user.EditingHabit!.HabitId, user.Id, ct);
        if (habit == null || !habit.AddMissedPeriod(startedAt, startedAt.AddMinutes(minutes)))
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "Период не может заканчиваться в будущем и не должен пересекаться с существующими периодами. Попробуйте ещё раз:",
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
