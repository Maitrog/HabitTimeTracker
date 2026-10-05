using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class CreateHabitCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        
        if (data == null)
            return;
        
        var habit = new Habit(user.Id, data, DateTime.UtcNow);
        user.ResetState();
        await repository.CreateHabitAsync(user, habit, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Новая привычка создана",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}