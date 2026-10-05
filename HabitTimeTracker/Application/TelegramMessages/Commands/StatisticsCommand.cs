using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StatisticsCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        var habits = await repository.GetHabitTotalTimesAsync(user.Id, ct);

        var text = habits.Count == 0
            ? "У вас пока нет привычек"
            : string.Join("\n", habits.Select(h => $"{h.Name} — {FormatDuration(h.DurationInSeconds)}"));

        var menu = new InlineKeyboardMarkup(
            new[] { InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset) });

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: text,
            replyMarkup: menu,
            cancellationToken: ct);
    }

    private static string FormatDuration(long seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return $"{time.Days} д {time.Hours} ч {time.Minutes} мин {time.Seconds} сек";
    }
}
