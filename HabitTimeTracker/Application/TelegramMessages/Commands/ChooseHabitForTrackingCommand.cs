using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class ChooseHabitForTrackingCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        if (!user.ChooseHabitForTracking())
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        var habits = await repository.GetActiveHabitsAsync(user.Id);
        
        InlineKeyboardMarkup menu = new(
            habits.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"{TelegramCommand.StartHabit} {x.Id}") }));

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Выберите привычку для начала отслеживания",
            replyMarkup: menu,
            cancellationToken: ct);
    }
}