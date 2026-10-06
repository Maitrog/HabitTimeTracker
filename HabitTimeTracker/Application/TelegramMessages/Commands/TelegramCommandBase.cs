using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using User = HabitTimeTracker.Domain.Models.User;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public abstract class TelegramCommandBase(ITelegramBotClient botClient)
{
    protected readonly ITelegramBotClient BotClient = botClient;

    protected readonly InlineKeyboardMarkup DefaultMenu = new([
        [InlineKeyboardButton.WithCallbackData("Создать привычку", TelegramCommand.CreateHabit)],
        [InlineKeyboardButton.WithCallbackData("Начать привычку", TelegramCommand.ChooseHabit)],
        [InlineKeyboardButton.WithCallbackData("Удалить привычку", TelegramCommand.DeleteHabit)],
        [InlineKeyboardButton.WithCallbackData("Добавить пропущенный период", TelegramCommand.AddMissedPeriod)],
        [InlineKeyboardButton.WithCallbackData("Статистика", TelegramCommand.Statistics)],
        [InlineKeyboardButton.WithCallbackData("Часовой пояс", TelegramCommand.TimeZone)]
    ]);

    internal static string Truncate(string value, int max = 100) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";

    internal static string FormatDuration(long seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return $"{time.Days} д {time.Hours} ч {time.Minutes} мин {time.Seconds} сек";
    }

    public virtual async Task Execute(User user, string? data, CancellationToken ct = default) => await SendBaseAnswerAsync(user, ct);

    protected Task SendBaseAnswerAsync(User user, CancellationToken ct) =>
        BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Что-то пошло не так, повторите попытку позже",
            cancellationToken: ct);
}