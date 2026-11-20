using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartEditLastPeriodFinishCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !Guid.TryParse(data, out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, user.Id, ct);
        if (habit?.GetLastPeriod() is not { FinishAt: not null } lastPeriod ||
            !user.StartEditLastPeriodFinish(habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Введите время, когда вы закончили:\nТекущее значение: `{lastPeriod.FinishAt:dd.MM.yyyy HH:mm:ss}`",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: ct);
    }
}
