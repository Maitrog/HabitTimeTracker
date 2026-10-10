using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using User = HabitTimeTracker.Domain.Models.User;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class HabitManagementCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        if (Guid.TryParse(data, out var habitId))
            await SendActionsAsync(user, habitId, ct);
        else
            await SendHabitListAsync(user, ct);
    }

    private async Task SendHabitListAsync(User user, CancellationToken ct)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        var habits = await repository.GetActiveHabitsAsync(user.Id);

        if (habits.Count == 0)
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "У вас пока нет привычек. Сначала создайте привычку.",
                replyMarkup: DefaultMenu,
                cancellationToken: ct);
            return;
        }

        InlineKeyboardMarkup menu = new(
            habits.Select(x => new[] { InlineKeyboardButton.WithCallbackData(Truncate(x.Name), $"{TelegramCommand.HabitManagement} {x.Id}") })
                .Append([InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset)]));

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Управление привычками: выберите привычку",
            replyMarkup: menu,
            cancellationToken: ct);
    }

    private async Task SendActionsAsync(User user, Guid habitId, CancellationToken ct)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        var habit = await repository.GetHabitAsync(habitId, user.Id, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var menu = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("Удалить привычку", $"{TelegramCommand.DeleteHabit} {habit.Id}")],
            [InlineKeyboardButton.WithCallbackData("Добавить пропущенный период", $"{TelegramCommand.AddMissedPeriodHabit} {habit.Id}")],
            [InlineKeyboardButton.WithCallbackData("Статистика", $"{TelegramCommand.Statistics} {habit.Id}")],
            [InlineKeyboardButton.WithCallbackData("Назад к привычкам", TelegramCommand.HabitManagement)]
        ]);

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: $"Управление привычкой «{habit.Name}»",
            replyMarkup: menu,
            cancellationToken: ct);
    }
}
