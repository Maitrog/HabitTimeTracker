using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class ResetCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider) : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        user.ResetState();
        await repository.UpdateUserAsync(user, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Добро пожаловать! Начните отслеживать время потраченное на свои новые привычки",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}
