using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class DeleteHabitCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
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

        habit.Delete();
        user.ResetState();

        await repository.UpdateHabitAsync(user, habit, ct);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Привычка успешно удалена",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}