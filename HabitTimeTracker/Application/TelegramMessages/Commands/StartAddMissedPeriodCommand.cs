using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class StartAddMissedPeriodCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        if (!user.StartChoosingHabitForMissedPeriod())
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habits = await repository.GetActiveHabitsAsync(user.Id);

        if (habits.Count == 0)
        {
            await BotClient.SendMessage(
                chatId: user.TelegramId.Id,
                text: "У вас пока нет привычек. Сначала создайте привычку.",
                cancellationToken: ct);
            return;
        }

        await repository.UpdateUserAsync(user, ct);

        InlineKeyboardMarkup menu = new(
            habits.Select(x => new[] { InlineKeyboardButton.WithCallbackData(x.Name, $"{TelegramCommand.AddMissedPeriodHabit} {x.Id}") })
                .Append([InlineKeyboardButton.WithCallbackData("Назад в главное меню", TelegramCommand.Reset)]));

        await BotClient.SendMessage(
            chatId: user.TelegramId.Id,
            text: "Выберите привычку для добавления пропущенного периода",
            replyMarkup: menu,
            cancellationToken: ct);
    }
}
