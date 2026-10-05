using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class FinishHabitCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
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

        habit.FinishLastPeriod(DateTime.UtcNow);
        user.ResetState();

        await repository.UpdateHabitAsync(user, habit, ct);

        var lastPeriod = habit.TimePeriods.Last();
        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Привычка записана. Начало: {lastPeriod.StartedAt:HH:mm:ss zz}. Завершение: {lastPeriod.FinishAt:HH:mm:ss zz}. Затраченное время: {lastPeriod.DurationInSecondes} секунд",
            cancellationToken: ct);
        
        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Что будем делать",
            replyMarkup: DefaultMenu,
            cancellationToken: ct);
    }
}