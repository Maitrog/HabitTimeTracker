using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class CompleteEpochCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        var habit = user.EditingHabit is { } editing
            ? await repository.GetHabitAsync(editing.HabitId, user.Id, ct) : null;
        var expectedId = user.EditingHabit?.EpochId ?? Guid.Empty;
        var block = habit?.GetEpochCompletionBlock(expectedId) ?? EpochCompletionBlock.Stale;

        if (block != EpochCompletionBlock.None)
        {
            user.ResetState();
            await repository.UpdateUserAsync(user, ct);
            await BotClient.SendMessage(user.TelegramId.Id, StartEpochCompletionCommand.BlockMessage(block),
                replyMarkup: DefaultMenu, cancellationToken: ct);
            return;
        }

        var epoch = habit!.TryCompleteEpoch(expectedId, data, DateTime.UtcNow);
        if (epoch == null)
        {
            await BotClient.SendMessage(user.TelegramId.Id,
                $"Название должно содержать от 1 до {Epoch.MaxNameLength} символов. Ответьте на исходное сообщение ещё раз.",
                cancellationToken: ct);
            return;
        }

        user.ResetState();
        try
        {
            await repository.SaveCompletedEpochAsync(user, epoch, ct);
        }
        catch (DbUpdateException ex) when (ex is DbUpdateConcurrencyException ||
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_Epochs" })
        {
            await BotClient.SendMessage(user.TelegramId.Id,
                "Привычка или диалог изменились. Откройте меню заново.",
                replyMarkup: new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("В меню", TelegramCommand.Reset)]]),
                cancellationToken: ct);
            return;
        }

        await BotClient.SendMessage(user.TelegramId.Id,
            $"Эпоха «{epoch.Name}» завершена. Началась новая эпоха.",
            replyMarkup: DefaultMenu, cancellationToken: ct);
    }
}
