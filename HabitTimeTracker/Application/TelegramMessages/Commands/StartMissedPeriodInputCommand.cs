using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartMissedPeriodInputCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null || !Guid.TryParse(data, out var habitId) ||
            !user.StartAddingMissedPeriod(habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Введите пропущенный период в формате:\n" +
                  "```\n" +
                  "06.10.2026 14:30; 45" +
                  "```\n" +
                  "Первая часть — дата и время начала, вторая — длительность в минутах\n" +
                  "Время интерпретируется как UTC",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: ct);
    }
}
