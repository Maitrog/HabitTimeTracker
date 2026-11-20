using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using System.Globalization;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class EpochHistoryCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();
        if (!Guid.TryParse(data, out var habitId) || await repository.GetHabitAsync(habitId, user.Id, ct) is not { } habit)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var epochs = await repository.GetCompletedEpochsAsync(habit.Id, user.Id, ct);
        var durations = habit.TimePeriods.GroupBy(p => p.EpochId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.DurationInSecondes));
        var rows = Labels(epochs, user.TimeZoneOffsetMinutes)
            .Select(e => $"{e.Caption} — {FormatDuration(durations.GetValueOrDefault(e.Id))}");
        var pages = rows.DefaultIfEmpty("Завершённых эпох пока нет.").Chunk(10).ToArray();
        var back = new InlineKeyboardMarkup([[InlineKeyboardButton.WithCallbackData("Назад к привычке",
            $"{TelegramCommand.HabitManagement} {habit.Id}")]]);

        for (var i = 0; i < pages.Length; i++)
            await BotClient.SendMessage(user.TelegramId.Id, string.Join('\n', pages[i]),
                replyMarkup: i == pages.Length - 1 ? back : null, cancellationToken: ct);
    }

    internal static IEnumerable<(Guid Id, string Caption)> Labels(IReadOnlyList<Epoch> epochs, int offset, int? maxLength = null)
    {
        var duplicates = epochs.GroupBy(e => e.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        foreach (var epoch in epochs)
        {
            var suffix = duplicates.Contains(epoch.Name)
                ? $" ({epoch.CompletedAtUtc.AddMinutes(offset):dd.MM.yyyy HH:mm:ss})" : "";
            var name = epoch.Name;
            if (maxLength is int limit)
            {
                var elements = new StringInfo(name);
                var available = limit - suffix.Length;
                if (elements.LengthInTextElements > available)
                    name = elements.SubstringByTextElements(0, available - 1) + "…";
            }
            yield return (epoch.Id, name + suffix);
        }
    }
}
