using HabitTimeTracker.Application.TelegramMessages.Commands.Factory;
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Messages;

public class TextMessageHandler(IServiceProvider serviceProvider, ITelegramBotClient botClient)
{
    public async Task Handle(User user, string command, string commandData, CancellationToken ct,
        bool isCallback = false, int? callbackMessageId = null, int? replyToMessageId = null)
    {
        if (command != TelegramCommand.Reset &&
            !user.AcceptsEpochDialogInput(isCallback, callbackMessageId, replyToMessageId))
            return;

        using var scope = serviceProvider.CreateScope();
        var telegramCommandFactory = scope.ServiceProvider.GetRequiredService<TelegramCommandFactory>();

        var executer = telegramCommandFactory.Create(botClient, user.ChatState, command);
        if (executer == null)
            return;

        await executer.Execute(user, commandData, ct);
    }
}
