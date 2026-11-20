using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartEpochCompletionCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
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

        var block = habit.GetEpochCompletionBlock(habit.CurrentEpochId);
        if (block != EpochCompletionBlock.None)
        {
            await BotClient.SendMessage(user.TelegramId.Id, BlockMessage(block), cancellationToken: ct);
            return;
        }

        var prompt = await BotClient.SendMessage(user.TelegramId.Id,
            $"Ответьте на это сообщение названием завершённой эпохи (1–{Epoch.MaxNameLength} символов).",
            replyMarkup: new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("Отмена", TelegramCommand.Reset)]]),
            cancellationToken: ct);
        if (user.StartEpochCompletion(habit.Id, habit.CurrentEpochId, prompt.Id))
            await repository.UpdateUserAsync(user, ct);
    }

    internal static string BlockMessage(EpochCompletionBlock block) => block switch
    {
        EpochCompletionBlock.TimerRunning => "Сначала остановите таймер.",
        EpochCompletionBlock.Empty => "В текущей эпохе пока нет записей.",
        _ => "Эпоха изменилась или привычка недоступна. Откройте меню заново."
    };
}
