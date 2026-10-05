using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartHabitCreationCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        if (!user.StartHabitCreation())
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Введите название привычки",
            cancellationToken: ct);
    }
}