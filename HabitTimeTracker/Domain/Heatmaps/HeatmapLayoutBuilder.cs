using System.Globalization;

namespace HabitTimeTracker.Domain.Heatmaps;

public static class HeatmapLayoutBuilder
{
    private static readonly string[] Weekdays = ["Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"];

    public static HeatmapModel Build(
        HeatmapPeriod period, DateOnly today, IReadOnlyDictionary<DateOnly, long> durations)
    {
        return period switch
        {
            HeatmapPeriod.Last7Days => BuildWeekStrip(today, durations),
            HeatmapPeriod.Last30Days => BuildCalendar(
                today.AddDays(-29), today, "Последние 30 дней", durations, DayNumberHeaders),
            HeatmapPeriod.CurrentMonth => BuildCalendar(
                new DateOnly(today.Year, today.Month, 1),
                new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month)),
                MonthTitle(today), durations, NoHeaders),
            HeatmapPeriod.CurrentYear => BuildCalendar(
                new DateOnly(today.Year, 1, 1),
                new DateOnly(today.Year, 12, 31),
                today.Year.ToString(), durations, MonthHeaders),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    private static HeatmapModel BuildWeekStrip(DateOnly today, IReadOnlyDictionary<DateOnly, long> durations)
    {
        var start = today.AddDays(-6);
        var cells = new List<HeatmapCell>();
        for (var i = 0; i < 7; i++)
        {
            var date = start.AddDays(i);
            cells.Add(new HeatmapCell(0, i, date, durations.GetValueOrDefault(date)));
        }

        return new HeatmapModel(1, 7, "Последние 7 дней", cells, [], [],
            start.ToString("dd.MM"), today.ToString("dd.MM"));
    }

    private static HeatmapModel BuildCalendar(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string title,
        IReadOnlyDictionary<DateOnly, long> durations,
        Func<IReadOnlyList<DateOnly>, DateOnly, IReadOnlyList<(int, string)>> headers)
    {
        var anchor = rangeStart.AddDays(-(((int)rangeStart.DayOfWeek + 6) % 7)); // понедельник <= rangeStart
        var columns = ((rangeEnd.DayNumber - anchor.DayNumber) / 7) + 1;

        var cells = new List<HeatmapCell>();
        var weekStarts = new List<DateOnly>();
        for (var c = 0; c < columns; c++)
        {
            var weekStart = anchor.AddDays(c * 7);
            weekStarts.Add(weekStart);
            for (var r = 0; r < 7; r++)
            {
                var date = weekStart.AddDays(r);
                if (date < rangeStart || date > rangeEnd)
                    continue;

                cells.Add(new HeatmapCell(r, c, date, durations.GetValueOrDefault(date)));
            }
        }

        return new HeatmapModel(7, columns, title, cells, Weekdays, headers(weekStarts, rangeStart), null, null);
    }

    private static IReadOnlyList<(int, string)> DayNumberHeaders(
        IReadOnlyList<DateOnly> weekStarts, DateOnly rangeStart) =>
        weekStarts.Select((d, i) => (i, d.Day.ToString())).ToList();

    private static IReadOnlyList<(int, string)> MonthHeaders(
        IReadOnlyList<DateOnly> weekStarts, DateOnly rangeStart)
    {
        var headers = new List<(int, string)>();
        for (var i = 0; i < weekStarts.Count; i++)
        {
            var monthDate = weekStarts[i] < rangeStart ? rangeStart : weekStarts[i];
            if (i == 0 || monthDate.Month != (weekStarts[i - 1] < rangeStart ? rangeStart : weekStarts[i - 1]).Month)
                headers.Add((i, MonthName(monthDate.Month)));
        }

        return headers;
    }

    private static IReadOnlyList<(int, string)> NoHeaders(
        IReadOnlyList<DateOnly> weekStarts, DateOnly rangeStart) => [];

    private static string MonthTitle(DateOnly date)
    {
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var name = date.ToString("MMMM", ru);
        return char.ToUpper(name[0], ru) + name[1..] + " " + date.Year;
    }

    private static string MonthName(int month)
    {
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var name = new DateTime(2000, month, 1).ToString("MMMM", ru);
        return char.ToUpper(name[0], ru) + name[1..];
    }
}
