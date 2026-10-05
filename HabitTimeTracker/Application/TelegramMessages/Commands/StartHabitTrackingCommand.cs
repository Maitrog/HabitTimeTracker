using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartHabitTrackingCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (data == null)
            return;

        if (!Guid.TryParse(data, out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }
        
        habit.StartNewPeriod(DateTime.UtcNow);
        if (!user.StartHabitTracking())
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        await repository.UpdateHabitAsync(user, habit, ct);

        var menu = new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData("Закончить", $"{TelegramCommand.FinishHabit} {habit.Id}"),
            ]
        ]);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Записываем прогресс",
            replyMarkup: menu,
            cancellationToken: ct);
    }
}