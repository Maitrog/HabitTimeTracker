using HabitTimeTracker.Application.TelegramMessages.Commands.Factory;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Messages;

public class TextMessageHandler(IServiceProvider serviceProvider, ITelegramBotClient botClient)
{
    public async Task Handle(User user, string command, string commandData, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var telegramCommandFactory = scope.ServiceProvider.GetRequiredService<TelegramCommandFactory>();

        var executer = telegramCommandFactory.Create(botClient, user.ChatState, command);
        if (executer == null)
            return;

        await executer.Execute(user, commandData, ct);
    }
}