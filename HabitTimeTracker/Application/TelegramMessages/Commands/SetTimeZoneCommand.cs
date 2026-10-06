using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class SetTimeZoneCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !int.TryParse(data.Trim(), out var minutes) || !user.SetTimeZoneOffsetMinutes(minutes))
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "Некорректное смещение. Введите целое число минут от -720 до 840:",
                cancellationToken: ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Часовой пояс сохранён: UTC{(minutes >= 0 ? "+" : string.Empty)}{minutes} мин.",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}
