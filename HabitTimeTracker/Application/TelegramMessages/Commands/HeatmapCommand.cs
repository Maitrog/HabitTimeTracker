using HabitTimeTracker.Application.Heatmaps;
using HabitTimeTracker.Application.TelegramMessages.Commands.Constants;
using HabitTimeTracker.DataAccess;
using HabitTimeTracker.Domain.Heatmaps;
using HabitTimeTracker.Domain.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = HabitTimeTracker.Domain.Models.User;

namespace HabitTimeTracker.Application.TelegramMessages.Commands;

public class HeatmapCommand(ITelegramBotClient botClient, IServiceProvider serviceProvider)
    : TelegramCommandBase(botClient)
{
    public override async Task Execute(User user, string? data, CancellationToken ct = default)
    {
        var repository = serviceProvider.GetRequiredService<HabitTimeTrackerDataContext>();

        var parts = data?.Split(':', 2) ?? [];
        if (parts.Length != 2 || !TryMapPeriod(parts[0], out var period) || !Guid.TryParse(parts[1], out var habitId))
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var habit = await repository.GetHabitAsync(habitId, user.Id, ct);
        if (habit == null)
        {
            await SendBaseAnswerAsync(user, ct);
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var durations = DailyDurationCalculator.Calculate(habit.TimePeriods, user.TimeZoneOffsetMinutes, nowUtc);
        var today = DateOnly.FromDateTime(nowUtc.AddMinutes(user.TimeZoneOffsetMinutes));
        var model = HeatmapLayoutBuilder.Build(period, today, durations);
        var png = HeatmapRenderer.Render(model);

        await BotClient.SendPhoto(
            chatId: user.TelegramId.Id,
            photo: InputFile.FromStream(new MemoryStream(png), "heatmap.png"),
            caption: BuildSummary(model),
            replyMarkup: StartHeatmapCommand.BuildPeriodMenu(habit.Id),
            cancellationToken: ct);
    }

    private static bool TryMapPeriod(string value, out HeatmapPeriod period)
    {
        period = value switch
        {
            "7" => HeatmapPeriod.Last7Days,
            "30" => HeatmapPeriod.Last30Days,
            "month" => HeatmapPeriod.CurrentMonth,
            "year" => HeatmapPeriod.CurrentYear,
            _ => (HeatmapPeriod)(-1)
        };
        return Enum.IsDefined(period);
    }

    private static string BuildSummary(HeatmapModel model)
    {
        var active = model.Cells.Where(c => c.DurationSeconds > 0).ToList();
        var total = active.Sum(c => c.DurationSeconds);
        if (total == 0)
            return "За выбранный период записей нет.";

        var best = active.MaxBy(c => c.DurationSeconds)!;
        return $"Всего: {FormatDuration(total)}\n" +
               $"Активных дней: {active.Count}\n" +
               $"Лучший день: {best.Date:dd.MM.yyyy} — {FormatDuration(best.DurationSeconds)}\n" +
               $"Среднее в активный день: {FormatDuration(total / active.Count)}";
    }
}
