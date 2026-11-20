using System.Globalization;
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

        var habit = user.EditingHabit is { } editing
            ? await repository.GetHabitAsync(editing.HabitId, user.Id, ct) : null;
        var selected = user.EditingHabit?.EpochId ?? habit?.CurrentEpochId;
        var completed = habit != null && selected is Guid selectedId && selectedId != habit.CurrentEpochId
            ? await repository.GetCompletedEpochAsync(habit.Id, user.Id, selectedId, ct) : null;
        if (habit == null || (selected != habit.CurrentEpochId && completed == null))
        {
            user.ResetState();
            await repository.UpdateUserAsync(user, ct);
            await BotClient.SendMessage(user.TelegramId.Id, "Эпоха или привычка недоступна. Начните добавление заново.",
                replyMarkup: DefaultMenu, cancellationToken: ct);
            return;
        }

        // Check the duration before adding it to a user-supplied date.
        if (minutes > (DateTime.UtcNow - startedAt).TotalMinutes ||
            !habit.AddMissedPeriod(startedAt, startedAt.AddMinutes(minutes), completed))
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "Период не может заканчиваться в будущем и не должен пересекаться с существующими периодами. Попробуйте ещё раз:",
                cancellationToken: ct);
            return;
        }

        user.ResetState();
        try
        {
            await repository.UpdateHabitAsync(user, habit, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await BotClient.SendMessage(user.TelegramId.Id, "Привычка или диалог изменились. Начните добавление заново.",
                replyMarkup: new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("В меню", TelegramCommand.Reset)]]),
                cancellationToken: ct);
            return;
        }

        var period = habit.TimePeriods.Last();
        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: FinishHabitCommand.BuildConfirmationText(period),
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: period == habit.GetLastPeriod() ? FinishHabitCommand.BuildMenu(habit.Id) : DefaultMenu,
            cancellationToken: ct);
    }
}
