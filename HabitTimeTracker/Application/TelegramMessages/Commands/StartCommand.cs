using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartCommand(ITelegramBotClient botClient) : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Добро пожаловать! Начните отслеживать время потраченное на свои новые привычки",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}
