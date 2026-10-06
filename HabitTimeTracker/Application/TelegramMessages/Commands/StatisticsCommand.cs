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

        var buttons = habits
            .Select(h => new[] { InlineKeyboardButton.WithCallbackData($"Heatmap: {Truncate(h.Name)}", $"{TelegramCommand.HeatmapHabit} {h.Id}") })
            .Append([InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset)]);

        var menu = new InlineKeyboardMarkup(buttons);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: text,
            replyMarkup: menu,
            cancellationToken: ct);
    }
}
