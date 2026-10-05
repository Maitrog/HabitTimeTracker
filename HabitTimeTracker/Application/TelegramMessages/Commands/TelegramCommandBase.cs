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
        [InlineKeyboardButton.WithCallbackData("Удалить привычку", TelegramCommand.DeleteHabit)]
    ]);

    public virtual async Task Execute(User user, string? data, CancellationToken ct = default) => await SendBaseAnswerAsync(user, ct);

    protected Task SendBaseAnswerAsync(User user, CancellationToken ct) =>
        BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Что-то пошло не так, повторите попытку позже",
            cancellationToken: ct);
}