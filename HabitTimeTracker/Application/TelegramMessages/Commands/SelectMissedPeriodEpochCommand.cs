using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class SelectMissedPeriodEpochCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider, bool isPaging)
    : TelegramCommandBase(botClient)
{
    private const int PageSize = 10;

    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        if (user.EditingHabit is not { PromptMessageId: { } messageId } editing ||
            await repository.GetHabitAsync(editing.HabitId, user.Id, ct) is not { } habit)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        if (isPaging)
        {
            var epochs = await repository.GetCompletedEpochsAsync(habit.Id, user.Id, ct);
            if (int.TryParse(data, out var page) && page >= 0 && page <= (epochs.Count - 1) / PageSize)
                await BotClient.EditMessageReplyMarkup(user.TelegramId.Id, messageId,
                    replyMarkup: BuildMenu(habit, epochs, user.TimeZoneOffsetMinutes, page), cancellationToken: ct);
            return;
        }

        if (!Guid.TryParseExact(data, "N", out var epochId) ||
            (epochId != habit.CurrentEpochId && await repository.GetCompletedEpochAsync(habit.Id, user.Id, epochId, ct) == null))
        {
            await BotClient.SendMessage(user.TelegramId.Id, "Эта эпоха недоступна. Выберите другую.", cancellationToken: ct);
            return;
        }

        if (user.SelectMissedPeriodEpoch(epochId))
        {
            await repository.UpdateUserAsync(user, ct);
            await StartMissedPeriodInputCommand.SendPeriodPromptAsync(BotClient, user, ct);
        }
    }

    internal static InlineKeyboardMarkup BuildMenu(Habit habit, IReadOnlyList<Epoch> epochs, int offset, int page)
    {
        List<InlineKeyboardButton[]> rows = [[PickButton("Текущая эпоха", habit.CurrentEpochId)]];
        foreach (var (id, caption) in EpochHistoryCommand.Labels(epochs, offset, 60).Skip(page * PageSize).Take(PageSize))
            rows.Add([PickButton(caption, id)]);

        List<InlineKeyboardButton> pages = [];
        if (page > 0)
            pages.Add(InlineKeyboardButton.WithCallbackData("Назад", $"{TelegramCommand.PageMissedPeriodEpochs} {page - 1}"));
        if (page < (epochs.Count - 1) / PageSize)
            pages.Add(InlineKeyboardButton.WithCallbackData("Далее", $"{TelegramCommand.PageMissedPeriodEpochs} {page + 1}"));
        if (pages.Count > 0)
            rows.Add(pages.ToArray());
        rows.Add([InlineKeyboardButton.WithCallbackData("Отмена", TelegramCommand.Reset)]);
        return new InlineKeyboardMarkup(rows);
    }

    private static InlineKeyboardButton PickButton(string caption, Guid epochId) =>
        InlineKeyboardButton.WithCallbackData(caption, $"{TelegramCommand.SelectMissedPeriodEpoch} {epochId:N}");
}
